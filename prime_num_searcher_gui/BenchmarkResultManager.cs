using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Windows.UI.Notifications;

namespace prime_num_searcher_gui
{
    enum Status : uint
    {
        NoProgress = 0,
        //Indeterminate = 0x1,
        Normal = 0x2,
        Benchmarking = Normal,
        Error = 0x4,
        Paused = 0x8
    }
    static class StatusEx
    {
        public static bool AnyOf(this Status target, params Status[] list) => list.Contains(target);
    }
    class BenchmarkResultManager : ValidatableDataBase
    {
        AeroProgress aeroProgress;
        public BenchmarkResultManager(IntPtr handle)
        {
            this.aeroProgress = new AeroProgress(handle);
        }
        public void NotifyError(string er)
        {
            this.BenchmarkStatus = Status.Error;
            //make progressbar value full
            this.ProgressBarValue = this.ProgressBarMax;
            Debug.WriteLine(er);
            var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            var texts = xml.GetElementsByTagName("text");
            texts[0].AppendChild(xml.CreateTextNode("prime_num_searcher_gui error"));
            texts[1].AppendChild(xml.CreateTextNode(er));
            windowsNotifier.Show(new ToastNotification(xml));
        }
        public void BeforeStartBenchmark()
        {
            this.BenchmarkStatus = Status.Benchmarking;
            this.ProgressBarValue = 0;
        }
        public void AfterFinishBenchmark()
        {
            if(this.ProgressBarValue != this.ProgressBarMax)
            {
                //make progressbar value full
                this.ReserchMaxNum = (this.ProgressBarValue + 1) * this.interval_;
            }
            this.BenchmarkStatus = Status.NoProgress;
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
                this.OnPropertyChanged("ProgressBarMax", "StatusBarText");
                this.aeroProgress.SetProgressValue(this.progressBarValue_, ProgressBarMax);
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
                this.OnPropertyChanged("ProgressBarMax", "StatusBarText");
                this.aeroProgress.SetProgressValue(this.progressBarValue_, ProgressBarMax);
            }
        }
        public UInt64 ProgressBarMax { get => this.reserchMaxNum_ / this.interval_ + 1; }
        private UInt64 progressBarValue_ = 0;
        public UInt64 ProgressBarValue {
            get => this.progressBarValue_;
            set {
                this.SetProperty(ref this.progressBarValue_, value);
                this.OnPropertyChanged("StatusBarText");
                this.aeroProgress.SetProgressValue(this.progressBarValue_, ProgressBarMax);
            }
        }
        private Status benchmarkStatus_ = Status.NoProgress;
        public Status BenchmarkStatus
        {
            get => this.benchmarkStatus_;
            set
            {
                this.benchmarkStatus_ = value;
                this.aeroProgress.SetProgressState(value);
                this.OnPropertyChanged("ProgressbarColor", "StatusBarText");
                this.NotifyButtonVisibilityChanged();
            }
        }
        public string StatusBarText
        {
            get => (this.benchmarkStatus_ == Status.NoProgress)
                ? (0 == progressBarValue_)
                    ? "準備完了"
                    : string.Format("Benchmark終了 ({0}/{1})", progressBarValue_, ProgressBarMax)
                : (this.benchmarkStatus_ == Status.Benchmarking) ? string.Format("Benchmark中 ({0}/{1})", progressBarValue_, ProgressBarMax)
                : (this.benchmarkStatus_ == Status.Paused) ? string.Format("一時停止中 ({0}/{1})", progressBarValue_, ProgressBarMax)
                : "エラー発生";
        }
        public string ProgressbarColor {
            get => (this.benchmarkStatus_.AnyOf(Status.NoProgress, Status.Benchmarking)) ? "SkyBlue"
                : (this.benchmarkStatus_ == Status.Paused) ? "Yellow"
                : "Red";
        }
        #region ButtonVisibility
        private Visibility ButtonVisibleWhen(params Status[] status) => (this.benchmarkStatus_.AnyOf(status)) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility BenchmarkButtonVisibility { get => this.ButtonVisibleWhen(Status.NoProgress, Status.Error); }
        public Visibility PauseButtonVisibility { get => this.ButtonVisibleWhen(Status.Benchmarking); }
        public Visibility ResumeButtonVisibility { get => this.ButtonVisibleWhen(Status.Paused); }
        public Visibility StopButtonVisibility { get => this.ButtonVisibleWhen(Status.Benchmarking, Status.Paused); }
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
