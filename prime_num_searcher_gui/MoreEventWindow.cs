using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using prime_num_searcher_gui.win32;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;

namespace prime_num_searcher_gui
{
    public sealed class DpiChangedEventArgs : EventArgs /*CancelEventArgs*/
    {
        public ushort DeviceDpiNew { get; private set; }
        public ushort DeviceDpiOld { get; private set; }
        public Rectangle SuggestedRectangle { get; private set; }
        public override string ToString() => $"was: {DeviceDpiOld}, now: {DeviceDpiNew}";
        public DpiChangedEventArgs(ushort old, IntPtr WParam, IntPtr LParam)
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
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_ENTERSIZEMOVE = 0x0231;
            const int WM_EXITSIZEMOVE = 0x0232;
            const int WM_DPICHANGED = 0x02E0;
            const int WM_MOVE = 0x0003;
            switch (msg)
            {
                case WM_DPICHANGED:
                    var lo = Util.LOWORD(wParam);
                    dpiNew = lo;
                    if(this.dpiOld == this.dpiNew)
                    {
                        this.willBeAdjusted = false;
                    }
                    else if(true == isBeingMoved)
                    {
                        this.willBeAdjusted = true;
                    }
                    else
                    {

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
        public IntPtr HWnd { get; private set; } = IntPtr.Zero;
        private uint dpiOld = 0;
        private uint dpiNew = 0;
        private bool isBeingMoved = false;
        private bool willBeAdjusted = false;
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
        public MoreEventWindow()
        {
            this.ResizeBegin += (object sender, EventArgs e) => { this.isBeingMoved = true; };
            this.ResizeEnd += (object sender, EventArgs e) => { this.isBeingMoved = false; };
            this.Move += (object sender, EventArgs e) =>
            {
                if(true == this.willBeAdjusted && this.IsLocationGood())
                {
                    this.willBeAdjusted = false;
                }
            };
            this.SourceInitialized += (object sender, EventArgs e) => {
                this.HWnd = new WindowInteropHelper(this).Handle;
                HwndSource.FromHwnd(this.HWnd).AddHook(new HwndSourceHook(WndProc));
            };
        }
    }
}
