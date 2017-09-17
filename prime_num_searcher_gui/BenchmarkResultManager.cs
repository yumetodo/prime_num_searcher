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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
        [ComImport()]
        [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList3
        {
            // ITaskbarList
            [PreserveSig]
            void HrInit();
            [PreserveSig]
            void AddTab(IntPtr hwnd);
            [PreserveSig]
            void DeleteTab(IntPtr hwnd);
            [PreserveSig]
            void ActivateTab(IntPtr hwnd);
            [PreserveSig]
            void SetActiveAlt(IntPtr hwnd);

            // ITaskbarList2
            [PreserveSig]
            void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

            // ITaskbarList3
            [PreserveSig]
            void SetProgressValue(IntPtr hwnd, UInt64 ullCompleted, UInt64 ullTotal);
            [PreserveSig]
            void SetProgressState(IntPtr hwnd, Status state);
        }

        [ComImport()]
        [Guid("56fdf344-fd6d-11d0-958a-006097c9a090")]
        [ClassInterface(ClassInterfaceType.None)]
        private class TaskbarInstance
        {
        }
        private static ITaskbarList3 taskbarInstance = (ITaskbarList3)new TaskbarInstance();
        private static bool taskbarSupported = Environment.OSVersion.Version >= new Version(6, 1);
        private IntPtr handle_;

        public BenchmarkResultManager(IntPtr handle)
        {
            this.handle_ = handle;
        }
        private void SetProgressState(Status taskbarState)
        {
            if (taskbarSupported) taskbarInstance.SetProgressState(this.handle_, taskbarState);
        }

        private void SetProgressValue(UInt64 progressValue, UInt64 progressMax)
        {
            if (taskbarSupported) taskbarInstance.SetProgressValue(this.handle_, progressValue, progressMax);
        }
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
            this.BenchmarkStatus = Status.Benchmarking;
            this.ProgressBarValue = 0;
        }
        public void AfterFinishBenchmark()
        {
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
                this.OnPropertyChanged("ProgressBarMax");
                this.SetProgressValue(this.progressBarValue_, ProgressBarMax);
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
                this.SetProgressValue(this.progressBarValue_, ProgressBarMax);
            }
        }
        public UInt64 ProgressBarMax { get => this.reserchMaxNum_ / this.interval_ + 1; }
        private UInt64 progressBarValue_ = 0;
        public UInt64 ProgressBarValue {
            get => this.progressBarValue_;
            set {
                this.SetProperty(ref this.progressBarValue_, value);
                this.SetProgressValue(this.progressBarValue_, ProgressBarMax);
            }
        }
        private Status benchmarkStatus_ = Status.NoProgress;
        public Status BenchmarkStatus
        {
            get => this.benchmarkStatus_;
            set
            {
                //make progressbar value full
                if (this.benchmarkStatus_ != value && value.AnyOf(Status.NoProgress, Status.Error)) this.ProgressBarValue = this.ProgressBarMax;
                this.benchmarkStatus_ = value;
                this.SetProgressState(value);
                this.OnPropertyChanged("ProgressbarColor");
                this.NotifyButtonVisibilityChanged();
            }
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
