using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Drawing.Imaging;
using Windows.UI.Xaml.Controls;
using System.Windows.Media;

namespace prime_num_searcher_gui
{
    public static class VisualEx
    {
        /// <summary>
        /// 現在の <see cref="T:System.Windows.Media.Visual"/> から、DPI 倍率を取得します。
        /// </summary>
        /// <returns>
        /// X 軸 および Y 軸それぞれの DPI 倍率を表す <see cref="T:System.Windows.Point"/>
        /// 構造体。取得に失敗した場合、(1.0, 1.0) を返します。
        /// </returns>
        public static Point GetDpiScaleFactor(this Visual visual)
        {
            var source = PresentationSource.FromVisual(visual);
            if (source != null && source.CompositionTarget != null)
            {
                return new Point(
                    source.CompositionTarget.TransformToDevice.M11,
                    source.CompositionTarget.TransformToDevice.M22);
            }

            return new Point(1.0, 1.0);
        }
    }
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private BenchmarkResultManager benchmarkResultManager_;
        private BenchmarkExecuter benchmarkExecuter_;
        private win32.WindowCapture windowCapture_;
        public MainWindow()
        {
            this.benchmarkExecuter_ = (Environment.Is64BitOperatingSystem) ? new BenchmarkExecuter("prime_num_searcher_x64.exe") : new BenchmarkExecuter("prime_num_searcher.exe");
            InitializeComponent();
            this.SourceInitialized += (object sender, EventArgs e) =>
            {
                var hWnd = new WindowInteropHelper(this).Handle;
                this.benchmarkResultManager_ = new BenchmarkResultManager(hWnd);
                this.windowCapture_ = new win32.WindowCapture(hWnd, new System.Drawing.Size((int)this.Width, (int)this.ActualHeight));
                //WndProc
                HwndSource.FromHwnd(hWnd).AddHook(new HwndSourceHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) => {
                    const int WM_SIZING = 0x0214;
                    const int WM_EXITSIZEMOVE = 0x0232;
                    const int WM_DPICHANGED = 0x02E0;
                    switch (msg)
                    {
                        case WM_DPICHANGED:
                            break;
                        case WM_SIZING:
                            break;
                        case WM_EXITSIZEMOVE:
                            break;
                    }
                    return IntPtr.Zero;
                }));
            };
        }
        private System.Drawing.Size GetDPI()
        {
            var dpiScaleFactor = this.GetDpiScaleFactor();
            return new System.Drawing.Size((int)this.Width, (int)this.ActualHeight);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //pass window handle
            this.DataContext = this.benchmarkResultManager_;
            var s1 = SystemParameters.WorkArea;
            var s2 = new System.Drawing.Size((int)this.Width, (int)this.Height);
            var dpiScaleFactor = this.GetDpiScaleFactor();
            var s3 = new System.Drawing.Size((int)(this.Width * dpiScaleFactor.X), (int)(this.Height * dpiScaleFactor.Y));
            var s4 = new System.Drawing.Size((int)((this.Width - 14) * dpiScaleFactor.X), (int)((this.Height - 7) * dpiScaleFactor.Y));
            var s5 = new System.Drawing.Size((int)(this.Width * dpiScaleFactor.X - 14), (int)(this.Height * dpiScaleFactor.Y - 7));
            this.windowCapture_ = new win32.WindowCapture(hWnd, new System.Drawing.Size((int)this.Width, (int)this.ActualHeight));
            this.SizeChanged += OnSizeChanged;
        }

        private async void BenchmarkButtonClick(object sender, RoutedEventArgs e)
        {
            this.benchmarkResultManager_.BeforeStartBenchmark();
            try
            {
                this.benchmarkResultManager_.PlotSources = await benchmarkExecuter_.Execute(
                    this.benchmarkResultManager_.ReserchMaxNum,
                    this.benchmarkResultManager_.Interval,
                    onProgressChange: p => { this.benchmarkResultManager_.ProgressBarValue = p; }
                );
                this.benchmarkResultManager_.YAxisLabelText = string.Format("時間 [{0}]", benchmarkExecuter_.ResultTimeUnit.ToUnitString());
            }
            catch(Exception ex)
            {
                this.benchmarkResultManager_.NotifyError(ex.ToString());
            }
            finally
            {
                this.benchmarkResultManager_.AfterFinishBenchmark();
            }
        }

        private async void PauseButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.benchmarkResultManager_.PlotSources = await this.benchmarkExecuter_.Pasue();
                this.benchmarkResultManager_.YAxisLabelText = string.Format("時間 [{0}]", benchmarkExecuter_.ResultTimeUnit.ToUnitString());
                this.benchmarkResultManager_.BenchmarkStatus = Status.Paused;
            }
            catch(Exception ex)
            {
                this.benchmarkResultManager_.NotifyError(ex.ToString());
            }
        }

        private void ResumeButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.benchmarkExecuter_.NotifyResume();
                this.benchmarkResultManager_.BenchmarkStatus = Status.Benchmarking;
            }
            catch (Exception ex)
            {
                this.benchmarkResultManager_.NotifyError(ex.ToString());
            }
        }

        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                this.benchmarkExecuter_.NotifyStop();
                //this.benchmarkResultManager_.BenchmarkStatus = Status.None;
            }
            catch (Exception ex)
            {
                this.benchmarkResultManager_.NotifyError(ex.ToString());
            }
        }
        private void SaveGraphButtonClick(object clickSender, RoutedEventArgs clickE)
        {
            try
            {
                var dialog = new SaveFileDialog() {
                    Title = "ベンチマーク結果グラフ画像を保存",
                    Filter = "SVG (*.svg)|*.svg|BMP (*.bmp)|*.bmp|PNG (*.png)|*.png|JPEG (*.jpg;*.jpeg;*.jpe;*.jfif)|*.jpg;*.jpeg;*.jpe;*.jfif"
                };
                dialog.FileOk += (object sender, CancelEventArgs e) =>
                {
                    var d = (SaveFileDialog)sender;
                    var saveFile = d.OpenFile();
                    if (1 == d.FilterIndex)
                    {
                        var plot = this.FindName("BenchmarkGraph") as OxyPlot.Wpf.Plot;
                        var svgExporter = new OxyPlot.SvgExporter { Width = plot.ActualWidth, Height = plot.ActualHeight };
                        svgExporter.Export(plot.ActualModel, saveFile);
                    }
                    else
                    {
                        ImageFormat fmt;
                        switch (d.FilterIndex)
                        {
                            case 2: fmt = ImageFormat.Bmp; break;
                            case 3: fmt = ImageFormat.Png; break;
                            case 4: fmt = ImageFormat.Jpeg; break;
                            default: fmt = ImageFormat.Png; break;
                        }
                        this.windowCapture_.Capture().Save(saveFile, fmt);
                    }
                };
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                this.benchmarkResultManager_.NotifyError(ex.ToString());
            }
        }
        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                this.windowCapture_.ScreenSize = new System.Drawing.Size((int)e.NewSize.Width, (int)e.NewSize.Height);
            }
            catch (Exception ex)
            {
                this.benchmarkResultManager_.NotifyError(ex.ToString());
            }
        }
    }
}
