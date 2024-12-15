using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Core;
using System.Net;

namespace miditoTcp
{
    class Program
    {
        static InputDevice[] InputDevices = null;

        static string ipAddr = "172.27.11.79";
        static int port = 12345;

        static TcpClient tcpClient = null;
        static NetworkStream stream = null;

        static TcpCommunicator tcp = null;

        static (string devName, byte portNum)[] inputDev = new (string, byte)[]{ 
            ("MMT-TG A", 2),
            ("MMT-TG B", 3),
            ("MMT-TG C", 4),
            ("MMT-TG D", 5)
        };

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            InputDevices = new InputDevice[inputDev.Length];
            
            for (int i = 0; i < inputDev.Length; i++)
            {
                int portIndex = i;
                InputDevices[i] = InputDevice.GetByName(inputDev[i].devName);
                InputDevices[i].EventReceived += (sender, e) => {
                    var midiDevice = (MidiDevice)sender;
                    MidiEventToBytesConverter x = new MidiEventToBytesConverter();
                    
                    byte[] data = x.Convert(e.Event);
                    if(data.Length > 0)
                    {
                        byte[] newdata = null; 
                        if (data[0] == 0xf0)
                        {
                            //SysExの場合 -1
                            //STX + Size + F5 + Port = 4
                            newdata = new byte[(data.Length - 1) + 4];
                            newdata[0] = 0xFF;
                            newdata[1] = (byte)((data.Length - 1) + 2); //length
                            newdata[2] = 0xF5;
                            newdata[3] = inputDev[portIndex].portNum;
                            newdata[4] = 0xF0;
                            for (int j = 2; j < data.Length; j++)
                            {
                                newdata[j + 3] = data[j];
                            }
                            
                        }
                        else
                        {
                            //STX + Size + F5 + Port = 4
                            newdata = new byte[(data.Length) + 4];
                            newdata[0] = 0xFF;
                            newdata[1] = (byte)(data.Length + 2); //length
                            newdata[2] = 0xF5;
                            newdata[3] = inputDev[portIndex].portNum;
                            for (int j = 0; j < data.Length; j++)
                            {
                                newdata[j + 4] = data[j];
                            }
                        }

                        try
                        {
                            tcp.Send(newdata);
                        }
                        catch(InvalidOperationException)
                        {
                            Console.WriteLine("Error!");
                            tcp = new TcpCommunicator(ipAddr, port);
                        }
                        catch (System.IO.IOException)
                        {
                            Console.WriteLine("Error!");
                            tcp = new TcpCommunicator(ipAddr, port);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error: Empty data");
                    }

                };
                InputDevices[i].StartEventsListening();
                Console.WriteLine($"Opened MIDI Device {InputDevices[i].Name} ({InputDevices[i].Id}) Port: {inputDev[i].portNum}");
            }

            tcp = new TcpCommunicator(ipAddr, port);

            Console.WriteLine($"Connected to {ipAddr}:{port}");
            Console.WriteLine("Press enter to exit");
            Console.ReadLine();

            for (int i = 0; i < inputDev.Length; i++) {
                InputDevices[i].Dispose();
            }

            tcp.Dispose();
            
           
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            //inputDeviceA.Dispose();    
            //inputDeviceB.Dispose();
            tcp.Dispose();
            //stream.Close();
            //tcpClient.Close();
            Console.WriteLine("exit");
        }
    }
}
