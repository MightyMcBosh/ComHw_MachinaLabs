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


        static MD5CryptoServiceProvider md5Hasher = new MD5CryptoServiceProvider();
        static byte[]? file1Hash, file2hash; 


        //keep a reference to the filename string from the UI. 
        private static string? _filename;
        public static byte[] FileData { get; private set; } 
        
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

        /// <summary>
        /// This is straightforward enough - Attempt to make a connection to the server application. \
        /// Send the file over via a basic socket, then recieve back via HTTP.
        /// Compare the two files - if they do not match then the task is no good.
        /// I'm also assuming that the size of the files is not out of the ordinary for an STL file. 
        /// </summary>
        private static async void RunTask()
        {
            bool error = false;
            Step = ProcessStep.Ready; 
            while (!error)
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

                        file1Hash = md5Hasher.ComputeHash(FileData);
                        Debug.WriteLine(file1Hash.ToString());                         
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


                        localSocket.Disconnect(false);
                        localSocket.Dispose();

                        Step = ProcessStep.Waiting; 
                        break;
                    case ProcessStep.Waiting:
                        break;
                    case ProcessStep.ReceivingFile:
                        break;
                    case ProcessStep.Complete:
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
            Step = ProcessStep.Ready; 
        }

        private static StringBuilder bob;

        /// <summary>
        /// Stl byte data - a header of 80 bytes, followed by 4 bytes of an unsigned integer - this says how many triangles exist in the file. 
        /// Proceed to loop through the rest of the file and extract the triangle vertex information. 
        /// </summary>
        static async Task ProcessSTLDataIntoCSV()
        { 
            if (FileData != null && FileData.Length > 84)
            {
                //make it so this can run at the same time as the network transfer, this is a PC and memory is not really a concern. I would do this differently on an RPI or ucontroller.
                byte[] data = new byte[FileData.Length];
                lock(FileData)
                {
                    Array.Copy(FileData,data,FileData.Length);  
                }


                bob = new StringBuilder(); 
                int index = 80;

                uint numTriangles = BitConverter.ToUInt32(FileData, index);
                index += sizeof(uint);
                byte[] chunk = new byte[50]; //size of each triangle - 12 bytes per normal vector, x,y,and z coordinates, and 2 bytes of attributes (unused, according to wikipedia)
                for(int i = 0; i < numTriangles; i++) //Extract all the data from the  file                
                {
                    
                }
            }
        }

        static string GetStringRepresentationOfFile()
        {
            if (bob != null)
                return bob.ToString();
            else
                return ""; 
        }
    }

    
}
