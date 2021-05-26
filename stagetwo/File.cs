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

        public static void open(System.IO.StreamReader stdin)
        {
            System.String filename = stdin.ReadLine();
            System.String mode = stdin.ReadLine();
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
                int error = Marshal.GetLastWin32Error();
                System.Console.Write("E:");
                System.Console.WriteLine(error);
                return;
            }

            System.Console.WriteLine(handle);
        }

        public static void read(System.IO.StreamReader stdin)
        {
            System.String line;
            IntPtr handle;
            uint count;
            uint nreceived;

            line = stdin.ReadLine();
            handle = new IntPtr(System.UInt32.Parse(line));
            line = stdin.ReadLine();
            count = System.UInt32.Parse(line);

            byte[] buffer = new byte[count];

            if (!Win32.ReadFile(handle, buffer, count, out nreceived, IntPtr.Zero))
            {
                System.Console.WriteLine("0");
                return;
            }

            System.Console.WriteLine(nreceived);

            using (Stream out_stream = System.Console.OpenStandardOutput())
            {
                out_stream.Write(buffer, 0, (int)nreceived);
            }

            return;
        }

        public static void write(System.IO.StreamReader stdin)
        {
            System.String line;
            IntPtr handle;
            uint nwritten;
            System.IO.MemoryStream script;

            line = stdin.ReadLine();
            handle = new IntPtr(System.UInt32.Parse(line));

            try
            {
                var stream = new System.IO.MemoryStream(System.Convert.FromBase64String(stdin.ReadLine()));
                var gz = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress);
                script = new System.IO.MemoryStream();
                gz.CopyTo(script);
            }
            catch (Exception)
            {
                System.Console.WriteLine("E:DECODE");
                return;
            }

            if (!Win32.WriteFile(handle, script.ToArray(), (uint)script.Length, out nwritten, IntPtr.Zero))
            {
                System.Console.WriteLine("0");
                return;
            }

            System.Console.WriteLine(nwritten);
            return;
        }

        public static void close(System.IO.StreamReader stdin)
        {
            IntPtr handle = new IntPtr(System.UInt32.Parse(stdin.ReadLine()));
            Win32.CloseHandle(handle);
        }

    }
}
