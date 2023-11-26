using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AudioTestView;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static Int16[] levels;
    public MainWindow()
    {
        InitializeComponent();
        int cols = 882;
        for (int i = 0; i < cols; i++)
        {
            TestGrid.ColumnDefinitions.Add(new ColumnDefinition());
            var vertical = new Rectangle(){Fill = new SolidColorBrush(Colors.Chocolate), StrokeThickness = 0};
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

        DispatcherTimer levelUpdate = new DispatcherTimer();
        levelUpdate.Interval = TimeSpan.FromMilliseconds(50);
        levelUpdate.Tick += (sender, args) =>
        {
            double total = TestGrid.ActualHeight;
            for (int i = 0; i < MainWindow.levels.Length; i++)
            {
                (TestGrid.Children[i] as Rectangle)!.Height = Math.Max(total * levels[i] / 32768 * 2, 0);
            }
        };
        levelUpdate.Start();
    }
    
    void WaveIn_DataAvailable(object? sender, NAudio.Wave.WaveInEventArgs e)
    {
        // copy buffer into an array of integers
        Int16[] values = new Int16[e.Buffer.Length / 2];
        Buffer.BlockCopy(e.Buffer, 0, values, 0, e.Buffer.Length);

        // determine the highest value as a fraction of the maximum possible value
        float fraction = (float)values.Max() / 32768;
        MainWindow.levels = values;
    }
    
}