using System.IO;
using System.Net;
using NAudio.Wave;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Threading;

namespace AudioTestView;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    readonly IPEndPoint ServerLocation = IPEndPoint.Parse(File.ReadAllText("ipconfig.txt"));
    private Guid Id;
    public NetworkStream Technical, SoundOut;
    
    public List<NetworkSoundPlayer> SoundInputs = new();
    private byte counter;
    private Int16 level;
    private byte[] rawSound;
    private bool socketLoaded;
    private int max;
    private int recived;
    private bool isNoize;
    private int inPacks, outPacks;

    public MainWindow()
    {
        InitializeComponent();

        var waveIn = new WaveInEvent
        {
            DeviceNumber = 0, // indicates which microphone to use
            WaveFormat = new WaveFormat(rate: 44100, bits: 16, channels: 1),
            BufferMilliseconds = 20
        };
        waveIn.DataAvailable += WaveIn_DataAvailable;
        waveIn.StartRecording();
        NetworkInit();

        IdTB.Text = Id.ToString();

        DispatcherTimer levelUpdate = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50),
        };
        levelUpdate.Tick += (sender, args) =>
        {
            ClientInd.Value = level;
            Indicator.Text = $"{SetLength(inPacks.ToString(), 8)} {SetLength(outPacks.ToString(), 8)}";

            MaxInd.Value = max;
            MaxIndText.Text = recived.ToString();
            isNoize = NoizeEnable.IsChecked.GetValueOrDefault();
        };
        levelUpdate.Start();
        Task.Run(NetworkTechnical);
    }

    void NetworkInit()
    {
        var technicalTcp = new TcpClient();
        technicalTcp.Connect(ServerLocation);
        Technical = technicalTcp.GetStream();

        TechnicalSend("new client technical");

        var idBuffer = new byte[16];
        Technical.ReadExactly(idBuffer, 0, 16);
        Id = new Guid(idBuffer);

        var soundTcp = new TcpClient();
        soundTcp.Connect(ServerLocation);
        SoundOut = soundTcp.GetStream();
        
        var soundInitData = "sound chanel source"u8.ToArray().Concat(Id.ToByteArray()).ToArray();
        SoundOut.Write(soundInitData);
    }

    void NetworkTechnical()
    {
        var rawMsgBuffer = new byte[128];
        while (true)
        {
            var currentLen = Technical.Read(rawMsgBuffer);
            var message = Encoding.UTF8.GetString(rawMsgBuffer[..currentLen]);
            if (message == "generate in chanel")
            {
                var currentInChanel = new NetworkSoundPlayer();
                currentInChanel.Launch(Id, ServerLocation);
                SoundInputs.Add(currentInChanel);
            }
        }
    }
    
    void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
    {
        rawSound = isNoize ? RandomNumberGenerator.GetBytes(e.Buffer.Length) : e.Buffer;
        SoundOut!.Write(rawSound);
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        Technical.Close();
        SoundOut.Close();
    }

    private static string SetLength(string value, int outLength)
    {
        return new string(' ', outLength - value.Length) + value;
    }

    void TechnicalSend(string message)
    {
        var data = Encoding.UTF8.GetBytes(message);
        Technical.Write(data, 0, data.Length);
    }
}