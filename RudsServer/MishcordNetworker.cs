﻿using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace RudsServer;

public class MishcordNetworker(TimeSpan updateRate) : IDisposable
{
    public readonly TimeSpan sendRate = updateRate;
    public Dictionary<RemoteClient, byte[]> soundBuffer = new();
    private TcpListener serverSoc = new TcpListener(IPAddress.Any, 10101);
    public bool sendEnable = true, acceptEnable = true;

    public void Start()
    {
        serverSoc.Start();
        Task.Run(SendOutSounds);
        Task.Run(CloseHandle);
        while (acceptEnable)
        {
            Console.WriteLine("Waiting for client");
            var connection = serverSoc.AcceptTcpClient();
            Console.WriteLine($"{connection.Client.Handle} connected");
            var client = new RemoteClient(connection.GetStream(), this);
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
                    // if (send.Key.clientPoint == buffer.Key.clientPoint)
                    //     continue;
                    if(buffer.Value is null)
                        continue;
                    send.Key.clientPoint.Write(buffer.Value);
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

public class RemoteClient(NetworkStream user, MishcordNetworker host)
{
    public FixedReciver clientPoint = new FixedReciver(user);

    public void Run()
    {
        Task.Run(HandleNetwork);
    }

    void HandleNetwork()
    {
        var logCounter = 0;

        Console.WriteLine($"Handling {clientPoint} start");

        while (clientPoint.Connected && host.sendEnable)
        {
            try
            {
                var data = clientPoint.Read();
                if (data.Length == 1)
                {
                    clientPoint.Close();
                    break;
                }

                host.soundBuffer[this] = data;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        Console.WriteLine($"{clientPoint} finished");
        clientPoint.Dispose();
    }
}

public class FixedReciver(NetworkStream stream) : IDisposable, IAsyncDisposable
{
    private NetworkStream ns = stream;
    public void Write(byte[] data)
    {
        ns.Write(Prepair(data));
    }

    public byte[] Read()
    {
        var msgLen = new byte[4];
        ns.Read(msgLen);
        var reciveLen = BitConverter.ToInt32(msgLen);
        var msg = new byte[reciveLen];
        for (int i = 0; i < reciveLen; i++)
        {
            msg[i] = (byte)ns.ReadByte();
        }

        return msg;
    }

    public override string ToString()
    {
        return ns.Socket.Handle.ToString();
    }

    public void Dispose()
    {
        ns.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await ns.DisposeAsync();
    }

    public void Close()
    {
        ns.Close();
    }

    public bool Connected => ns.Socket.Connected;

    public static bool operator ==(FixedReciver fr1, FixedReciver fr2) =>
        fr1.ns.Socket.Handle == fr2.ns.Socket.Handle;

    public static bool operator !=(FixedReciver fr1, FixedReciver fr2) => !(fr1 == fr2);
    
    public static byte[] Prepair(byte[] raw)
    {
        var send = new byte[4 + raw.Length];
        BitConverter.GetBytes(raw.Length).CopyTo(send, 0);
        raw.CopyTo(send, 4);
        return send;
    }
}