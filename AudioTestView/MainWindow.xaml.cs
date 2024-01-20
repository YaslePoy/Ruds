using System.IO;
using System.Net;
using NAudio.Wave;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Controls;
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
    private short level;
    private byte[] rawSound;
    private short[] compressed;
    private bool socketLoaded;
    private int max;
    private int recived;
    private bool isNoize;
    private int inPacks, outPacks;
    private WaveInEvent recorder;
    public MainWindow()
    {
        InitializeComponent();
        for (int i = -1; i < WaveIn.DeviceCount; i++)
        {
            var caps = NAudio.Wave.WaveIn.GetCapabilities(i);
            MicroCB.Items.Add(caps.ProductName);
        }
        NetworkInit();

        IdTB.Text = Id.ToString();

        DispatcherTimer levelUpdate = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50),
        };
        levelUpdate.Tick += (sender, args) =>
        {
            ClientInd.Value = level;
            isNoize = NoizeEnable.IsChecked.GetValueOrDefault();
            ConnectionViewer.ItemsSource = null;
            ConnectionViewer.ItemsSource = SoundInputs.Select(i => i.GetStats());
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
        if (compressed is null)
            compressed = new short[e.Buffer.Length / 2]; 
        
        Buffer.BlockCopy(e.Buffer, 0, compressed, 0, e.Buffer.Length);
        level = compressed.Max();
        // level = (short)(++level % 5000);
        rawSound = isNoize ? /*RandomNumberGenerator.GetBytes(e.Buffer.Length)*/ new byte[e.Buffer.Length] : e.Buffer;
        SoundOut.Write(rawSound);
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

    private void MicroCB_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if(MicroCB.SelectedIndex == 0)
            return;
        if(recorder is not null)
            recorder.StopRecording();
        recorder = new WaveInEvent
        {
            DeviceNumber = MicroCB.SelectedIndex - 2,
            WaveFormat = new (rate: 44100, bits: 16, channels: 1),
            BufferMilliseconds = 20
        };
        recorder.DataAvailable += WaveIn_DataAvailable;
        recorder.StartRecording();
    }
}