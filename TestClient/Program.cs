using System.Net.Sockets;
using System.Security.Cryptography;

namespace TestClient;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, TCP!");
        TcpClient tcp = new TcpClient();
        tcp.Connect("217.66.19.16", 901);
        var ns = tcp.GetStream();
        var input = new byte[16];
        for (int i = 0; i < 10; i++)
        {
            ns.Write(RandomNumberGenerator.GetBytes(16));
            ns.ReadExactly(input, 0, 16);
            Console.WriteLine(string.Join(", ", input.Select(i => $"{i, 4}")));
        }
    }
}