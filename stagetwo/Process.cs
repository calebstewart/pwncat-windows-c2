using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static stagetwo.Protocol;

namespace stagetwo
{
    class Process
    {
        public static Dictionary<string, object> start(string command)
        {
            IntPtr stdin_read, stdin_write;
            IntPtr stdout_read, stdout_write;
            IntPtr stderr_read, stderr_write;
            Win32.SECURITY_ATTRIBUTES pSec = new Win32.SECURITY_ATTRIBUTES();
            Win32.STARTUPINFO pInfo = new Win32.STARTUPINFO();
            Win32.PROCESS_INFORMATION childInfo = new Win32.PROCESS_INFORMATION();

            pSec.nLength = Marshal.SizeOf(pSec);
            pSec.bInheritHandle = 1;
            pSec.lpSecurityDescriptor = IntPtr.Zero;

            if (!Win32.CreatePipe(out stdin_read, out stdin_write, ref pSec, Win32.BUFFER_SIZE_PIPE))
            {
                throw new ProtocolError(Marshal.GetLastWin32Error(), "create stdin pipe failed");
            }

            if (!Win32.CreatePipe(out stdout_read, out stdout_write, ref pSec, Win32.BUFFER_SIZE_PIPE))
            {
                Win32.CloseHandle(stdin_read);
                Win32.CloseHandle(stdin_write);
                throw new ProtocolError(Marshal.GetLastWin32Error(), "create stdout pipe failed");
            }

            if (!Win32.CreatePipe(out stderr_read, out stderr_write, ref pSec, Win32.BUFFER_SIZE_PIPE))
            {
                Win32.CloseHandle(stdout_read);
                Win32.CloseHandle(stdout_write);
                Win32.CloseHandle(stdin_read);
                Win32.CloseHandle(stdin_write);
                throw new ProtocolError(Marshal.GetLastWin32Error(), "create stderr pipe failed");
            }

            pInfo.cb = Marshal.SizeOf(pInfo);
            pInfo.hStdError = stderr_write;
            pInfo.hStdOutput = stdout_write;
            pInfo.hStdInput = stdin_read;
            pInfo.dwFlags |= (Int32)Win32.STARTF_USESTDHANDLES;

            if (!Win32.CreateProcessW(null, command, IntPtr.Zero, IntPtr.Zero, true, 0, IntPtr.Zero, null, ref pInfo, out childInfo))
            {
                Win32.CloseHandle(stdin_read);
                Win32.CloseHandle(stdin_write);
                Win32.CloseHandle(stdout_read);
                Win32.CloseHandle(stdout_write);
                Win32.CloseHandle(stderr_read);
                Win32.CloseHandle(stderr_write);
                throw new Protocol.ProtocolError(Marshal.GetLastWin32Error(), "create process failed");
            }

            Win32.CloseHandle(stdin_read);
            Win32.CloseHandle(stdout_write);
            Win32.CloseHandle(stderr_write);

            return new Dictionary<string, object>()
            {
                { "handle", childInfo.hProcess },
                { "stdin", stdin_write },
                { "stdout", stdout_read },
                { "stderr", stderr_read }
            };
        }

        public static Dictionary<string, object> poll(int iHandle)
        {
            IntPtr hProcess = new IntPtr(iHandle);
            UInt32 result = Win32.WaitForSingleObject(hProcess, 0);
            UInt32 exit_code = 0;

            if( result == 0xFFFFFFFF)
            {
                throw new ProtocolError(Marshal.GetLastWin32Error(), "wait for single object failed");
            }

            if (result == 0x00000102L)
            {
                return new Dictionary<string, object>()
                {
                    { "stopped", false }
                };
            }

            if (!Win32.GetExitCodeProcess(hProcess, out exit_code))
            {
                return new Dictionary<string, object>()
                {
                    { "stopped", true },
                    { "code", null }
                };
            }

            return new Dictionary<string, object>()
            {
                { "stopped", true },
                { "code", (int)exit_code }
            };
        }

        public static void kill(int iHandle, int code)
        {
            Win32.TerminateProcess(new IntPtr(iHandle), (UInt32)code);
        }

    }
}
