

using Comm_HW_Server;
using System.Net.Security;
using System.Reflection;

Console.WriteLine("Server Begin");
Console.WriteLine("MachinaLabs - Communication Homework");
Console.WriteLine("Ben Stewart - September 2022");
Console.WriteLine("\n\n");

Console.WriteLine("Please enter Port to listen on (default is 2222)");
string value = Console.ReadLine();
int port;
try
{
    port = int.Parse(value); 
}
catch
{
    port = 2222;
}

StlListener listener = new StlListener(port);

Thread t = listener.Begin();

while(t.IsAlive)
    Thread.Sleep(1000);


Console.WriteLine("Process Complete, Press Any key to exit");
Console.ReadKey(); 


