using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using NAudio.Wave;

class Program
{
    public static async Task Main(string[] args)
    {
        Socket local = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        local.Bind(new IPEndPoint(IPAddress.Any, 10101));
        local.Listen();
        while (true)
        {
            Console.WriteLine("listen");
            var soc = local.Accept();
            Task.Run(() => Play(soc));
            // Console.WriteLine("Listening");
            // var msg = (await listener.ReceiveAsync()).Buffer;
            // var strings = msg.Chunk(8);
            // foreach (var str in strings)
            // {
            //     Console.WriteLine(string.Join("", str.Select(i=> $"{i, 4}")));
            // }
            // ms.AddSamples(msg, 0, msg.Length);
        }
    }

    static void Play(Socket from)
    {
        BufferedWaveProvider ms = new BufferedWaveProvider(new WaveFormat(44100, 16, 1));
        var wo = new WaveOutEvent();
        wo.Init(ms);
        wo.Play();
        var data = new byte[2048];
        int len = 0;
        while (true)
        {
            len = from.Receive(data);
            ms.AddSamples(data, 0, len);
        }
    }
}