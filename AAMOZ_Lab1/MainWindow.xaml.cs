using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScottPlot;

namespace ImageLocalization
{
    public partial class MainWindow : Window
    {
        private BitmapSource _originalSource;
        private int _width, _height;

        public MainWindow()
        {
            InitializeComponent();
            // Початкове налаштування графіків
            PlotVertical.Plot.Axes.InvertY(); // Щоб 0 був зверху, як у зображення
            PlotVertical.Plot.HideGrid();
            PlotHorizontal.Plot.HideGrid();
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog { Filter = "Images|*.jpg;*.png;*.bmp" };
            if (op.ShowDialog() == true)
            {
                _originalSource = new BitmapImage(new Uri(op.FileName));
                ImgDisplay.Source = _originalSource;
                _width = _originalSource.PixelWidth;
                _height = _originalSource.PixelHeight;
            }
        }

        private void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (_originalSource == null) return;

            // 1. Створення Grayscale (256 відтінків сірого) - Пункт 3
            FormatConvertedBitmap grayBmp = new FormatConvertedBitmap(_originalSource, PixelFormats.Gray8, null, 0);
            ImgGray.Source = grayBmp;

            byte[] pixels = new byte[_width * _height];
            grayBmp.CopyPixels(pixels, _width, 0);

            // 2. Створення Чорно-білого (Binary) - Пункт 2[cite: 1]
            byte[] binaryPixels = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                binaryPixels[i] = (byte)(pixels[i] > 128 ? 255 : 0);
            }
            ImgBinary.Source = BitmapSource.Create(_width, _height, 96, 96, PixelFormats.Gray8, null, binaryPixels, _width);

            // 3. Розрахунок проєкцій - Пункт 5[cite: 1]
            double[] hProj = new double[_width];
            double[] vProj = new double[_height];

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    byte val = pixels[y * _width + x];
                    hProj[x] += val;
                    vProj[y] += val;
                }
            }

            // 4. Пошук мінімумів та оновлення інтерфейсу
            var xMins = FindMinima(hProj);
            var yMins = FindMinima(vProj);

            UpdatePlots(hProj, vProj);
            DrawLocalization(xMins, yMins);
    
            // Вивід статистики в текстове поле
            TxtAllStats.Text = $"HI(x) Mean: {hProj.Average():F1}\n" +
                               $"VI(y) Mean: {vProj.Average():F1}\n" +
                               $"X-Minima found: {xMins.Count}\n" +
                               $"Y-Minima found: {yMins.Count}";
        }

        private List<int> FindMinima(double[] data)
        {
            List<int> mins = new List<int>();
            double avg = data.Average();
            // Шукаємо точки, які значно нижчі за середнє (впадини)
            for (int i = 5; i < data.Length - 5; i++)
            {
                if (data[i] < avg * 0.8 && data[i] < data[i - 1] && data[i] < data[i + 1])
                {
                    if (mins.Count == 0 || i - mins.Last() > 10) // Уникаємо купчастості
                        mins.Add(i);
                }
            }
            return mins;
        }

        private void UpdatePlots(double[] hData, double[] vData)
        {
            // Горизонтальна проєкція (знизу)
            PlotHorizontal.Plot.Clear();
            var hBars = PlotHorizontal.Plot.Add.Bars(hData);
            PlotHorizontal.Plot.Axes.SetLimits(0, _width, 0, hData.Max());
            PlotHorizontal.Refresh();

            // Вертикальна проєкція (справа)
            PlotVertical.Plot.Clear();
            var vBars = PlotVertical.Plot.Add.Bars(vData);
            vBars.Horizontal = true; // Стовпчики ростуть вбік
            PlotVertical.Plot.Axes.SetLimits(0, vData.Max(), 0, _height);
            PlotVertical.Refresh();
        }

        private void DrawLocalization(List<int> xMins, List<int> yMins)
        {
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                dc.DrawImage(_originalSource, new Rect(0, 0, _width, _height));
                Pen pen = new Pen(Brushes.Cyan, 2); // Колір ліній як на скріншоті

                foreach (var x in xMins)
                    dc.DrawLine(pen, new Point(x, 0), new Point(x, _height));

                foreach (var y in yMins)
                    dc.DrawLine(pen, new Point(0, y), new Point(_width, y));
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(_width, _height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            ImgDisplay.Source = rtb;
        }
    }
}