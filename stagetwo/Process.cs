using System;
using System.Runtime.InteropServices;

namespace stagetwo
{
    class Process
    {
        public static void start()
        {
            IntPtr stdin_read, stdin_write;
            IntPtr stdout_read, stdout_write;
            IntPtr stderr_read, stderr_write;
            Win32.SECURITY_ATTRIBUTES pSec = new Win32.SECURITY_ATTRIBUTES();
            Win32.STARTUPINFO pInfo = new Win32.STARTUPINFO();
            Win32.PROCESS_INFORMATION childInfo = new Win32.PROCESS_INFORMATION();
            System.String command = System.Console.ReadLine();

            pSec.nLength = Marshal.SizeOf(pSec);
            pSec.bInheritHandle = 1;
            pSec.lpSecurityDescriptor = IntPtr.Zero;

            if (!Win32.CreatePipe(out stdin_read, out stdin_write, ref pSec, Win32.BUFFER_SIZE_PIPE))
            {
                System.Console.WriteLine("E:IN");
                return;
            }

            if (!Win32.CreatePipe(out stdout_read, out stdout_write, ref pSec, Win32.BUFFER_SIZE_PIPE))
            {
                System.Console.WriteLine("E:OUT");
                return;
            }

            if (!Win32.CreatePipe(out stderr_read, out stderr_write, ref pSec, Win32.BUFFER_SIZE_PIPE))
            {
                System.Console.WriteLine("E:ERR");
                return;
            }

            pInfo.cb = Marshal.SizeOf(pInfo);
            pInfo.hStdError = stderr_write;
            pInfo.hStdOutput = stdout_write;
            pInfo.hStdInput = stdin_read;
            pInfo.dwFlags |= (Int32)Win32.STARTF_USESTDHANDLES;

            if (!Win32.CreateProcessW(null, command, IntPtr.Zero, IntPtr.Zero, true, 0, IntPtr.Zero, null, ref pInfo, out childInfo))
            {
                System.Console.WriteLine("E:PROC");
                return;
            }

            Win32.CloseHandle(stdin_read);
            Win32.CloseHandle(stdout_write);
            Win32.CloseHandle(stderr_write);

            System.Console.WriteLine(childInfo.hProcess);
            System.Console.WriteLine(stdin_write);
            System.Console.WriteLine(stdout_read);
            System.Console.WriteLine(stderr_read);
        }

        public static void poll()
        {
            IntPtr hProcess = new IntPtr(System.UInt32.Parse(System.Console.ReadLine()));
            System.UInt32 result = Win32.WaitForSingleObject(hProcess, 0);

            if (result == 0x00000102L)
            {
                System.Console.WriteLine("R");
                return;
            }
            else if (result == 0xFFFFFFFF)
            {
                System.Console.WriteLine("E");
                return;
            }

            if (!Win32.GetExitCodeProcess(hProcess, out result))
            {
                System.Console.WriteLine("E");
            }

            System.Console.WriteLine(result);
        }

        public static void kill()
        {
            IntPtr hProcess = new IntPtr(System.UInt32.Parse(System.Console.ReadLine()));
            UInt32 code = System.UInt32.Parse(System.Console.ReadLine());
            Win32.TerminateProcess(hProcess, code);
        }

    }
}
