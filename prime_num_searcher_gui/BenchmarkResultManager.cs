using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI.Notifications;

namespace prime_num_searcher_gui
{
    enum Status : uint
    {
        None = 0,
        Benchmarking = 1,
        Paused = 2,
        Error = 3
    }
    static class StatusExtention
    {
        public static bool AnyOf(this Status target, params Status[] list) => list.Contains(target);
    }
    class BenchmarkResultManager : ValidatableDataBase
    {
        public void NotifyError(string er)
        {
            this.BenchmarkStatus = Status.Error;
            Debug.WriteLine(er);
            var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            var texts = xml.GetElementsByTagName("text");
            texts[0].AppendChild(xml.CreateTextNode("prime_num_searcher_gui error"));
            texts[1].AppendChild(xml.CreateTextNode(er));
            windowsNotifier.Show(new ToastNotification(xml));
        }
        public void BeforeStartBenchmark()
        {
            this.IsNotBenchmarking = false;
            this.BenchmarkStatus = Status.Benchmarking;
            this.ProgressBarValue = 0;
        }
        public void AfterFinishBenchmark()
        {
            this.IsNotBenchmarking = true;
            this.BenchmarkStatus = Status.None;
        }

        #region win10notify
        ToastNotifier windowsNotifier = ToastNotificationManager.CreateToastNotifier("prime_num_searcher_gui");
        #endregion
        #region Binding
        private Dictionary<string, List<ScatterPoint>> plotSources_ = BenchmarkExecuter.CreateResultDictionary();
        public Dictionary<string, List<ScatterPoint>> PlotSources {
            get => this.plotSources_;
            set { this.SetProperty(ref this.plotSources_, value); }
        }
        private UInt64 reserchMaxNum_ = 2;
        [Required]
        [NumericRange(2, UInt64.MaxValue, ErrorMessage = "2以上の値を入力してください")]
        public UInt64 ReserchMaxNum {
            get => this.reserchMaxNum_;
            set
            {
                this.SetAndValidatePropaty(ref this.reserchMaxNum_, value);
                this.OnPropertyChanged("ProgressBarMax");
            }
        }
        private UInt64 interval_ = 1;
        [Required]
        [NumericRange(1, UInt64.MaxValue, ErrorMessage = "1以上の値を入力してください")]
        public UInt64 Interval {
            get => this.interval_;
            set
            {
                this.SetAndValidatePropaty(ref this.interval_, value);
                this.OnPropertyChanged("ProgressBarMax");
            }
        }
        public UInt64 ProgressBarMax { get => this.reserchMaxNum_ / this.interval_ + 1; }
        private UInt64 progressBarValue_ = 0;
        public UInt64 ProgressBarValue {
            get => this.progressBarValue_;
            set { this.SetProperty(ref this.progressBarValue_, value); }
        }
        private Status benchmarkStatus = Status.None;
        public Status BenchmarkStatus
        {
            get => this.benchmarkStatus;
            set
            {
                Debug.Assert(Enum.GetNames(typeof(Status)).Length == progressBarColors.Length);
                this.benchmarkStatus = value;
                this.OnPropertyChanged("ProgressbarColor");
                this.NotifyButtonVisibilityChanged();
                //make progressbar value full
                if(value.AnyOf(Status.None, Status.Error)) this.ProgressBarValue = this.ProgressBarMax;
            }
        }
        private static readonly string[] progressBarColors = { "SkyBlue", "SkyBlue", "Yellow", "Red" };
        public string ProgressbarColor
        {
            get => progressBarColors[(uint)benchmarkStatus];
        }
        private bool isNotBenchmarking = true;
        public bool IsNotBenchmarking
        {
            get => this.isNotBenchmarking;
            set { this.SetProperty(ref this.isNotBenchmarking, value); }
        }
        #region ButtonVisibility
        public Visibility BenchmarkButtonVisibility
        {
            get => (this.benchmarkStatus.AnyOf(Status.None, Status.Error)) ? Visibility.Visible : Visibility.Collapsed;
        }
        public Visibility PauseButtonVisibility
        {
            get => (this.benchmarkStatus.AnyOf(Status.Benchmarking)) ? Visibility.Visible : Visibility.Collapsed;
        }
        public Visibility ResumeButtonVisibility
        {
            get => (this.benchmarkStatus.AnyOf(Status.Paused)) ? Visibility.Visible : Visibility.Collapsed;
        }
        public Visibility StopButtonVisibility
        {
            get => (this.benchmarkStatus.AnyOf(Status.Benchmarking, Status.Paused)) ? Visibility.Visible : Visibility.Collapsed;
        }
        private void NotifyButtonVisibilityChanged()
        {
            this.OnPropertyChanged("BenchmarkButtonVisibility");
            this.OnPropertyChanged("PauseButtonVisibility");
            this.OnPropertyChanged("ResumeButtonVisibility");
            this.OnPropertyChanged("StopButtonVisibility");
        }
        #endregion
        #endregion
    }
}
