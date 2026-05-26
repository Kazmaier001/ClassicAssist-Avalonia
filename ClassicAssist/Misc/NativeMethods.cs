#region License

// Copyright (C) 2020 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.Runtime.InteropServices;

namespace ClassicAssist.Misc
{
    public static class NativeMethods
    {
        public enum TernaryRasterOperations : uint
        {
            /// <summary>dest = source</summary>
            SRCCOPY = 0x00CC0020,

            /// <summary>dest = source OR dest</summary>
            SRCPAINT = 0x00EE0086,

            /// <summary>dest = source AND dest</summary>
            SRCAND = 0x008800C6,

            /// <summary>dest = source XOR dest</summary>
            SRCINVERT = 0x00660046,

            /// <summary>dest = source AND (NOT dest)</summary>
            SRCERASE = 0x00440328,

            /// <summary>dest = (NOT source)</summary>
            NOTSRCCOPY = 0x00330008,

            /// <summary>dest = (NOT src) AND (NOT dest)</summary>
            NOTSRCERASE = 0x001100A6,

            /// <summary>dest = (source AND pattern)</summary>
            MERGECOPY = 0x00C000CA,

            /// <summary>dest = (NOT source) OR dest</summary>
            MERGEPAINT = 0x00BB0226,

            /// <summary>dest = pattern</summary>
            PATCOPY = 0x00F00021,

            /// <summary>dest = DPSnoo</summary>
            PATPAINT = 0x00FB0A09,

            /// <summary>dest = pattern XOR dest</summary>
            PATINVERT = 0x005A0049,

            /// <summary>dest = (NOT dest)</summary>
            DSTINVERT = 0x00550009,

            /// <summary>dest = BLACK</summary>
            BLACKNESS = 0x00000042,

            /// <summary>dest = WHITE</summary>
            WHITENESS = 0x00FF0062
        }

        [DllImport( "user32" )]
        public static extern bool GetWindowRect( IntPtr hWnd, out RECT rect );

        [DllImport( "gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true )]
        public static extern IntPtr SelectObject( IntPtr hdc, IntPtr hgdiobj );

        [DllImport( "gdi32.dll" )]
        public static extern IntPtr CreateCompatibleBitmap( IntPtr hdc, int nWidth, int nHeight );

        [DllImport( "gdi32.dll", SetLastError = true )]
        public static extern IntPtr CreateCompatibleDC( IntPtr hdc );

        [DllImport( "gdi32.dll" )]
        public static extern bool DeleteObject( IntPtr hObject );

        [DllImport( "gdi32.dll" )]
        public static extern bool DeleteDC( IntPtr hdc );

        [DllImport( "user32.dll" )]
        public static extern IntPtr GetDC( IntPtr hWnd );

        [DllImport( "user32.dll" )]
        public static extern int ReleaseDC( IntPtr hWnd, IntPtr hDC );

        [DllImport( "gdi32.dll" )]
        public static extern bool BitBlt( IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc,
            int nXSrc, int nYSrc, TernaryRasterOperations dwRop );

        [DllImport( "user32.dll" )]
        public static extern bool GetClientRect( IntPtr hWnd, out RECT lpRect );

        [DllImport( "user32.dll" )]
        public static extern bool ClientToScreen( IntPtr hWnd, ref POINT lpPoint );

        [StructLayout( LayoutKind.Sequential )]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public delegate bool EnumWindowsProc( IntPtr hWnd, IntPtr lParam );

        [DllImport( "user32.dll" )]
        public static extern bool EnumWindows( EnumWindowsProc lpEnumFunc, IntPtr lParam );

        [DllImport( "user32.dll", CharSet = CharSet.Unicode, SetLastError = true )]
        public static extern int GetClassName( IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount );

        [DllImport( "user32.dll", SetLastError = true )]
        public static extern uint GetWindowThreadProcessId( IntPtr hWnd, out uint lpdwProcessId );

        [DllImport( "user32.dll" )]
        public static extern bool IsWindowVisible( IntPtr hWnd );

        [DllImport( "user32.dll" )]
        public static extern bool PrintWindow( IntPtr hWnd, IntPtr hdcBlt, uint nFlags );

        // PW_RENDERFULLCONTENT (added in Windows 8.1) asks DWM to render the
        // window's actual contents into the DC, working around GDI's inability
        // to BitBlt Vulkan/D3D/OpenGL surfaces directly. Critically: this also
        // captures the window EVEN IF another window is on top of it.
        public const uint PW_RENDERFULLCONTENT = 0x00000002;

        [DllImport( "user32.dll" )]
        public static extern int GetSystemMetrics( int nIndex );

        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;

        [DllImport( "gdi32.dll" )]
        public static extern int GetDIBits( IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines,
            IntPtr lpvBits, ref BITMAPINFOHEADER lpbi, uint uUsage );

        public const uint BI_RGB = 0;
        public const uint DIB_RGB_COLORS = 0;

        [StructLayout( LayoutKind.Sequential )]
        public struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
            public uint biColors1;
            public uint biColors2;
            public uint biColors3;
            public uint biColors4;
        }

        [StructLayout( LayoutKind.Sequential )]
        public struct RECT
        {
            public int Left; // x position of upper-left corner
            public int Top; // y position of upper-left corner
            public int Right; // x position of lower-right corner
            public int Bottom; // y position of lower-right corner
        }

        [DllImport( "user32.dll" )]
        public static extern IntPtr FindWindow( string className, string windowTitle );

        [DllImport( "user32.dll" )]
        [return: MarshalAs( UnmanagedType.Bool )]
        public static extern bool ShowWindow( IntPtr hWnd, ShowWindowEnum flags );

        [DllImport( "user32.dll" )]
        public static extern bool ShowWindowAsync( IntPtr hWnd, ShowWindowEnum flags );

        [DllImport( "user32.dll" )]
        public static extern int SetForegroundWindow( IntPtr hwnd );

        [DllImport( "user32.dll" )]
        [return: MarshalAs( UnmanagedType.Bool )]
        public static extern bool GetWindowPlacement( IntPtr hWnd, ref Windowplacement lpwndpl );

        [DllImport( "user32.dll", SetLastError = true )]
        public static extern void SwitchToThisWindow( IntPtr hWnd, bool fAltTab );

        [DllImport( "user32.dll" )]
        public static extern bool SetWindowPos( IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags );

        [DllImport( "user32.dll" )]
        public static extern IntPtr GetDesktopWindow();

        public struct Windowplacement
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        #region ShowWindowEnum
        public enum ShowWindowEnum
        {
            /// <summary>
            ///   Hides the window and activates another window.
            /// </summary>
            Hide = 0,

            /// <summary>
            ///   Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.
            /// </summary>
            Normal = 1,

            /// <summary>
            ///   Activates the window and displays it as a minimized window.
            /// </summary>
            ShowMinimized = 2,

            /// <summary>
            ///   Maximizes the specified window.
            /// </summary>
            Maximize = 3, // is this the right value?
            /// <summary>
            ///   Activates the window and displays it as a maximized window.
            /// </summary>
            ShowMaximized = 3,

            /// <summary>
            ///   Displays a window in its most recent size and position. This value is similar to <see
            ///    cref="Win32.ShowWindowCommand.Normal" /> , except the window is not activated.
            /// </summary>
            ShowNoActivate = 4,

            /// <summary>
            ///   Activates the window and displays it in its current size and position.
            /// </summary>
            Show = 5,

            /// <summary>
            ///   Minimizes the specified window and activates the next top-level window in the Z order.
            /// </summary>
            Minimize = 6,

            /// <summary>
            ///   Displays the window as a minimized window. This value is similar to <see cref="Win32.ShowWindowCommand.ShowMinimized" /> , except the window is not activated.
            /// </summary>
            ShowMinNoActive = 7,

            /// <summary>
            ///   Displays the window in its current size and position. This value is similar to <see cref="Win32.ShowWindowCommand.Show" /> , except the window is not activated.
            /// </summary>
            ShowNA = 8,

            /// <summary>
            ///   Activates and displays the window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window.
            /// </summary>
            Restore = 9,

            /// <summary>
            ///   Sets the show state based on the SW_* value specified in the STARTUPINFO structure passed to the CreateProcess function by the program that started the application.
            /// </summary>
            ShowDefault = 10,

            /// <summary>
            ///   <b>Windows 2000/XP:</b> Minimizes a window, even if the thread that owns the window is not responding. This flag should only be used when minimizing windows from a different thread.
            /// </summary>
            ForceMinimize = 11
        }

        #endregion
    }
}