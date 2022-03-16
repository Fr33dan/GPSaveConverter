using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GPSaveConverter
{
    public partial class NativeMethods
    {
        internal const int EN_Link = 0x70B;
        internal const int WM_NOTIFY = 0x4E;
        internal const int WM_User = 0x400;
        internal const int WM_REFLECT = WM_User + 0x1C00;
        internal const int WM_ReflectNotify = WM_REFLECT | WM_NOTIFY;
        internal const int WM_LBUTTONDOWN = 0x201;
        internal const int EM_GETTEXTRANGE = WM_User + 75;

        public partial struct NMHDR
        {
            public IntPtr hwndFrom;
            public IntPtr idFrom; // This is declared as UINT_PTR in winuser.h
            public int code;
        }

        [StructLayout(LayoutKind.Sequential)]
        public partial class ENLINK
        {
            public NMHDR nmhdr;
            public int msg = 0;
            public IntPtr wParam = IntPtr.Zero;
            public IntPtr lParam = IntPtr.Zero;
            public CHARRANGE charrange = null;
        }

        [StructLayout(LayoutKind.Sequential)]
        public partial class ENLINK64
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 56)]
            public byte[] contents = new byte[56];
        }

        [StructLayout(LayoutKind.Sequential)]
        public partial class CHARRANGE
        {
            public int cpMin;
            public int cpMax;
        }

        [StructLayout(LayoutKind.Sequential)]
        public partial class TEXTRANGE
        {
            public CHARRANGE chrg;
            public IntPtr lpstrText; // allocated by caller, zero terminated by RichEdit
        }

        public abstract partial class CharBuffer
        {
            public static CharBuffer CreateBuffer(int size)
            {
                if (Marshal.SystemDefaultCharSize == 1)
                {
                    return new AnsiCharBuffer(size);
                }

                return new UnicodeCharBuffer(size);
            }

            public abstract IntPtr AllocCoTaskMem();
            public abstract string GetString();
            public abstract void PutCoTaskMem(IntPtr ptr);
            public abstract void PutString(string s);
        }

        public partial class AnsiCharBuffer : CharBuffer
        {
            internal byte[] buffer;
            internal int offset;

            public AnsiCharBuffer(int size)
            {
                buffer = new byte[size];
            }

            public override IntPtr AllocCoTaskMem()
            {
                var result = Marshal.AllocCoTaskMem(buffer.Length);
                Marshal.Copy(buffer, 0, result, buffer.Length);
                return result;
            }

            public override string GetString()
            {
                int i = offset;
                while (i < buffer.Length && buffer[i] != 0)
                    i += 1;
                string result = Encoding.Default.GetString(buffer, offset, i - offset);
                if (i < buffer.Length)
                {
                    i += 1;
                }

                offset = i;
                return result;
            }

            public override void PutCoTaskMem(IntPtr ptr)
            {
                Marshal.Copy(ptr, buffer, 0, buffer.Length);
                offset = 0;
            }

            public override void PutString(string s)
            {
                var bytes = Encoding.Default.GetBytes(s);
                int count = Math.Min(bytes.Length, buffer.Length - offset);
                Array.Copy(bytes, 0, buffer, offset, count);
                offset += count;
                if (offset < buffer.Length)
                {
                    buffer[offset] = 0;
                    offset += 1;
                }
            }
        }

        public partial class UnicodeCharBuffer : CharBuffer
        {
            internal char[] buffer;
            internal int offset;

            public UnicodeCharBuffer(int size)
            {
                buffer = new char[size];
            }

            public override IntPtr AllocCoTaskMem()
            {
                var result = Marshal.AllocCoTaskMem(buffer.Length * 2);
                Marshal.Copy(buffer, 0, result, buffer.Length);
                return result;
            }

            public override string GetString()
            {
                int i = offset;
                while (i < buffer.Length && buffer[i] != 0)
                    i += 1;
                string result = new string(buffer, offset, i - offset);
                if (i < buffer.Length)
                {
                    i += 1;
                }

                offset = i;
                return result;
            }

            public override void PutCoTaskMem(IntPtr ptr)
            {
                Marshal.Copy(ptr, buffer, 0, buffer.Length);
                offset = 0;
            }

            public override void PutString(string s)
            {
                int count = Math.Min(s.Length, buffer.Length - offset);
                s.CopyTo(0, buffer, offset, count);
                offset += count;
                if (offset < buffer.Length)
                {
                    buffer[offset] = '\0';
                    offset += 1;
                }
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, TEXTRANGE lParam);
    }
}
