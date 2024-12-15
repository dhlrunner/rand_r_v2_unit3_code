#define RECV_BUFFER_SIZE 128

#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include <fcntl.h>
#include <stdio.h>
#include <dlfcn.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <stdlib.h>
#include <signal.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <pthread.h>

int g_obvio=0;
#define DPRINTF(format, args...)	if (!g_obvio) { g_obvio=1; fprintf(stderr, format, ## args); g_obvio=0; }

#ifndef RTLD_NEXT
#define RTLD_NEXT ((void *) -1l)
#endif

#define REAL_LIBC RTLD_NEXT

typedef void (*sighandler_t)(int);

static int data_w_fd = -1, hook_fd = -1, data_r_fd = -1;

int serverSocket = -1;
unsigned int g_listeningPort = 12345;

void* listning_thread(void* arg);

//Ctrl-Cハンドラー
void exitHandler(int);

//初期化
static void _libhook_init() __attribute__ ((constructor));
static void _libhook_init() {   
	
	signal(SIGINT, exitHandler);
	
	printf("[] Hooking!\n");
	//ソケット作成
	printf("Creating Socket\n");
	serverSocket = socket(AF_INET, SOCK_STREAM, 0);
	if (serverSocket == -1)
	{
		perror("Error: Failed to create socket\n");
		return;
	}
	struct sockaddr_in serverAddr;
	memset(&serverAddr, 0, sizeof(serverAddr));
	serverAddr.sin_family = AF_INET;
	serverAddr.sin_addr.s_addr = htonl(INADDR_ANY); //すべてのネットワークIFから接続許可
	serverAddr.sin_port = htons(g_listeningPort);
	if (bind(serverSocket, (struct sockaddr *)&serverAddr, sizeof(serverAddr)) == -1)
	{
		perror("Error: Failed to bind socket\n");
		close(serverSocket);
		return;
	}
	
	//待機
	if (listen(serverSocket, 5) == -1)
	{
		perror("Error: Failed to listen on socket\n");
		close(serverSocket);
		return;
	}
	
	//イベントスレッド
	pthread_t ptid; 
	pthread_create(&ptid, NULL, &listning_thread, NULL); 
	printf("Event listning thread created. %d\n", ptid);
}

ssize_t write (int fd, const void *buf, size_t count);
void free (void *buf);

//カラー付きでプリント
void println_c(const char *format, ...){
	va_list args;
    va_start(args, format);
	
	DPRINTF("\x1B[41m");
	DPRINTF(format, args);
	DPRINTF("\x1B[0m\n");
	
	va_end(args);
}

//MIDIデータ送信関数のフック
void psqMidiSendImmediateData(void* param_1,int param_2,unsigned char* param_3, int length){
	
	println_c ("HOOK: libOkdplayer.so psqMidiSendImmediateData() hook ");
	printf ("HOOK: 	p1=%d, p2=%d, p3=0x%p, p4=%d\n", param_1, param_2, param_3, length);
	
	static void (*org_func) (void*,int,unsigned char*,int) = NULL;
	if (!org_func)
		org_func = (void (*) (void*,int,unsigned char*,int)) dlsym (REAL_LIBC, "psqMidiSendImmediateData");
	
	org_func(param_1, param_2, param_3, length);

	return;
}

void* listning_thread(void* arg){
    unsigned char buffer[RECV_BUFFER_SIZE];
	while(1){
		struct sockaddr_in clientAddr;
		socklen_t clientAddrLen = sizeof(clientAddr);
		int clientSocket = accept(serverSocket, (struct sockaddr *)&clientAddr, &clientAddrLen);
		if (clientSocket == -1)
		{
			perror("Error: Failed to accept connection\n");
			close(serverSocket);
			return;
		}
		printf("[+] Client connected: %s:%d\n", inet_ntoa(clientAddr.sin_addr), ntohs(clientAddr.sin_port));
		
		
		while(1){
			//STXチェック
			int n = recv(clientSocket, buffer, 1, 0);
			if (n <= 0) {
				perror("[E] Data receive failed or connection closed");
				break;
			}
			
			if(buffer[0] != 0xFF){
				printf("[E] Unexpected first byte: %x\n", buffer[0]);
				break;
			}
			
			//2バイト目(データ長さ)
			n = recv(clientSocket, buffer, 1, 0);
			if (n <= 0) {
				perror("[E] Data receive failed or connection closed");
				break;
			}
			
			unsigned char dataLength = buffer[0];
			if (dataLength > 0 && dataLength < RECV_BUFFER_SIZE) {
				//MIDIデータ受信
				n = recv(clientSocket, buffer, dataLength, 0);
				if (n <= 0) {
					perror("[E] Data receive failed or connection closed");
					break;
				}				
				unsigned char port = buffer[1];
				psqMidiSendImmediateData(NULL,port,buffer+2,(unsigned short)dataLength-2);	
			}
		}	
		//ソケット閉じる
		close(clientSocket);
		printf("[-] Client disconnected\n");
	}
	return;
}

//Ctrl-C ハンドラー
void exitHandler(int dummy)
{
  printf("[-] Closing socket\n");
  close(serverSocket);
  exit(dummy);
}