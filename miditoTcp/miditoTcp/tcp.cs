//Copied from https://qiita.com/Apeworks/items/94b75416d1534dbd4507

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace miditoTcp
{
    public class TcpCommunicator : IDisposable
    {
        public TcpCommunicator(TcpClient tcpClient)
        {
            if (tcpClient == null) throw new ArgumentNullException(nameof(tcpClient));
            this.TcpClient = tcpClient;
            this.Name = $"[{this.Socket.RemoteEndPoint}]";
        }

        public TcpCommunicator(string host, int port) : this(new TcpClient(host, port)) { }

        private TcpClient TcpClient { get; }

        protected Socket Socket => this.TcpClient?.Client;

        public string Name { get; }

        public bool IsConnecting
        {
            get
            {
                try
                {
                    if ((this.TcpClient == null) || !this.TcpClient.Connected) return false;
                    if (this.Socket == null) return false;
                    return !(this.Socket.Poll(1, SelectMode.SelectRead) && (this.Socket.Available <= 0));
                }
                catch
                {
                    return false; // 強制で切断した場合に Socket が null になるため、例外を無視
                }
            }
        }

        public void Dispose()
        {
            if (this.TcpClient != null)
            {
                this.TcpClient.Close();
                (this.TcpClient as IDisposable).Dispose();
            }
        }

        static string ToHexString(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var sb = new StringBuilder(data.Length * 2);
            foreach (var item in data) sb.Append($"{item:X2}");
            return sb.ToString();
        }

        public void Send(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            //if (!this.IsConnecting) throw new InvalidOperationException();
            Console.WriteLine($"{this.Name} << " + ToHexString(data));
            var stream = this.TcpClient.GetStream();
            stream.Write(data, 0, data.Length);
            //오버헤드 이슈로 try catch 일단 제거
            //try
            //{

            //}
            //catch (Exception ex)
            //{
            //    throw new ApplicationException("Attempt to send failed.", ex);
            //}
        }

        public byte[] Receive()
        {
            if (!this.IsConnecting) throw new InvalidOperationException();
            try
            {
                using (var memory = new MemoryStream())
                {
                    var stream = this.TcpClient.GetStream();
                    while (stream.DataAvailable)
                    {
                        var buffer = new byte[8192];
                        var count = stream.Read(buffer, 0, buffer.Length);
                        if (count > 0)
                        {
                            var data = new byte[count];
                            Array.Copy(buffer, 0, data, 0, count);
                            //Console.WriteLine($"{this.Name} >> " + ToHexString(data));
                            memory.Write(data, 0, data.Length);
                        }
                    }
                    return memory.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Attempt to receive failed.", ex);
            }
        }
    }

}
