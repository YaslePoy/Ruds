using System.Net;
using System.Net.Sockets;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RudsServer;

public class MishcordNetworker(TimeSpan updateRate) : IDisposable
{
    private CancellationTokenSource CommandBufferToken;
    private Dictionary<Guid, RemoteClient> clients = new Dictionary<Guid, RemoteClient>();


    private TcpListener serverSoc = new(IPAddress.Any, 900);
    public bool sendEnable = true, acceptEnable = true;

    public Queue<string> CommandBuffer = new();
    public Queue<Guid> AppendChanelBuffer = new(), BackwardAppendChanelBuffer = new();
    public Guid currentNewClient;

    public void Start()
    {
        serverSoc.Start();
        Task.Run(CloseHandle);
        CommandBufferToken = new CancellationTokenSource();
        Task.Run(HandleCommandBuffer, CommandBufferToken.Token);
        while (acceptEnable)
        {
            Console.WriteLine("Waiting for connection");
            HandleConnection(serverSoc.AcceptTcpClient());
        }
    }

    void HandleConnection(TcpClient client)
    {
        const int initLen = 128;
        Console.WriteLine($"{client.Client.Handle} connected");
        var initBuffer = new byte[initLen];
        var clientStream = client.GetStream();
        var actualLen = clientStream.Read(initBuffer, 0, 128);
        initBuffer = initBuffer[..actualLen];
        var initMessage = Encoding.UTF8.GetString(initBuffer);
        if (initMessage.StartsWith("new client technical"))
        {
            var id = Guid.NewGuid();
            RemoteClient current = new RemoteClient(clientStream, this, id);
            current.InitBack();
            clients.Add(id, current);
            current.Run();
        }
        else if (initMessage.StartsWith("sound chanel source"))
        {
            var currentId = new Guid(initBuffer[19..(19 + 16)]);
            currentNewClient = currentId;
            clients[currentId].SoundStream = clientStream;
            Console.WriteLine($"{currentId} client sound chanel source connected!");
            AppendChanelBuffer.Clear();
            foreach (var idClient in clients)
            {
                if (idClient.Key == currentId)
                    continue;
                AppendChanelBuffer.Enqueue(idClient.Key);
            }

            BackwardAppendChanelBuffer = new Queue<Guid>(AppendChanelBuffer);
            if (AppendChanelBuffer.Count != 0)
                clients[currentId].Send("generate in chanel"u8.ToArray());
        }
        else if (initMessage.StartsWith("sound chanel resend"))
        {
            var currentId = new Guid(initBuffer[19..(19 + 16)]);
            if (AppendChanelBuffer.Count != 0)
            {
                clients[AppendChanelBuffer.Dequeue()].AppendSoundEndPoint(clientStream, currentId);
                if (AppendChanelBuffer.Count != 0)
                    clients[currentId].Send("generate in chanel"u8.ToArray());
                else
                    clients[BackwardAppendChanelBuffer.Dequeue()].Send("generate in chanel"u8.ToArray());
            }

            else if (BackwardAppendChanelBuffer.Count >= 0)
            {
                clients[currentNewClient].AppendSoundEndPoint(clientStream, currentId);
                if (BackwardAppendChanelBuffer.Count != 0)
                    clients[BackwardAppendChanelBuffer.Dequeue()].Send("generate in chanel"u8.ToArray());
            }
        }
    }

    void HandleCommandBuffer()
    {
        while (true)
        {
            while (CommandBuffer.Count == 0) ;
            var command = CommandBuffer.Dequeue();
        }
    }

    private void CloseHandle()
    {
        while (true)
        {
            if (Console.ReadLine() == "close")
            {
                CommandBufferToken.Cancel();
            }

            break;
        }

        Dispose();
    }

    public void Dispose()
    {
        serverSoc?.Dispose();
        sendEnable = false;
        acceptEnable = false;
        Console.WriteLine("Server disposed");
    }
}

public class RemoteClient(NetworkStream user, MishcordNetworker host, Guid identifier)
{
    public NetworkStream TechnicalStream = user, SoundStream;
    public Guid Id = identifier;

    public Dictionary<Guid, NetworkStream> SoundEndPoints = new();

    private byte[] receiveBuffer = new byte[2048];

    public void Run()
    {
        Task.Run(HandleNetwork);
        Task.Run(HandleSound);
    }

    private void HandleSound()
    {
        var soundInBuffer = new byte[2048];
        var lastRead = 0;
        while (SoundStream is null) ;
        while (SoundStream.Socket.Connected)
        {
            lastRead = SoundStream.Read(soundInBuffer, 0, 2048);
            if(SoundEndPoints.Count == 0)
                continue;
            foreach (var endPoint in SoundEndPoints)
            {
                endPoint.Value.Write(soundInBuffer, 0, lastRead);
            }
        }
    }

    void HandleNetwork()
    {
        Console.WriteLine($"Handling {TechnicalStream} start");

        while (TechnicalStream.Socket.Connected && host.sendEnable)
        {
            try
            {
                host.CommandBuffer.Enqueue(Encoding.UTF8.GetString(Read()));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        Console.WriteLine($"{TechnicalStream} finished");
        TechnicalStream.Dispose();
    }

    public void Send(byte[] data)
    {
        TechnicalStream.Write(data, 0, data.Length);
    }

    byte[] Read()
    {
        var lenBuffer = new byte[4];

        TechnicalStream.ReadExactly(lenBuffer, 0, 4);
        var receiveLen = BitConverter.ToInt32(lenBuffer);

        TechnicalStream.ReadExactly(receiveBuffer, 0, receiveLen);
        return receiveBuffer[..receiveLen];
    }

    public void InitBack()
    {
        TechnicalStream.Write(Id.ToByteArray(), 0, 16);
    }

    public void AppendSoundEndPoint(NetworkStream stream, Guid id)
    {
        SoundEndPoints.Add(id, stream);
    }
}