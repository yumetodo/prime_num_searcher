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
using Windows.UI.Notifications;

namespace prime_num_searcher_gui
{
    class BenchmarkResultManager : ValidatableDataBase
    {
        private void MakeProgressBarFull()
        {
            this.ProgressBarValue = this.ProgressBarMax;
        }
        public void NotifyError(string er)
        {
            this.IsNoError = false;
            this.MakeProgressBarFull();
            Debug.WriteLine(er);
            var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            var texts = xml.GetElementsByTagName("text");
            texts[0].AppendChild(xml.CreateTextNode("prime_num_searcher_gui error"));
            texts[1].AppendChild(xml.CreateTextNode(er));
            windowsNotifier.Show(new ToastNotification(xml));
        }
        public void ClearError()
        {
            this.isNoError = true;
        }
        public void BeforeStartBenchmark()
        {
            this.ClearError();
            this.IsNotBenchmarking = false;
            this.ProgressBarValue = 0;
        }
        public void AfterFinishBenchmark()
        {
            this.IsNotBenchmarking = true;
            this.MakeProgressBarFull();
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
        private UInt64 progressBarValue_ = 1;
        public UInt64 ProgressBarValue {
            get => this.progressBarValue_;
            set { this.SetProperty(ref this.progressBarValue_, value); }
        }
        private bool isNoError = true;
        public bool IsNoError
        {
            get => this.isNoError;
            set
            {
                this.SetProperty(ref this.isNoError, value);
                this.OnPropertyChanged("ProgressbarColor");
            }
        }
        private static readonly string[] progressBarColors = { "Red", "SkyBlue" };
        public string ProgressbarColor
        {
            get => progressBarColors[this.isNoError ? 1 : 0];
        }
        private bool isNotBenchmarking = true;
        public bool IsNotBenchmarking
        {
            get => this.isNotBenchmarking;
            set { this.SetProperty(ref this.isNotBenchmarking, value); }
        }
        #endregion
    }
}
