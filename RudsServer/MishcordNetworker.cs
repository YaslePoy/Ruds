using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace RudsServer;

public class MishcordNetworker(TimeSpan updateRate) : IDisposable
{
    public readonly TimeSpan sendRate = updateRate;
    public Dictionary<RemoteClient, byte[]> soundBuffer = new();
    private Socket serverSoc = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private bool sendEnable = true;

    void Start()
    {
        serverSoc.Bind(new IPEndPoint(IPAddress.Any, 10101));
        serverSoc.Listen();
        Task.Run(SendOutSounds);
        while (true)
        {
            var connection = serverSoc.Accept();
            var client = new RemoteClient(connection, this);
            soundBuffer.Add(client, null);
            client.Run();
        }
    }

    void SendOutSounds()
    {
        int sendLocCounter = 0;
        Stopwatch sendTimer = new Stopwatch();
        while (sendEnable)
        {
            int sendCount = 0;
            sendTimer.Start();
            foreach (var buffer in soundBuffer)
            {
                foreach (var send in soundBuffer)
                {
                    if (send.Key.end.Handle == buffer.Key.end.Handle)
                        continue;
                    send.Key.end.Send(send.Value);
                    sendCount++;
                }
            }

            sendTimer.Stop();
            var sleepTime = sendRate - sendTimer.Elapsed;
            if (sleepTime > TimeSpan.Zero)
                Thread.Sleep(sleepTime);
            if (sendLocCounter++ % 100 == 0)
                Console.WriteLine(sendCount);
        }
    }

    public void Dispose()
    {
        serverSoc?.Dispose();
        sendEnable = false;
        Console.WriteLine("Server disposed");
    }
}

public class RemoteClient
{
    private readonly MishcordNetworker host;
    public Socket end;

    public RemoteClient(Socket user, MishcordNetworker host)
    {
        end = user;
        this.host = host;
    }

    public void Run()
    {
        Task.Run(HandleNetwork);
    }

    void HandleNetwork()
    {
        var netInput = new byte[2048];
        int inputLen = 0, logCounter = 0;

        Console.WriteLine($"Handling {end.Handle} start");

        while (end.Connected)
        {
            try
            {
                inputLen = end.Receive(netInput);
                var data = netInput[..inputLen];
                if (data.Length == 1 && data[0] == 255)
                {
                    end.Disconnect(false);
                    end.Dispose();
                    break;
                }

                if (logCounter++ % 100 == 0)
                    Console.WriteLine($"Recived {inputLen} bytes from {end.Handle}");
                host.soundBuffer[this] = data;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}