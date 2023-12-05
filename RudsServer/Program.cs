using System.Net;
using System.Net.Sockets;

class Program
{
    private static List<Socket> users = new();
    private static Dictionary<Socket, byte[]> soundBuffer = new();
    private static int SendLog = 0;

    public static async Task Main(string[] args)
    {
        System.Timers.Timer bufferSender = new(TimeSpan.FromMilliseconds(20));
        bufferSender.Elapsed += (sender, eventArgs) => SendSoundBuffer();
        bufferSender.Start();
        Socket local = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var getting = new IPEndPoint(IPAddress.Any, 10101);
        local.Bind(getting);
        local.Listen();
        while (true)
        {
            Console.WriteLine("listen");
            var soc = local.Accept();
            users.Add(soc);
            Task.Run(() => Play(soc));
            Console.WriteLine(users.Count + "Connected");
        }
    }

    static void Play(Socket from)
    {
        try
        {
            var raw = new byte[2048];
            int len;
            Console.WriteLine($"{from.Handle} start");
            int log = 0;
            while (from.Connected)
            {
                len = from.Receive(raw);
                if (len == 1 && raw[0] == 255)
                    break;
                var data = raw[..len];

                PublishSound(from, data, log++ % 100 == 0);
            }

            from.Close();
            users.Remove(from);
            Console.WriteLine($"{from.Handle} finished");
            soundBuffer.Remove(from);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    static void PublishSound(Socket socket, byte[] sound, bool log)
    {
        if (!soundBuffer.TryAdd(socket, sound))
            soundBuffer[socket] = sound;
        if (log)
            Console.WriteLine($"Published {socket.Handle}");
    }

    static void SendSoundBuffer()
    {
        try
        {
            int sendCount = 0;
            foreach (var buffer in soundBuffer)
            {
                foreach (var send in soundBuffer)
                {
                    if (send.Key.Handle == buffer.Key.Handle)
                        continue;
                    send.Key.Send(send.Value);
                    sendCount++;
                }
            }

            if (SendLog++ % 75 == 0)
                Console.WriteLine(sendCount);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}