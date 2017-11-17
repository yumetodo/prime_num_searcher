using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace prime_num_searcher_gui.win32
{
    #region Win32Structure
    public enum MonitorDpiType { Effective, Angular, Raw, Default = Effective }
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }
    public enum SW
    {
        HIDE = 0,
        SHOWNORMAL = 1,
        SHOWMINIMIZED = 2,
        SHOWMAXIMIZED = 3,
        SHOWNOACTIVATE = 4,
        SHOW = 5,
        MINIMIZE = 6,
        SHOWMINNOACTIVE = 7,
        SHOWNA = 8,
        RESTORE = 9,
        SHOWDEFAULT = 10,
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public int length;
        public int flags;
        public SW showCmd;
        public POINT minPosition;
        public POINT maxPosition;
        public RECT normalPosition;
    }
    public enum BitmapCompressionMode : uint
    {
        BI_RGB = 0,
        BI_RLE8 = 1,
        BI_RLE4 = 2,
        BI_BITFIELDS = 3,
        BI_JPEG = 4,
        BI_PNG = 5
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public BitmapCompressionMode biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;

        public void Init()
        {
            biSize = (uint)Marshal.SizeOf(this);
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RGBQUAD
    {
        public byte rgbBlue;
        public byte rgbGreen;
        public byte rgbRed;
        public byte rgbReserved;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public RGBQUAD[] bmiColors;
    }
    enum DIBColorMode : uint
    {
        DIB_RGB_COLORS = 0,
        DIB_PAL_COLORS = 1
    }
    public enum TernaryRasterOperations : uint
    {
        SRCCOPY = 0x00CC0020,
        SRCPAINT = 0x00EE0086,
        SRCAND = 0x008800C6,
        SRCINVERT = 0x00660046,
        SRCERASE = 0x00440328,
        NOTSRCCOPY = 0x00330008,
        NOTSRCERASE = 0x001100A6,
        MERGECOPY = 0x00C000CA,
        MERGEPAINT = 0x00BB0226,
        PATCOPY = 0x00F00021,
        PATPAINT = 0x00FB0A09,
        PATINVERT = 0x005A0049,
        DSTINVERT = 0x00550009,
        BLACKNESS = 0x00000042,
        WHITENESS = 0x00FF0062,
        //only if WinVer >= 5.0.0 (see wingdi.h)
        CAPTUREBLT = 0x40000000
    }
    #endregion
    static class Util
    {
        public static uint MAKELONG(uint low, uint high) => (high << 16) | (low & 0xffff);
        public static IntPtr MAKELPARAM(uint low, uint high) => (IntPtr)((high << 16) | (low & 0xffff));
        public static ushort HIWORD(uint n) => (ushort)((n >> 16) & 0xffff);
        public static ushort HIWORD(IntPtr n) => HIWORD(unchecked((uint)(ulong)n));
        public static ushort LOWORD(uint n) => (ushort)(n & 0xffff);
        public static ushort LOWORD(IntPtr n) => LOWORD(unchecked((uint)(ulong)n));
        public static short SignedHIWORD(IntPtr n) => SignedHIWORD(unchecked((int)(long)n));
        public static short SignedLOWORD(IntPtr n) => SignedLOWORD(unchecked((int)(long)n));
        public static short SignedHIWORD(int n) => (short)((n >> 16) & 0xffff);
        public static short SignedLOWORD(int n) => (short)(n & 0xFFFF);
    }
    class DeletableDeviceContext : IDisposable
    {
        private static class Api
        {
            [DllImport("gdi32.dll", SetLastError = true)]
            public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
            [DllImport("user32.dll")]
            public static extern int DeleteDC(IntPtr hdc);
        }
        private IntPtr hdc_;
        public DeletableDeviceContext(IntPtr hdc)
        {
            this.hdc_ = hdc;
        }
        public static implicit operator IntPtr(DeletableDeviceContext val) => val.hdc_;
        public static DeletableDeviceContext CreateCompatibleDC(IntPtr hdc) => new DeletableDeviceContext(Api.CreateCompatibleDC(hdc));
        #region IDisposable
        private bool disposed = false;
        ~DeletableDeviceContext()
        {
            this.Dispose();
        }
        public void Dispose()
        {
            if (!this.disposed)
            {
                Api.DeleteDC(this.hdc_);
                this.disposed = true;
            }
            GC.SuppressFinalize(this);
        }
        #endregion
    }
    class ReleasableDeviceContext : IDisposable
    {
        private static class Api
        {
            [DllImport("user32.dll")]
            public static extern IntPtr GetDC(IntPtr hWnd);
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);
            [DllImport("user32.dll")]
            public static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);
        }
        private IntPtr hWnd_;
        private IntPtr hdc_;
        public ReleasableDeviceContext(IntPtr hWnd, IntPtr hdc)
        {
            this.hWnd_ = hWnd;
            this.hdc_ = hdc;
        }
        public static implicit operator IntPtr(ReleasableDeviceContext val) => val.hdc_;
        public static ReleasableDeviceContext GetDC(IntPtr hWnd) => new ReleasableDeviceContext(hWnd, Api.GetDC(hWnd));
        public static ReleasableDeviceContext GetWindowDC(IntPtr hWnd) => new ReleasableDeviceContext(hWnd, Api.GetWindowDC(hWnd));
        #region IDisposable
        private bool disposed = false;
        ~ReleasableDeviceContext()
        {
            this.Dispose();
        }
        public void Dispose()
        {
            if (!this.disposed)
            {
                Api.ReleaseDC(this.hWnd_, this.hdc_);
                this.disposed = true;
            }
            GC.SuppressFinalize(this);
        }
        #endregion
    }
    class HBitmap : IDisposable
    {
        private static class Api
        {
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi, DIBColorMode pila, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);
            [DllImport("gdi32.dll")]
            public static extern int DeleteObject(IntPtr hdc);
        }
        private IntPtr hBitmap_;
        public HBitmap(IntPtr hdc)
        {
            this.hBitmap_ = hdc;
        }
        public static implicit operator IntPtr(HBitmap val) => val.hBitmap_;
        public static HBitmap CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, DIBColorMode pila, out IntPtr ppvBits, IntPtr hSection, uint dwOffset = 0)
            => new HBitmap(Api.CreateDIBSection(hdc, ref pbmi, pila, out ppvBits, hSection, dwOffset));
        public static HBitmap CreateDIBSection(IntPtr hdc, BITMAPINFO pbmi, DIBColorMode pila) => CreateDIBSection(hdc, ref pbmi, pila, out _, IntPtr.Zero);
        #region IDisposable
        private bool disposed = false;
        ~HBitmap()
        {
            this.Dispose();
        }
        public void Dispose()
        {
            if (!this.disposed)
            {
                Api.DeleteObject(this.hBitmap_);
                this.disposed = true;
            }
            GC.SuppressFinalize(this);
        }
        #endregion

    }
    class WindowCapture
    {
        private static class Api
        {
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);
            [DllImport("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
            public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
            /// <summary>
            ///    Performs a bit-block transfer of the color data corresponding to a
            ///    rectangle of pixels from the specified source device context into
            ///    a destination device context.
            /// </summary>
            /// <param name="hdc">Handle to the destination device context.</param>
            /// <param name="nXDest">The leftmost x-coordinate of the destination rectangle (in pixels).</param>
            /// <param name="nYDest">The topmost y-coordinate of the destination rectangle (in pixels).</param>
            /// <param name="nWidth">The width of the source and destination rectangles (in pixels).</param>
            /// <param name="nHeight">The height of the source and the destination rectangles (in pixels).</param>
            /// <param name="hdcSrc">Handle to the source device context.</param>
            /// <param name="nXSrc">The leftmost x-coordinate of the source rectangle (in pixels).</param>
            /// <param name="nYSrc">The topmost y-coordinate of the source rectangle (in pixels).</param>
            /// <param name="dwRop">A raster-operation code.</param>
            /// <returns>
            ///    <c>true</c> if the operation succeedes, <c>false</c> otherwise. To get extended error information, call <see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error"/>.
            /// </returns>
            [DllImport("gdi32.dll", EntryPoint = "BitBlt", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool BitBlt([In] IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, [In] IntPtr hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);
        }
        private DeletableDeviceContext screenDC_;
        private HBitmap ScreenHBitmap_;
        private IntPtr windowHandle_;
        private ReleasableDeviceContext windowDC_;
        private Size screenSize_;
        public Size ScreenSize
        {
            get => this.screenSize_;
            set
            {
                if (this.screenSize_ != value)
                {
                    WINDOWPLACEMENT wp = new WINDOWPLACEMENT() { length = Marshal.SizeOf<WINDOWPLACEMENT>() };
                    Api.GetWindowPlacement(this.windowHandle_, ref wp);
                    this.screenSize_ = value;
                    this.ScreenHBitmap_ = HBitmap.CreateDIBSection(IntPtr.Zero, CreateBITMAPINFO(value), DIBColorMode.DIB_RGB_COLORS);
                    //bind
                    Api.SelectObject(this.screenDC_, this.ScreenHBitmap_);
                }
            }
        }
        public WindowCapture(IntPtr hWnd, Size size)
        {
            this.windowHandle_ = hWnd;
            this.windowDC_ = ReleasableDeviceContext.GetWindowDC(hWnd);
            this.screenDC_ = DeletableDeviceContext.CreateCompatibleDC(IntPtr.Zero);
            this.ScreenSize = size;
        }
        public Bitmap Capture()
        {
            Api.BitBlt(this.screenDC_, 0, 0, ScreenSize.Width, ScreenSize.Height, this.windowDC_, 7, 0, TernaryRasterOperations.SRCCOPY);
            return Image.FromHbitmap(this.ScreenHBitmap_);
        }
        private static BITMAPINFO CreateBITMAPINFO(Size size) {
            BITMAPINFO bmi = new BITMAPINFO();
            bmi.bmiHeader.biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
            bmi.bmiHeader.biWidth = size.Width;
            bmi.bmiHeader.biHeight = size.Height;
            bmi.bmiHeader.biPlanes = 1;
            bmi.bmiHeader.biBitCount = 32;
            bmi.bmiHeader.biCompression = BitmapCompressionMode.BI_RGB;
            bmi.bmiHeader.biSizeImage = (uint)(size.Height * ((3 * size.Width + 3) / 4) * 4);
            return bmi;
        }
    }
}
