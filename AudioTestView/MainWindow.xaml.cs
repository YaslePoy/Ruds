using System.Collections;
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
public partial class MainWindow : Window
{
    private byte counter;
    private FixedReciver server;
    private Int16[] levels = new short[882];
    private byte[] rawSound;
    private bool socketLoaded;
    private int max;
    private int recived;
    private bool isNoize;
    private int inPacks, outPacks;

    public MainWindow()
    {
        InitializeComponent();
        int cols = 882;
        for (int i = 0; i < cols; i++)
        {
            TestGrid.ColumnDefinitions.Add(new ColumnDefinition());
            var vertical = new Rectangle { Fill = new SolidColorBrush(Colors.Chocolate), StrokeThickness = 0 };
            TestGrid.Children.Add(vertical);
            Grid.SetColumn(vertical, i);
        }

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
            double total = TestGrid.ActualHeight;
            for (int i = 0; i < levels.Length; i++)
            {
                (TestGrid.Children[i] as Rectangle)!.Height = Math.Max(total * levels[i] / 32768 * 2, 0);
            }

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
        try
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
                    ms.AddSamples(buffed, 0, buffed.Length);
                }
                catch (Exception e)
                {
                    ms.ClearBuffer();
                }
            }
        }
        catch (Exception e)
        {
            MessageBox.Show(e.ToString());
            throw;
        }
    }

    void WaveIn_DataAvailable(object? sender, NAudio.Wave.WaveInEventArgs e)
    {
        // copy buffer into an array of integers
        Int16[] values = new Int16[e.Buffer.Length / 2];
        rawSound = isNoize ? RandomNumberGenerator.GetBytes(e.Buffer.Length) : e.Buffer;
        Buffer.BlockCopy(rawSound, 0, values, 0, e.Buffer.Length);

        // determine the highest value as a fraction of the maximum possible value
        float fraction = (float)values.Max() / 32768;
        levels = values;
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