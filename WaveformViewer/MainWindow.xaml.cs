using System.Windows;
using ScottPlot;

namespace WaveformViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //Waveform.Plot.Grid.MajorLineColor = Color.FromHex("#0e3d54");
            //Waveform.Plot.FigureBackground.Color = Color.FromHex("#07263b");
            //Waveform.Plot.DataBackground.Color = Color.FromHex("#0b3049");
            Waveform.Plot.Add.Palette = new ScottPlot.Palettes.Penumbra();
            // change figure colors
            Waveform.Plot.FigureBackground.Color = Color.FromHex("#181818");
            Waveform.Plot.DataBackground.Color = Color.FromHex("#1f1f1f");
            // change axis and grid colors
            Waveform.Plot.Axes.Color(Color.FromHex("#d7d7d7"));
            Waveform.Plot.Grid.MajorLineColor = Color.FromHex("#404040");
            // change legend colors
            Waveform.Plot.Legend.BackgroundColor = Color.FromHex("#404040");
            Waveform.Plot.Legend.FontColor = Color.FromHex("#d7d7d7");
            Waveform.Plot.Legend.OutlineColor = Color.FromHex("#d7d7d7");

            Waveform.Plot.PlotControl!.Menu.Add("Reset Zoom", (c) =>
            {
                c.Plot.Axes.AutoScale();
                c.Refresh();
            });

        }

        private async void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // 获取拖放的文件路径
                var rawData = e.Data.GetData(DataFormats.FileDrop);
                if (rawData == null) return;
                var files = (string[])rawData;

                // 遍历所有拖放的文件路径
                //foreach (string file in files)
                //{
                //    // 处理文件，例如显示文件名
                //    MessageBox.Show($"File Dropped: {file}");
                //}
                await (this.DataContext as MainWindowModel)!.GenerateWaveform(Waveform, files);
                this.Title = string.Join(",", files);
            }
        }

        private void MainWindow_OnDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy; // 设置拖放效果为“复制”
            }
            else
            {
                e.Effects = DragDropEffects.None; // 如果不是文件，显示“禁止”符号
            }
        }
    }
}