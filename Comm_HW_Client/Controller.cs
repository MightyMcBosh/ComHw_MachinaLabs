using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets; 
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Dynamic;

namespace Comm_HW_Client
{
    public delegate void ProcessFault(ProcessStep step, string args);
    public delegate void ProcessStepChange(ProcessStep newProcessStep); 

    
    public enum ProcessStep
    {
        Ready,
        ReadingFile,
        SendingFile,
        Waiting, 
        ReceivingFile,
        ComparingFiles,
        Complete,
        Error,
    }
    /// <summary>
    /// Basic MVC architecture - offload the major logic into a controller class. this manages the process to talk back to
    /// the UI whil it's attempting to read and write the file. 
    /// </summary>
    public static class Controller
    {

        public static  void Init()
        {
            Step = ProcessStep.Error; 
        }
        private static Thread taskThread;

        private static FileStream fs;
        
        public static event ProcessFault? OnProcessFault;
        public static event ProcessStepChange? OnProcessStepChange; 
        private static ProcessStep _step; 
        public static ProcessStep Step
        {
            get=>_step; 
            private set
            {
                if(_step != value)
                {
                    _step = value;
                    OnProcessStepChange?.Invoke(value); 
                }
            }
        }

        static MD5 md5 = MD5.Create(); 

        //keep a reference to the filename string from the UI. 
        private static string? _filename;
        public static byte[] FileData { get; private set; }
        public static byte[] ReturnedData { get; private set; }
        //helper method to run this process asynchronously. 
        public static bool StartTask(string filename)
        {
            _filename = filename;
            if(Step == ProcessStep.Ready || Step == ProcessStep.Error)
            {
                taskThread = new Thread(RunTask);
                taskThread.Start();
                return true; 
            }
            return false; 
        }
        public static void AbortTask()
        {
            if (taskThread != null)
            {
                taskThread.Abort();
            }
        }

        /// <summary>
        /// This is straightforward enough - Attempt to make a connection to the server application. \
        /// Send the file over via a basic socket, then recieve back via HTTP.
        /// Compare the two files - if they do not match then the task is no good.
        /// I'm also assuming that the size of the files is not out of the ordinary for an STL file. 
        /// </summary>
        private static async void RunTask()
        {
            bool error = false, complete = false;
            Step = ProcessStep.Ready; 
            while (!error && ! complete)
            {
                switch (Step)
                {
                    case ProcessStep.Ready:
                        try
                        {
                            fs = new FileStream(_filename,FileMode.Open);
                            Step = ProcessStep.ReadingFile; 
                        }
                        catch
                        {
                            OnProcessFault?.Invoke(Step, "Error Opening File");
                            Step = ProcessStep.Error; 
                        }
                        break;
                case ProcessStep.ReadingFile:
                        long read = 0;
                        long bytesToRead = fs.Length;
                        FileData = new byte[bytesToRead];  
                        while (read < fs.Length)
                        {
                            //we can reasonably expect that we won't get any overflow here
                            int bytesRead = fs.Read(FileData, (int)read, (int)bytesToRead);

                            read += bytesRead;
                            bytesToRead -= read; 
                        }
                        fs.Close();
                        fs.Dispose();

                                          
                        Step = ProcessStep.SendingFile; 
                        break;
                    case ProcessStep.SendingFile:
                        IPEndPoint local = new IPEndPoint(IPAddress.Loopback, 2223);
                        IPEndPoint remote = new IPEndPoint(IPAddress.Loopback, 2222);
                        Socket localSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
                        localSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true); 

                        try
                        {                            
                            localSocket.Bind(local);
                            localSocket.Connect(remote);
                        }
                        catch (Exception)
                        { 
                            localSocket.Dispose(); 
                            Step = ProcessStep.Error;

                            break; 
                        }

                        localSocket.SendTo(BitConverter.GetBytes(FileData.Length), remote);

                        int bytesSent = 0; 
                        while(bytesSent < FileData.Length)
                        {
                            int tmp = localSocket.Send(FileData, bytesSent, FileData.Length - bytesSent, SocketFlags.None);
                            bytesSent += tmp;
                            Thread.Sleep(10); 
                        }


                        localSocket.Close(); 
                        localSocket.Dispose();

                        Step = ProcessStep.Waiting; 
                        break;
                    case ProcessStep.Waiting:
                        //Begin 
                        HttpListener? listener = new HttpListener();
                        listener.Prefixes.Add("http://localhost:8005/");
                        
                        listener.Start();
                        ReturnedData = null; 
                        var context = listener.GetContext(); //blocking until the connection is made; 
                        var req = context.Request;
                        if (req.Url.Segments[req.Url.Segments.Length - 1] == "stl" && req.HttpMethod == "POST") 
                        {
                            ReturnedData = new byte[req.ContentLength64];
                            bytesToRead = (int)req.ContentLength64;
                            int readData = 0;
                            while(bytesToRead > 0)
                            {
                               int tmp =  req.InputStream.Read(ReturnedData, readData, (int)bytesToRead);
                               bytesToRead -= tmp;
                               readData += tmp; 
                            }
                            
                        }

                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        context.Response.Close();
                        if(ReturnedData != null)
                        {
                            Debug.WriteLine($"read file: {ReturnedData.Length} bytes");

                        }
                        else
                        {
                            Debug.WriteLine($"File not read");
                            Step = ProcessStep.Error;
                            break; 
                        }
                        byte[] hash1 = md5.ComputeHash(FileData);
                        byte[] hash2 = md5.ComputeHash(ReturnedData);

                        bool ok1 = hash1.SequenceEqual(hash2);

                        if (false)//for debug
                        {
                            fs = new FileStream($"Comparison_{DateTime.Now.ToString("ddMMyy_hhmmss")}.csv", FileMode.CreateNew);

                            for (int i = 0; i < FileData.Length; i++)
                            {
                                fs.Write(Encoding.UTF8.GetBytes($"{FileData[i]},{ReturnedData[i]},{FileData[i] - ReturnedData[i]}\n"));
                            }
                            fs.Close();
                            fs.Dispose();  
                        }

                        if (ok1)
                            Step = ProcessStep.Complete;
                        else
                            Step = ProcessStep.Error; 
                        break;
                    case ProcessStep.ReceivingFile:
                       

                        break;
                    case ProcessStep.Complete:
                        complete = true; 
                        break;
                    case ProcessStep.Error:
                        error = true;
                        if (fs != null)
                        {
                            fs.Dispose();
                        }
                        break;               
                }
                Thread.Sleep(50);                 
            }
        }
    }
}
