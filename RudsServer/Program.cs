using System.Net;
using System.Net.Sockets;
using RudsServer;

class Program
{
    public static void Main(string[] args)
    {
        MishcordNetworker server = new MishcordNetworker(TimeSpan.FromMilliseconds(20));
        server.Start();
    }
}