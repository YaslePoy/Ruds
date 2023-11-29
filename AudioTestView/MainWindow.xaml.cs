using System.Globalization;
using System.IO;
using System.Net;
using NAudio.Wave;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AudioTestView;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private Socket server;
    //private static NetworkStream serverStream = server.GetStream();
    private Int16[] levels = new short[882];
    private byte[] rawSound;
    private DateTime updateTime;
    private bool socketLoaded;
    private int max;
    public MainWindow()
    {
        InitializeComponent();
        int cols = 882;
        for (int i = 0; i < cols; i++)
        {
            TestGrid.ColumnDefinitions.Add(new ColumnDefinition());
            var vertical = new Rectangle() { Fill = new SolidColorBrush(Colors.Chocolate), StrokeThickness = 0 };
            TestGrid.Children.Add(vertical);
            Grid.SetColumn(vertical, i);
        }
        var waveIn = new NAudio.Wave.WaveInEvent
        {
            DeviceNumber = 0, // indicates which microphone to use
            WaveFormat = new NAudio.Wave.WaveFormat(rate: 44100, bits: 16, channels: 1),
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
            Indicator.Text = updateTime.ToString(CultureInfo.InvariantCulture);

            if (socketLoaded)
            {
                SLInd.Fill = new SolidColorBrush(Colors.Red);
            }

            MaxInd.Text = max.ToString();
        };
        levelUpdate.Start();
    }


    void NetworkHandle()
    {
        server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        server.Connect(IPEndPoint.Parse(File.ReadAllText("ipconfig.txt")));

        BufferedWaveProvider ms = new BufferedWaveProvider(new WaveFormat(44100, 16, 1));
        var wo = new WaveOutEvent();
        wo.Init(ms);
        wo.Play();


        while (server.Connected)
        {
            var buffed = new byte[4096];
            int len = server.Receive(buffed);
            buffed = buffed[..len];
            updateTime = DateTime.Now;
            var values = new short[buffed.Length / 2];
            Buffer.BlockCopy(buffed, 0, values, 0, buffed.Length);
            max = values.Max();
            ms.AddSamples(buffed, 0, len);
        }
    }

    void WaveIn_DataAvailable(object? sender, NAudio.Wave.WaveInEventArgs e)
    {
        // copy buffer into an array of integers
        Int16[] values = new Int16[e.Buffer.Length / 2];
        Buffer.BlockCopy(e.Buffer, 0, values, 0, e.Buffer.Length);
        rawSound = e.Buffer;
        // determine the highest value as a fraction of the maximum possible value
        float fraction = (float)values.Max() / 32768;
        levels = values;
        SendSound();

    }

    void SendSound()
    {
        // return;
        socketLoaded = true;

        server.Send(rawSound);
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        server.Send(new byte[] { (byte)255 });
        server.Close();
    }
}