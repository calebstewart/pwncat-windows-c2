using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace stagetwo
{
    class File
    {

        public static Dictionary<string, object> open(string filename, string mode)
        {
            uint desired_access = 0;
            uint creation_disposition = Win32.OPEN_EXISTING;
            IntPtr handle;

            if (mode.Contains("r"))
            {
                desired_access |= Win32.GENERIC_READ;
            }
            if (mode.Contains("w"))
            {
                desired_access |= Win32.GENERIC_WRITE;
                creation_disposition = Win32.CREATE_ALWAYS;
            }

            handle = Win32.CreateFile(filename, desired_access, 0, IntPtr.Zero, creation_disposition, Win32.FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

            if (handle == (new IntPtr(-1)))
            {
                throw new Protocol.ProtocolError(Marshal.GetLastWin32Error(), "failed to open file");
            }

            return new Dictionary<string, object>()
            {
                { "handle", (UInt32)handle }
            };
        }

        public static Dictionary<string, object> read(int uiHandle, int count)
        {
            IntPtr handle = new IntPtr(uiHandle);
            byte[] buffer = new byte[count];
            uint nreceived;

            if (!Win32.ReadFile(handle, buffer, (uint)count, out nreceived, IntPtr.Zero))
            {
                throw new Protocol.ProtocolError(Marshal.GetLastWin32Error(), "failed to read data + " + count.ToString());
            }

            return new Dictionary<string, object>(){
                { "data", Convert.ToBase64String(buffer, 0, (int)nreceived) }
            };
        }

        public static Dictionary<string, object> write(int iHandle, string szData)
        {
            IntPtr handle = new IntPtr(iHandle);
            uint nwritten;
            byte[] data = Convert.FromBase64String(szData);


            if (!Win32.WriteFile(handle, data, (uint)data.Length, out nwritten, IntPtr.Zero))
            {
                throw new Protocol.ProtocolError(Marshal.GetLastWin32Error(), "failed to write data");
            }

            return new Dictionary<string, object>()
            {
                { "count", nwritten }
            };
        }

        public static Dictionary<string, object> close(int iHandle)
        {
            Win32.CloseHandle(new IntPtr(iHandle));
            return new Dictionary<string, object>() { };
        }

    }
}
