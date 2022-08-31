using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets; 
using System.Threading.Tasks;

namespace Comm_HW_Server
{
    /// <summary>
    /// I could just use a TcpListener, but this way I can show that I at least understand to some degree what's going on under the hood.
    /// 
    /// Listens for the connection from the client, waits for the data, accepts it, then closes the socket. Processes it into a csv. 
    /// 
    /// Next, opens an HTTP connection and posts the STL file back to the client. Then Posts the CSV. 
    /// </summary>
    public class StlListener
    {
        IPEndPoint host;
        IPEndPoint remote;
        Socket _server;
        int port;

        public StlListener(int port)
        {
            host = new IPEndPoint(IPAddress.Any, port);
            _server = new Socket(SocketType.Stream,ProtocolType.Tcp);
            _server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _server.Bind(host); 
        }

        public void Begin()
        {
            new Thread(RunSocket).Start(); 
        }

        public void RunSocket()
        {
            Console.WriteLine("Socket Created, Waiting For Connection");
            _server.Listen(); 
            Socket _client = _server.Accept();

            Console.WriteLine("Socket Connected!");
            bool done = false;


            byte[] recvBuffer = new byte[4];
            
            while(_client.Available < 4)
            { 
                Thread.Sleep(10); 
            }
            _client.Receive(recvBuffer);

            uint dataToRead = BitConverter.ToUInt32(recvBuffer, 0);
            recvBuffer = new byte[dataToRead];
            Console.WriteLine($"INcoming file: {dataToRead} bvtes");
            int dataRecvd = 0; 
            while (dataToRead > 0)
            {
                int recvd = _client.Receive(recvBuffer);
                dataRecvd += recvd;
                dataToRead -= (uint)recvd;

                if (recvd == 0)
                    Console.WriteLine("socket not responding"); 
            }

            _server.Close(); 
            Console.WriteLine("Reading File Complete"); 
        }
    }
}
