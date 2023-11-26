using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace SenderTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            UdpClient sender = new UdpClient(13345);
            IPEndPoint server = new IPEndPoint(IPAddress.Parse("192.168.1.35"), 10101);
            sender.Send(RandomNumberGenerator.GetBytes(512), server);
        }
    }
}
