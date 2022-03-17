using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static GPSaveConverter.NativeMethods;

namespace GPSaveConverter
{
    public partial class RichTextBoxFixedForFriendlyLinks : RichTextBox
    {
        [DllImport("user32.dll")]
        static extern bool HideCaret(IntPtr hWnd);

        internal ENLINK ConvertFromENLINK64(ENLINK64 es64)
        {
            // Note: the RichTextBox.ConvertFromENLINK64 method is written using C# unsafe code
            // this is version uses a GCHandle to pin the byte array so that 
            // the same Marshal.Read_Xyz methods can be used

            var es = new ENLINK();
            GCHandle? hndl = null;
            try
            {
                hndl = GCHandle.Alloc(es64.contents, GCHandleType.Pinned);
                var es64p = hndl.Value.AddrOfPinnedObject();
                es.nmhdr = new NMHDR();
                es.charrange = new CHARRANGE();
                es.nmhdr.hwndFrom = Marshal.ReadIntPtr(es64p);
                es.nmhdr.idFrom = Marshal.ReadIntPtr(es64p + 8);
                es.nmhdr.code = Marshal.ReadInt32(es64p + 16);
                es.msg = Marshal.ReadInt32(es64p + 24);
                es.wParam = Marshal.ReadIntPtr(es64p + 28);
                es.lParam = Marshal.ReadIntPtr(es64p + 36);
                es.charrange.cpMin = Marshal.ReadInt32(es64p + 44);
                es.charrange.cpMax = Marshal.ReadInt32(es64p + 48);
            }
            finally
            {
                if (hndl.HasValue)
                    hndl.Value.Free();
            }

            return es;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ReflectNotify)
            {
                NMHDR hdr = (NMHDR)m.GetLParam(typeof(NMHDR));
                if (hdr.code == EN_Link)
                {
                    ENLINK lnk;
                    if (IntPtr.Size == 4)
                    {
                        lnk = (ENLINK)m.GetLParam(typeof(ENLINK));
                    }
                    else
                    {
                        lnk = ConvertFromENLINK64((ENLINK64)m.GetLParam(typeof(ENLINK64)));
                    }

                    if (lnk.msg == WM_LBUTTONDOWN)
                    {
                        string linkUrl = CharRangeToString(lnk.charrange);
                        // Still check if linkUrl is not empty
                        if (!string.IsNullOrEmpty(linkUrl))
                        {
                            OnLinkClicked(new LinkClickedEventArgs(linkUrl));
                        }

                        m.Result = new IntPtr(1);
                        return;
                    }
                }
            }

            HideCaret(this.Handle);
            base.WndProc(ref m);
        }

        private string CharRangeToString(CHARRANGE c)
        {
            string ret = string.Empty;
            var txrg = new TEXTRANGE() { chrg = c };

            // 'Windows bug: 64-bit windows returns a bad range for us.  VSWhidbey 504502.  
            // 'Putting in a hack to avoid an unhandled exception.
            // If c.cpMax > Text.Length OrElse c.cpMax - c.cpMin <= 0 Then
            // Return String.Empty
            // End If

            // *********
            // c.cpMax can be greater than Text.Length if using friendly links
            // with RichEdit50. so that check is not valid.  

            // instead of the hack above, first check that the number of characters is positive 
            // and then use the result of sending EM_GETTEXTRANGE  to handle the 
            // possibilty of Text.Length < c.cpMax
            // *********

            int numCharacters = c.cpMax - c.cpMin + 1; // +1 for null termination
            if (numCharacters > 0)
            {
                var charBuffer = default(CharBuffer);
                charBuffer = CharBuffer.CreateBuffer(numCharacters);
                IntPtr unmanagedBuffer = IntPtr.Zero;
                try
                {
                    unmanagedBuffer = charBuffer.AllocCoTaskMem();
                    if (unmanagedBuffer == IntPtr.Zero)
                    {
                        throw new OutOfMemoryException();
                    }

                    txrg.lpstrText = unmanagedBuffer;
                    IntPtr len = SendMessage(new HandleRef(this, Handle), EM_GETTEXTRANGE, 0, txrg);
                    if (len != IntPtr.Zero)
                    {
                        charBuffer.PutCoTaskMem(unmanagedBuffer);
                        ret = charBuffer.GetString();
                    }
                }
                finally
                {
                    if (txrg.lpstrText != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(unmanagedBuffer);
                    }
                }
            }

            return ret;
        }
    }
}
