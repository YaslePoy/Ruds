using System.Net;
using System.Net.Sockets;

namespace TestServer;

class Program
{
    static void Main(string[] args)
    {
        TcpListener listener = new TcpListener(IPEndPoint.Parse("192.168.1.35:901"));
        listener.Start();
        Console.WriteLine("Catching");
        var client = listener.AcceptTcpClient().GetStream();
        Console.WriteLine("Client connected");
        client.CopyTo(client);
    }
}