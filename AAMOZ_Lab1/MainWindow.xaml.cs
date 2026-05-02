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
using Microsoft.Win32;

namespace AAMOZ_Lab1;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private BitmapSource _originalSource;
    private int _width, _height;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog op = new OpenFileDialog
        {
            Title = "Оберіть зображення",
            Filter = "Image files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png"
        };
        if (op.ShowDialog() == true)
        {
            _originalSource = new BitmapImage(new Uri(op.FileName));
            ImgOriginal.Source = _originalSource;
            TxtStatus.Text = $"Завантажено: {op.SafeFileName}";
        }
    }

    private void BtnProcess_Click(object sender, RoutedEventArgs e)
    {
        if (_originalSource == null) return;

        // Перетворення в масив байтів для швидкості
        _width = _originalSource.PixelWidth;
        _height = _originalSource.PixelHeight;

        // Формуємо Gray та Binary варіанти (пункти 2 та 3 ТЗ)
        FormatConvertedBitmap grayBmp = new FormatConvertedBitmap(_originalSource, PixelFormats.Gray8, null, 0);
        ImgGray.Source = grayBmp;

        // Отримуємо інтенсивності I(x,y)
        byte[] pixels = new byte[_width * _height];
        grayBmp.CopyPixels(pixels, _width, 0);

        // 1. Створення ЧБ зображення (пункт 2)
        byte[] binaryPixels = pixels.Select(p => (byte)(p > 128 ? 255 : 0)).ToArray();
        ImgBinary.Source = BitmapSource.Create(_width, _height, 96, 96, PixelFormats.Gray8, null, binaryPixels, _width);

        // 2. Розрахунок проєкцій (пункт 5)
        double[] horizProj = new double[_width];
        double[] vertProj = new double[_height];

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                byte intensity = pixels[y * _width + x];
                horizProj[x] += intensity;
                vertProj[y] += intensity;
            }
        }

        // 3. Пошук локальних мінімумів (пункти 6, 7, 8)
        var xMinima = FindLocalMinima(horizProj);
        var yMinima = FindLocalMinima(vertProj);

        // Візуалізація результатів
        UpdatePlots(horizProj, vertProj);
        DisplayStatistics(horizProj, vertProj);
        DrawLocalizationLines(xMinima, yMinima);
    }

    private List<int> FindLocalMinima(double[] data)
    {
        List<int> minima = new List<int>();
        double threshold = data.Average() * 0.7; // Емпіричний поріг для "різких змін"

        for (int i = 1; i < data.Length - 1; i++)
        {
            if (data[i] < data[i - 1] && data[i] < data[i + 1] && data[i] < threshold)
            {
                minima.Add(i);
            }
        }

        return minima;
    }

    private void UpdatePlots(double[] hData, double[] vData)
    {
        PlotHorizontal.Plot.Clear();
        PlotHorizontal.Plot.Add.Bars(hData);
        PlotHorizontal.Refresh();

        PlotVertical.Plot.Clear();
        PlotVertical.Plot.Add.Bars(vData);
        PlotVertical.Refresh();
    }

    private void DisplayStatistics(double[] hData, double[] vData)
    {
        // Обчислення статистики (пункт 9)
        double hMean = hData.Average();
        double hSum = hData.Sum();
        TxtStatHorizontal.Text = $"Середнє: {hMean:F2}; Сума: {hSum:F0}";

        var hTable = hData.Select((val, i) => new ProjectionPoint
        {
            Index = i, Value = val, Relative = val / hSum
        }).Take(100).ToList(); // Обмежимо для DataGrid

        GridFreqHorizontal.ItemsSource = hTable;

        double vMean = vData.Average();
        double vSum = vData.Sum();
        TxtStatVertical.Text = $"Середнє: {vMean:F2}; Сума: {vSum:F0}";

        var vTable = vData.Select((val, i) => new ProjectionPoint
        {
            Index = i, Value = val, Relative = val / vSum
        }).Take(100).ToList();

        GridFreqVertical.ItemsSource = vTable;
    }

    private void DrawLocalizationLines(List<int> xMins, List<int> yMins)
    {
        DrawingVisual dv = new DrawingVisual();
        using (DrawingContext dc = dv.RenderOpen())
        {
            dc.DrawImage(_originalSource, new Rect(0, 0, _width, _height));
            Pen pen = new Pen(Brushes.Lime, 2);

            foreach (var x in xMins)
                dc.DrawLine(pen, new Point(x, 0), new Point(x, _height));

            foreach (var y in yMins)
                dc.DrawLine(pen, new Point(0, y), new Point(_width, y));
        }

        RenderTargetBitmap rtb = new RenderTargetBitmap(_width, _height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(dv);
        ImgOriginal.Source = rtb;
    }
}

public class ProjectionPoint
{
    public int Index { get; set; }      // X або Y
    public double Value { get; set; }   // n_i або m_j (Частота)
    public double Relative { get; set; } // P_i (Відносна частота)
}