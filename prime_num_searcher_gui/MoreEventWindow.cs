using System;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using prime_num_searcher_gui.win32;
using System.Drawing;
using System.Diagnostics;

namespace prime_num_searcher_gui
{
    /// <summary>
    /// 遅延型DpiChangedEvent用
    /// <a href="http://8thway.blogspot.jp/2013/10/winforms-per-monitor-dpi-aggendum.html">WindowsフォームとPer-Monitor DPI（続）</a>を参照
    /// <see cref="DelayedDpiChangedEventHandler"/>
    /// </summary>
    public sealed class DelayedDpiChangedEventArgs : EventArgs /*CancelEventArgs*/
    {
        public ushort DeviceDpiNew { get; private set; }
        public ushort DeviceDpiOld { get; private set; }
        public Rectangle SuggestedRectangle { get; private set; }
        public override string ToString() => $"was: {DeviceDpiOld}, now: {DeviceDpiNew}";
        public DelayedDpiChangedEventArgs(ushort old, IntPtr WParam, IntPtr LParam)
        {
            this.DeviceDpiOld = old;
            this.DeviceDpiNew = Util.LOWORD(WParam);
            Debug.Assert(Util.HIWORD(WParam) == this.DeviceDpiNew, "Non-square pixels!");
            RECT suggestedRect = Marshal.PtrToStructure<RECT>(LParam);
            this.SuggestedRectangle = Rectangle.FromLTRB(suggestedRect.left, suggestedRect.top, suggestedRect.right, suggestedRect.bottom);
        }
    }
    class MoreEventWindow : Window
    {
        static class W32
        {
            public const uint MONITOR_DEFAULTTONULL = 0x00000000;
            public const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001;
            public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
            [DllImport("user32.dll")]
            public static extern IntPtr MonitorFromRect([In] ref RECT lprc, uint dwFlags);
            [DllImport("SHCore.dll")]
            public static extern IntPtr GetDpiForMonitor(IntPtr hmonitor, MonitorDpiType dpiType, ref uint dpiX, ref uint dpiY);
        }
        public event EventHandler ResizeBegin;
        protected virtual void OnResizeBegin(EventArgs e) => ResizeBegin?.Invoke(this, e);
        public event EventHandler ResizeEnd;
        protected virtual void OnResizeEnd(EventArgs e) => ResizeEnd?.Invoke(this, e);
        /// <summary>
        /// Occurs when the control is moved.
        /// </summary>
        public event EventHandler Move;
        protected virtual void OnMove(EventArgs e) => Move?.Invoke(this, e);
        public delegate void DelayedDpiChangedEventHandler(object sender, DelayedDpiChangedEventArgs e);
        /// <summary>
        /// 遅延されたDPI変更イベントです。
        /// <list type="number">
        /// <item><description>1. ウィンドウの移動開始と移動終了イベントを捉えて、移動中か否かをチェックできるようにしておく。</description></item>
        /// <item><description>2. WM_DPICHANGEDが来たときは、それに含まれる移動先のDPIと、ウィンドウの現在のDPIを比べ、これが違うときで、移動中のときは待機に入り、移動中でないときは直ちにイベントを発火する。逆にDPIが同じときは、待機を解除する。</description></item>
        /// <item><description>
        /// 3. ウィンドウの移動（Form.Move）イベントを捉え、待機中のときは、
        /// <list type="number">
        /// <item><description>1) その位置でウィンドウをリサイズした場合と同じ長方形を生成する。</description></item>
        /// <item><description>この長方形が属するモニターをMonitorFromRect関数で得て、スクリーン外でないことを確認の上、そのモニターのDPIをGetDpiForMonitor関数で得る。</description></item>
        /// <item><description>これが移動先のDPIと一致するときはイベントを発火し、待機を解除する。DPIが一致しないときはそのまま待機を続ける（以後、DPIが一致する位置に移動するまで繰り返し）。</description></item>
        /// </list>
        /// </description></item>
        /// <item><description>4. 待機中に移動元のモニターに戻ったときは、またWM_DPICHANGEDが来るので、2.によって待機は解除される。</description></item>
        /// </list>
        /// </summary>
        public event DelayedDpiChangedEventHandler DelayedDpiChanged;
        protected virtual void OnDelayedDpiChanged(DelayedDpiChangedEventArgs e) => DelayedDpiChanged?.Invoke(this, e);
        public IntPtr HWnd { get; private set; } = IntPtr.Zero;
        private ushort dpiOld = 0;
        private ushort dpiNew = 0;
        private bool isBeingMoved = false;
        private bool willBeAdjusted = false;
        private IntPtr wParam_ = IntPtr.Zero;
        private IntPtr lParam_ = IntPtr.Zero;
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_ENTERSIZEMOVE = 0x0231;
            const int WM_EXITSIZEMOVE = 0x0232;
            const int WM_DPICHANGED = 0x02E0;
            const int WM_MOVE = 0x0003;
            switch (msg)
            {
                case WM_DPICHANGED:
                    //save param
                    this.wParam_ = wParam;
                    this.lParam_ = lParam;
                    this.dpiNew = Util.LOWORD(wParam);
                    if (this.dpiOld == this.dpiNew)
                    {
                        this.willBeAdjusted = false;
                    }
                    else if(true == isBeingMoved)
                    {
                        this.willBeAdjusted = true;
                    }
                    else
                    {
                        this.OnDelayedDpiChanged(new DelayedDpiChangedEventArgs(this.dpiOld, wParam, lParam));
                    }
                    break;
                case WM_ENTERSIZEMOVE:
                    this.OnResizeBegin(new EventArgs());
                    break;
                case WM_EXITSIZEMOVE:
                    this.OnResizeEnd(new EventArgs());
                    break;
                case WM_MOVE:
                    this.OnMove(new EventArgs());
                    break;
            }
            return IntPtr.Zero;
        }
        private bool IsLocationGood()
        {
            //abort
            if (0 == this.dpiOld) return false;
            var factor = dpiNew / dpiOld;
            var widthDiff = Convert.ToInt32(SystemParameters.WorkArea.Width * factor) - SystemParameters.WorkArea.Width;
            var heightDiff = Convert.ToInt32(SystemParameters.WorkArea.Height * factor) - SystemParameters.WorkArea.Height;
            var r = new Rect(this.RenderSize);
            var rect = new RECT() { left = (int)r.Left, top = (int)r.Top, right = (int)r.Right, bottom = (int)r.Bottom };
            //Get handle to monitor that has the largest intersection with the rectangle.
            var handleMonitor = W32.MonitorFromRect(ref rect, W32.MONITOR_DEFAULTTONULL);
            if (IntPtr.Zero == handleMonitor) return false;
            uint dpiX = 0;
            uint dpiY = 0;
            var result = W32.GetDpiForMonitor(handleMonitor, MonitorDpiType.Default, ref dpiX, ref dpiY);
            if (IntPtr.Zero != result) return false;
            return (dpiX == dpiNew);
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            //promise initializing HWnd occur before another registered SourceInitialized event
            this.HWnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(this.HWnd).AddHook(new HwndSourceHook(WndProc));
            base.OnSourceInitialized(e);
        }
        public MoreEventWindow()
        {
            this.ResizeBegin += (object sender, EventArgs e) => { this.isBeingMoved = true; };
            this.ResizeEnd += (object sender, EventArgs e) => { this.isBeingMoved = false; };
            this.Move += (object sender, EventArgs e) =>
            {
                if(true == this.willBeAdjusted && this.IsLocationGood())
                {
                    this.willBeAdjusted = false;
                    this.OnDelayedDpiChanged(new DelayedDpiChangedEventArgs(this.dpiOld, this.wParam_, this.lParam_));
                }
            };
        }
    }
}
