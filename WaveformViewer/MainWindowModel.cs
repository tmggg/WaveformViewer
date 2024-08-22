using System.IO;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using ScottPlot;
using ScottPlot.Palettes;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using Color = ScottPlot.Color;

namespace WaveformViewer;

public partial class MainWindowModel : ObservableObject
{
    [ObservableProperty] private string? _c1;

    private IPlottable? _c1v, _c2v, _c3v, _c4v;
    //[ObservableProperty] private SolidBrush? _c1c;
    //[ObservableProperty] private SolidBrush? _c2c;
    //[ObservableProperty] private SolidBrush? _c3c;
    //[ObservableProperty] private SolidBrush? _c4c;

    private bool _c1View = true;


    private double[]? _c1X = [], _c2X = [], _c3X = [], _c4X = [];
    private double[]? _c1Y = [], _c2Y = [], _c3Y = [], _c4Y = [];
    [ObservableProperty] private string? _c2;
    private bool _c2View = true;
    [ObservableProperty] private string? _c3;
    private bool _c3View = true;
    [ObservableProperty] private string? _c4;
    private bool _c4View = true;
    private WpfPlot? _plot;
    private Crosshair? crosshair;

    public MainWindowModel()
    {
        C1 = "VCE";
        C2 = "IC";
        C3 = "VGE";
        C4 = "IG";
    }

    public bool C1View
    {
        get => _c1View;
        set
        {
            SetProperty(_c1View, value, b =>
            {
                _c1View = value;
                if (_c1v != null) _c1v.IsVisible = value;
                _plot?.Refresh();
            });
        }
    }

    public bool C2View
    {
        get => _c2View;
        set
        {
            SetProperty(_c2View, value, b =>
            {
                _c2View = value;
                if (_c2v != null) _c2v.IsVisible = value;
                _plot?.Refresh();
            });
        }
    }

    public bool C3View
    {
        get => _c3View;
        set
        {
            SetProperty(_c3View, value, b =>
            {
                _c3View = value;
                if (_c3v != null) _c3v.IsVisible = value;
                _plot?.Refresh();
            });
        }
    }

    public bool C4View
    {
        get => _c4View;
        set
        {
            SetProperty(_c4View, value, b =>
            {
                _c4View = value;
                if (_c4v != null) _c4v.IsVisible = value;
                _plot?.Refresh();
            });
        }
    }

    public async Task GenerateWaveform(WpfPlot plt, params string[] path)
    {
        if (path.Length > 4)
            MessageBox.Show("不支持大于4个波形文件解析!", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        if (!path.Any(p => p.Contains("C1") || p.Contains("C2") || p.Contains("C3") || p.Contains("C4")))
            MessageBox.Show("存在不规范文件名，请在文件名开头用 C1(VGE) C2(VCE) C3(IC) C4(IG) 导入!", "错误", MessageBoxButton.OK,
                MessageBoxImage.Error);
        var t1 = Task.Run(() =>
        {
            try
            {
                var c1 = GetData(path, "C1");
                _c1X = c1.Select(d => d!.Item1).ToArray();
                _c1Y = c1.Select(d => d!.Item2).ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        });
        var t2 = Task.Run(() =>
        {
            try
            {
                var c1 = GetData(path, "C2");
                _c2X = c1.Select(d => d!.Item1).ToArray();
                _c2Y = c1.Select(d => d!.Item2).ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return Task.CompletedTask;
        });
        var t3 = Task.Run(() =>
        {
            try
            {
                var c1 = GetData(path, "C3");
                _c3X = c1.Select(d => d!.Item1).ToArray();
                _c3Y = c1.Select(d => d!.Item2).ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return Task.CompletedTask;
        });
        var t4 = Task.Run(() =>
        {
            try
            {
                var c1 = GetData(path, "C4");
                _c4X = c1.Select(d => d!.Item1).ToArray();
                _c4Y = c1.Select(d => d!.Item2).ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return Task.CompletedTask;
        });
        Task.WaitAll(t1, t2, t3, t4);
        var c1Count = _c1X!.Length;
        var c2Count = _c2X!.Length;
        var c3Count = _c3X!.Length;
        var c4Count = _c4X!.Length;
        var min = new List<int> { c1Count, c2Count, c3Count, c4Count }.Where(n => n != 0).Min();
        //处理波形对齐
        if (min != 0)
        {
            foreach (var xArray in from data in new List<double[]?> { _c1X, _c2X, _c3X, _c4X }.OfType<double[]>()
                     where data.Any()
                     select data!.Skip(100).Take(min - 100).ToArray())
            {
                if (_c1X.Length != 0)
                    _c1X = xArray;
                if (_c2X.Length != 0)
                    _c2X = xArray;
                if (_c3X.Length != 0)
                    _c3X = xArray;
                if (_c4X.Length != 0)
                    _c4X = xArray;
            }

            //_c2X = _c2X!.Skip(100).Take(min - 100).ToArray();
            //_c3X = _c3X!.Skip(100).Take(min - 100).ToArray();
            //_c4X = _c4X!.Skip(100).Take(min - 100).ToArray();
            _c1Y = _c1Y!.Skip(100).Take(min - 100).ToArray();
            _c2Y = _c2Y!.Skip(100).Take(min - 100).ToArray();
            _c3Y = _c3Y!.Skip(100).Take(min - 100).ToArray();
            _c4Y = _c4Y!.Skip(100).Take(min - 100).ToArray();
        }

        DrawPlot(plt);
    }

    private List<Tuple<double, double>?> GetData(string[] path, string ch)
    {
        var p1 = path.FirstOrDefault(p => p.Contains(ch));
        if (string.IsNullOrWhiteSpace(p1)) return new List<Tuple<double, double>?>();
        var lines = File.ReadAllLines(p1);
        var data = lines.Select(p =>
        {
            double x, y;
            var data = p.Split(',');
            if (data.Length != 2) return null;
            if (double.TryParse(data[0], out var x1))
                x = x1;
            else
                return null;
            if (double.TryParse(data[1], out var y1))
                y = y1;
            else
                return null;
            return new Tuple<double, double>(x, y);
        }).ToList();
        data.RemoveAll(p => p == null);
        return data;
    }

    private void DrawPlot(WpfPlot plt)
    {
        plt.Reset();
        _plot = plt;
        plt.Plot.Add.Palette = new Penumbra();
        // change figure colors
        plt.Plot.FigureBackground.Color = Color.FromHex("#181818");
        plt.Plot.DataBackground.Color = Color.FromHex("#1f1f1f");
        // change axis and grid colors
        plt.Plot.Axes.Color(Color.FromHex("#d7d7d7"));
        plt.Plot.Grid.MajorLineColor = Color.FromHex("#404040");
        // change legend colors
        plt.Plot.Legend.BackgroundColor = Color.FromHex("#404040");
        plt.Plot.Legend.FontColor = Color.FromHex("#d7d7d7");
        plt.Plot.Legend.OutlineColor = Color.FromHex("#d7d7d7");

        plt.Plot.Axes.Left.Label.Text = "VGE(V)";
        plt.Plot.Axes.Left.Label.ForeColor = Colors.Yellow;
        plt.Plot.Axes.Color(Colors.Yellow);

        var c2Y = plt.Plot.Axes.AddLeftAxis();
        c2Y.LabelText = "VCE(V)";
        c2Y.LabelFontColor = Colors.DeepPink;
        c2Y.Color(Colors.DeepPink);

        var c3Y = plt.Plot.Axes.AddLeftAxis();
        c3Y.LabelText = "IC(A)";
        c3Y.LabelFontColor = Colors.DodgerBlue;
        c3Y.Color(Colors.DodgerBlue);

        var c4Y = plt.Plot.Axes.AddLeftAxis();
        c4Y.LabelText = "IG(A)";
        c4Y.LabelFontColor = Colors.Green;
        c4Y.Color(Colors.Green);

        //var c1 = plt.Plot.Add.ScatterLine(_c1X!, _c1Y!);
        var c1 = plt.Plot.Add.SignalXY(_c1X!, _c1Y!);
        c1.Color = Colors.Yellow;
        c1.LegendText = "VGE(V)";

        //var c2 = plt.Plot.Add.ScatterLine(_c2X!, _c2Y!);
        var c2 = plt.Plot.Add.SignalXY(_c2X!, _c2Y!);
        c2.Color = Colors.DeepPink;
        c2.Axes.YAxis = c2Y;
        c2.LegendText = "VCE(V)";

        //var c3 = plt.Plot.Add.ScatterLine(_c3X!, _c3Y!);
        var c3 = plt.Plot.Add.SignalXY(_c3X!, _c3Y!);
        c3.Color = Colors.DodgerBlue;
        c3.Axes.YAxis = c3Y;
        c3.LegendText = "IC(A)";

        //var c4 = plt.Plot.Add.ScatterLine(_c4X!, _c4Y!);
        var c4 = plt.Plot.Add.SignalXY(_c4X!, _c4Y!);
        c4.Color = Colors.Green;
        c4.Axes.YAxis = c4Y;
        c4.LegendText = "IG(A)";

        _c1v = c1;
        _c2v = c2;
        _c3v = c3;
        _c4v = c4;

        crosshair = plt.Plot.Add.Crosshair(0, 0);
        crosshair.TextColor = Colors.White;
        crosshair.TextBackgroundColor = crosshair.HorizontalLine.Color;
        plt.MouseMove -= PltOnMouseMove;
        plt.MouseMove += PltOnMouseMove;
        plt.Plot.Axes.AutoScale();
        plt.Plot.ShowLegend(Alignment.LowerCenter, Orientation.Horizontal);
        plt.Refresh();
    }

    private void PltOnMouseMove(object sender, MouseEventArgs e)
    {
        var p = e.GetPosition(_plot!);
        Pixel mousePixel = new(p.X * _plot!.DisplayScale, p.Y * _plot.DisplayScale);
        var mouseCoordinates = _plot.Plot.GetCoordinates(mousePixel);
        crosshair!.Position = mouseCoordinates;
        //var c2 = _plot.Plot.GetCoordinates(mousePixel, _c2v!.Axes.XAxis, _c2v.Axes.YAxis);
        //var c3 = _plot.Plot.GetCoordinates(mousePixel, _c3v!.Axes.XAxis, _c3v.Axes.YAxis);
        //var c4 = _plot.Plot.GetCoordinates(mousePixel, _c4v!.Axes.XAxis, _c4v.Axes.YAxis);
        var str = string.Empty;
        foreach (var data in new List<double[]?> { _c1X, _c2X, _c3X, _c4X })
            if (data is { Length: > 0 })
                if (data.First() <= mouseCoordinates.X && data.Last() >= mouseCoordinates.X)
                {
                    var xIndex = 0;
                    double c1Y = double.NaN, c2Y = double.NaN, c3Y = double.NaN, c4Y = double.NaN;
                    for (var i = 0; i < data.Length; i++)
                    {
                        if (!(data[i] >= mouseCoordinates.X)) continue;
                        xIndex = i;
                        break;
                    }

                    if (_c1Y!.Length > xIndex)
                        c1Y = _c1Y![xIndex];
                    if (_c2Y!.Length > xIndex)
                        c2Y = _c2Y![xIndex];
                    if (_c3Y!.Length > xIndex)
                        c3Y = _c3Y![xIndex];
                    if (_c4Y!.Length > xIndex)
                        c4Y = _c4Y![xIndex];
                    str = $"VGE:{c1Y:N3} V\r\n" +
                          $"VCE:{c2Y:N3} V\r\n" +
                          $"IC:{c3Y:N3} A\r\n" +
                          $"IG:{c4Y:N3} A";
                }

        crosshair.HorizontalLine.Text = str;
        crosshair.HorizontalLine.LabelRotation = 0;
        crosshair.HorizontalLine.LabelOffsetX = 60;
        crosshair.VerticalLine.LabelOffsetY = -30;
        crosshair.VerticalLine.Text = $"{mouseCoordinates.X:N3} μS";
        _plot.Refresh();
    }
}