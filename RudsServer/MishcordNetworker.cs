using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace RudsServer;

public class MishcordNetworker(TimeSpan updateRate) : IDisposable
{
    public readonly TimeSpan sendRate = updateRate;
    public Dictionary<RemoteClient, byte[]> soundBuffer = new();
    private Socket serverSoc = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    public bool sendEnable = true, acceptEnable = true;

    public void Start()
    {
        serverSoc.Bind(new IPEndPoint(IPAddress.Any, 10101));
        serverSoc.Listen();
        Task.Run(SendOutSounds);
        Task.Run(CloseHandle);
        while (acceptEnable)
        {
            Console.WriteLine("Waiting for client");
            var connection = serverSoc.Accept();
            Console.WriteLine($"{connection.Handle} connected");
            var client = new RemoteClient(connection, this);
            soundBuffer.Add(client, null);
            client.Run();
            Console.WriteLine("Client runned");
        }
    }

    private void CloseHandle()
    {
        while (true)
        {
            if (Console.ReadLine() == "close")
                break;
        }

        Dispose();
    }

    void SendOutSounds()
    {
        int sendLocCounter = 0;
        Stopwatch sendTimer = new Stopwatch();
        while (sendEnable)
        {
            sendTimer.Start();
            int sendCount = 0;
            foreach (var buffer in soundBuffer)
            {
                foreach (var send in soundBuffer)
                {
                    if (send.Key.clientPoint.Handle == buffer.Key.clientPoint.Handle)
                        continue;
                    send.Key.clientPoint.Send(buffer.Value);
                    sendCount++;
                }
            }

            sendTimer.Stop();
            var sleepTime = sendRate - sendTimer.Elapsed;
            if (sleepTime > TimeSpan.Zero)
                Thread.Sleep(sleepTime);
            if (sendLocCounter++ % 150 == 0)
                Console.WriteLine(sendCount);
        }
    }

    public void Dispose()
    {
        serverSoc?.Dispose();
        sendEnable = false;
        acceptEnable = false;
        Console.WriteLine("Server disposed");
    }
}

public class RemoteClient(Socket user, MishcordNetworker host)
{
    public Socket clientPoint = user;

    public void Run()
    {
        Task.Run(HandleNetwork);
    }

    void HandleNetwork()
    {
        var netInput = new byte[2048];
        int inputLen, logCounter = 0;

        Console.WriteLine($"Handling {clientPoint.Handle} start");

        while (clientPoint.Connected && host.sendEnable)
        {
            try
            {
                inputLen = clientPoint.Receive(netInput);
                var data = netInput[..inputLen];
                if (data.Length == 1 && data[0] == 255)
                {
                    clientPoint.Disconnect(false);
                    break;
                }

                if (logCounter++ % 100 == 0)
                    Console.WriteLine($"Recived {inputLen} bytes from {clientPoint.Handle}");
                host.soundBuffer[this] = data;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        Console.WriteLine($"{clientPoint.Handle} finished");
        clientPoint.Dispose();
    }
}