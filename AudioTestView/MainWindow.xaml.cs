using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using NAudio.Wave;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using RudsServer;

namespace AudioTestView;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private byte counter;
    private FixedReciver server;
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
        Task.Run(NetworkHandle);

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

        System.Timers.Timer sendOutTimer = new(TimeSpan.FromMicroseconds(20));
        sendOutTimer.Elapsed += (o, e) =>
        {
            if (server is not null && !server.Connected || rawSound == null)
                return;
            server?.Write(rawSound);
            outPacks++;
        };
        sendOutTimer.Start();
    }

    void NetworkHandle()
    {
        var cl = new TcpClient();
        cl.Connect(IPEndPoint.Parse(File.ReadAllText("ipconfig.txt")));
        server = new FixedReciver(cl.GetStream());

        BufferedWaveProvider ms = new BufferedWaveProvider(new WaveFormat(44100, 16, 1));
        var wo = new WaveOutEvent();
        wo.Init(ms);
        wo.Play();
        byte[] buffed;
        while (server.Connected)
        {
            buffed = server.Read();
            var values = new short[buffed.Length / 2];
            Buffer.BlockCopy(buffed, 0, values, 0, buffed.Length);
            max = values.Max();
            recived++;
            inPacks++;
            try
            {
                while (!ms.ReadFully) ;

                ms.AddSamples(buffed, 0, buffed.Length);
            }
            catch (Exception e)
            {
                ms.ClearBuffer();
                MessageBox.Show(e.ToString());
            }
        }
    }

    void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
    {
        Int16[] values = new Int16[e.Buffer.Length / 2];
        rawSound = isNoize ? RandomNumberGenerator.GetBytes(e.Buffer.Length) : e.Buffer;
        Buffer.BlockCopy(rawSound, 0, values, 0, e.Buffer.Length);

        level = values.Max();
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        server.Write(new byte[] { 255 });
        server.Close();
    }

    private static string SetLength(string value, int outLength)
    {
        return new string(' ', outLength - value.Length) + value;
    }
}