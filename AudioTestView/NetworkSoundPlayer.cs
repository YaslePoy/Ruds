using System.Net;
using System.Net.Sockets;
using NAudio.Wave;

namespace AudioTestView;

public class NetworkSoundPlayer
{
    private Guid SenderId;
    private short MaxLevel;
    private CancellationTokenSource stopToken;
    public void Launch(Guid clientId, IPEndPoint server)
    {
        stopToken = new CancellationTokenSource();
        SenderId = clientId;
        Task.Run(() => PlayNetwork(clientId, server), stopToken.Token);
    }
    void PlayNetwork(Guid clientId, IPEndPoint server)
    {
        TcpClient catcher = new TcpClient();
        catcher.Connect(server);
        using (var stream = catcher.GetStream())
        {
            BufferedWaveProvider soundStream = new BufferedWaveProvider(new WaveFormat(44100, 16, 1));
            var wo = new WaveOutEvent();
            wo.Init(soundStream);
            wo.Play();
            stream.Write("sound chanel resend"u8.ToArray().Concat(clientId.ToByteArray()).ToArray());
            var receiveBuffer = new byte[1024];
            while (true)
            {
                var received = stream.Read(receiveBuffer);
                soundStream.AddSamples(receiveBuffer, 0, received);
            }
        }
    }

    public void Stop()
    {
        stopToken.Cancel();
    }

    public SoundStatistics GetStats()
    {
        return new SoundStatistics(SenderId, MaxLevel);
    }
    
}

public record SoundStatistics(Guid id, short level);
