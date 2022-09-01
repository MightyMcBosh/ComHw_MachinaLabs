using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets; 
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Diagnostics;

namespace Comm_HW_Server
{
    public delegate void DoAThing(); 
    //helper class cause I'm lazy and like my code wrapped up
    public class Triangle
    {

        float[] vector, vertex1, vertex2, vertex3;
        ushort stuff;

        public Triangle(byte[] initializer)
        {



            if (initializer == null || initializer.Length != 50)
            {
                throw new ArgumentOutOfRangeException(); 
            }

            vector = new float[3];
            vertex1 = new float[3];
            vertex2 = new float[3];
            vertex3 = new float[3];

            int index = 0;
            for (int i = 0; i < 3; i++)
            {
                vector[i] = BitConverter.ToSingle(initializer, index);
                index += sizeof(float); 
            }
            for (int i = 0; i < 3; i++)
            {
                vertex1[i] = BitConverter.ToSingle(initializer, index);
                index += sizeof(float);
            }
            for (int i = 0; i < 3; i++)
            {
                vertex2[i] = BitConverter.ToSingle(initializer, index);
                index += sizeof(float);
            }
            for (int i = 0; i < 3; i++)
            {
                vertex3[i] = BitConverter.ToSingle(initializer, index);
                index += sizeof(float);
            }
            stuff = BitConverter.ToUInt16(initializer, index);  
        }

     
        public override string ToString()
        {
            StringBuilder bob = new StringBuilder(); 
            //for(int i = 0; i < 3; i++) //Not needed for the challenge
            //{
            //    bob.AppendLine(vector[i].ToString("D4")+ ",");
            //}
            for (int i = 0; i < 3; i++)
            {
                bob.Append(vertex1[i].ToString("F4") + ",");
            }
            bob.Append('\n');
            for (int i = 0; i < 3; i++)
            {
                bob.Append(vertex2[i].ToString("F4") + ",");
            }
            bob.Append('\n');
            for (int i = 0; i < 3; i++)
            {
                bob.Append(vertex3[i].ToString("F4") + ",");
            }
            bob.Append('\n');


            return bob.ToString(); 
        }
    }
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

        public Thread Begin()
        {
            Thread t = new Thread(RunCommunicationTask);
                t.Start();
            return t; 
        }

        public void RunCommunicationTask()
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
                int recvd = _client.Receive(recvBuffer, dataRecvd, (int)dataToRead, SocketFlags.None); 
                dataRecvd += recvd;
                dataToRead -= (uint)recvd;

                if (recvd == 0)
                    Console.WriteLine("socket not responding"); 
            }

            _server.Close();
            _server.Dispose(); 
            Console.WriteLine("Reading File Complete");
            Console.WriteLine("Converting STL to CSV...");
            // for the bonus challenge, i am assuming that the STL file in question is thebinary version
            //Read and ingnore 80 bytes - this is the header that doesn't contain any useful information to me. 
            //read 4 bytes into a UINT32 - this is how many triangles are in the file
            //the remaining file length is going to be 50 * that numberof the
            //12 bytes for the triangle vector in space
            //12 bytes each for the x,y, and z coordinates of the 3 vertices of the triangle
            //2 more bytes as configuration data we dont need

            uint index = 80;
            uint numberTriangles = BitConverter.ToUInt32(recvBuffer,(int)index);
            index += sizeof(uint);

            List<Triangle> list = new List<Triangle>();
            byte[] tmp = new byte[50];  
            for(int i = 0; i < numberTriangles; i++)
            {
                Array.Copy(recvBuffer, index, tmp, 0, 50);
                try
                {
                    list.Add(new Triangle(tmp));
                }
                catch (ArgumentOutOfRangeException e)
                {
                    Console.WriteLine($"data is wrong size - ");                    
                }
                index += 50; 
            }

            Debug.WriteLine($"Triangle Count = {list.Count}");
            string fn = $"output_{DateTime.Now.ToString("ddMMyy_hhmmss")}.stl";
            FileStream fs = new FileStream(fn, FileMode.CreateNew);

            fs.Write(recvBuffer);
            fs.Close();
            fs.Dispose();

            Console.WriteLine($"File saved as {fn}");
            string fn_csv = $"CSVoutput_{DateTime.Now.ToString("ddMMyy_hhmmss")}.csv";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("X Coordinate, Y Coordinate, Z Coordinate");
            foreach(Triangle t in list)
            {
                builder.Append(t.ToString()); 
            }


            fs = new FileStream(fn_csv, FileMode.CreateNew); 
            fs.Write(Encoding.UTF8.GetBytes(builder.ToString()));
            fs.Close();
            fs.Dispose();
            Console.WriteLine("CSV Written To Disk");

            Console.WriteLine("Returning file to Task A");
            //Open HTTP Client to post the file back to the listener
            HttpClient client = new HttpClient();
            try
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                var t = client.PostAsync("http://localhost:8005/stl", new ByteArrayContent(recvBuffer));
                t.Wait();
                Console.WriteLine($"Task A responded with {t.GetAwaiter().GetResult().StatusCode}");

                }
            catch (Exception)
            {

                Console.WriteLine("Task A not Responding");
            }
            client.CancelPendingRequests();
            client.Dispose(); 

            Console.WriteLine("Task Complete, Check root directory for STL and CSV files.");
          
       



        }
    }
}

