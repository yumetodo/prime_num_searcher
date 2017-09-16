using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OxyPlot.Series;


namespace prime_num_searcher_gui
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private BenchmarkResultManager benchmarkResultManager_ = new BenchmarkResultManager();
        private BenchmarkExecuter benchmarkExecuter_;
        public MainWindow()
        {
            benchmarkExecuter_ = (Environment.Is64BitOperatingSystem) ? new BenchmarkExecuter("prime_num_searcher_x64.exe") : new BenchmarkExecuter("prime_num_searcher.exe");
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.benchmarkResultManager_ = new BenchmarkResultManager();
            this.DataContext = this.benchmarkResultManager_;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            this.benchmarkResultManager_.BeforeStartBenchmark();
            try
            {
                this.benchmarkResultManager_.PlotSources = await benchmarkExecuter_.Execute(
                    this.benchmarkResultManager_.ReserchMaxNum,
                    this.benchmarkResultManager_.Interval,
                    onProgressChange: p => { this.benchmarkResultManager_.ProgressBarValue = p; }
                );
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
    }
}
