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
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private BenchmarkResultManager benchmarkResultManager_;
        private BenchmarkExecuter benchmarkExecuter_;
        private win32.WindowCapture windowCapture_;
        private IntPtr hWnd_;
        public MainWindow()
        {
            this.benchmarkExecuter_ = (Environment.Is64BitOperatingSystem) ? new BenchmarkExecuter("prime_num_searcher_x64.exe") : new BenchmarkExecuter("prime_num_searcher.exe");
            InitializeComponent();
            this.SourceInitialized += (object sender, EventArgs e) =>
            {
                this.hWnd_ = new WindowInteropHelper(this).Handle;
                this.benchmarkResultManager_ = new BenchmarkResultManager(this.hWnd_);
                this.windowCapture_ = new win32.WindowCapture(this.hWnd_, new System.Drawing.Size((int)this.Width, (int)this.ActualHeight));
            };
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //pass window handle
            this.DataContext = this.benchmarkResultManager_;
            this.windowCapture_ = new win32.WindowCapture(this.hWnd_, new System.Drawing.Size((int)this.Width, (int)this.ActualHeight));
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
