using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text.RegularExpressions;

namespace NodeAddons
{
    class Program
    {
        static TcpListener listener;
        static int port = 8899;
        static string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        static void Main(string[] args)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            // 启用服务器线程
            new Thread(new ThreadStart(StartServer)).Start();

            Console.WriteLine("Http server run at {0}.", port);
        }

        // Http 服务器
        static void StartServer()
        {
            while(true)
            {
                // 这里会阻塞线程，直到接受到一个请求
                Socket socket = listener.AcceptSocket();
                // 将请求单独开一个线程处理；while(true)会回到等待下一个请求状态，周而复始
                new Thread(new ParameterizedThreadStart(HandleRequest)).Start(socket);
            }
            
        }

        // 处理一个请求
        static void HandleRequest(object args)
        {
            Socket socket = (Socket)args;
            byte[] receive = new byte[1024];
            socket.Receive(receive, receive.Length, SocketFlags.None);
            string httpRawTxt = Encoding.ASCII.GetString(receive);

            // 通过 stdio(Console.WriteLine) 实现与 node.js 通讯
            // ## 开头、结尾，方便区分这个条输出是给 node.js 通讯用的
            Console.WriteLine("##" + httpRawTxt + "##");
            SendToBrowser(ref socket, now);
        }

        // 发送数据
        static void SendToBrowser(ref Socket socket, string body)
        {
            string header = "HTTP/1.1 200 OK\r\n"
                + "Content-Type: text/html\r\n"
                + "Content-Length: " + body.Length + "\r\n"
                + "Access-Control-Allow-Origin: *\r\n" // 支持跨域
                + "\r\n"; // 响应头与响应体分界
            byte[] data = Encoding.ASCII.GetBytes(header + body);

            if (socket.Connected)
            {
                int res = socket.Send(data, data.Length, SocketFlags.None);
                if (res == -1)
                {
                    Console.WriteLine("Socket Error cannot Send Packet.");
                }
                else
                {
                    Console.WriteLine(">> [{0}]", now);
                }
                socket.Close();
            }
        }
    }
}
