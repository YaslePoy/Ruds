using System.Net;
using System.Net.Sockets;
using NAudio.Wave;

class Program
{
    private static List<Socket> users = new();
    public static async Task Main(string[] args)
    {
        Socket local = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        local.Bind(new IPEndPoint(IPAddress.Any, 10101));
        local.Listen();
        while (true)
        {
            Console.WriteLine("listen");
            var soc = local.Accept();
            users.Add(soc);
            Task.Run(() => Play(soc));
        }
    }

    static void Play(Socket from)
    {
        BufferedWaveProvider ms = new BufferedWaveProvider(new WaveFormat(44100, 16, 1));
        var wo = new WaveOutEvent();
        wo.Init(ms);
        wo.Play();
        var raw = new byte[2048];
        int len = 0;
        Console.WriteLine($"{from.Handle} start");
        while (from.Connected)
        {
            // Console.WriteLine($"{from.Handle} reciving");
            len = from.Receive(raw);
            // Console.WriteLine($"{from.Handle} recived {len}");
            if (len == 1 && raw[0] == 255)
                break;
            var data = raw[..len];

            // ms.AddSamples(data, 0, len);
            SendToAll(data, from.Handle);
        }
        from.Close();
        users.Remove(from);
        Console.WriteLine($"{from.Handle} finished");
    }
    static void SendToAll(byte[] sound, IntPtr skip)
    {
        foreach (var toSend in users)
        {
            // if(toSend.Handle.Equals(skip))
            // {
            //     Console.WriteLine($"Not sended to {skip}");
            //     continue;
            // }
            toSend.SendAsync(sound);
        }
    }
}