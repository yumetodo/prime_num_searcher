using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using Windows.UI.Xaml.Controls;

namespace prime_num_searcher_gui
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private BenchmarkResultManager benchmarkResultManager_;
        private BenchmarkExecuter benchmarkExecuter_;
        public MainWindow()
        {
            this.benchmarkExecuter_ = (Environment.Is64BitOperatingSystem) ? new BenchmarkExecuter("prime_num_searcher_x64.exe") : new BenchmarkExecuter("prime_num_searcher.exe");
            InitializeComponent();
            this.SourceInitialized += (object sender, EventArgs e) =>
            {
                var hWnd = new WindowInteropHelper(this).Handle;
                this.benchmarkResultManager_ = new BenchmarkResultManager(hWnd);
            };
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //pass window handle
            this.DataContext = this.benchmarkResultManager_;
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
                };
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                this.benchmarkResultManager_.NotifyError(ex.ToString());
            }
        }
    }
}
