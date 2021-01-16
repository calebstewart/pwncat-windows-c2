using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Management.Automation.Runspaces;
using System.Management.Automation;
using System.Collections.ObjectModel;


namespace stagetwo
{
    public class StageTwo
    {

        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
        private const uint ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002;
        private const uint PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;
        private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
        private const uint CREATE_NO_WINDOW = 0x08000000;
        private const int STARTF_USESTDHANDLES = 0x00000100;
        private const int BUFFER_SIZE_PIPE = 1048576;

        private const UInt32 INFINITE = 0xFFFFFFFF;
        private const int SW_HIDE = 0;
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint OPEN_EXISTING = 3;
        private const uint OPEN_ALWAYS = 4;
        private const uint TRUNCATE_EXISTING = 5;
        private const int STD_INPUT_HANDLE = -10;
        private const int STD_OUTPUT_HANDLE = -11;
        private const int STD_ERROR_HANDLE = -12;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFOEX
        {
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COORD
        {
            public short X;
            public short Y;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DuplicateHandle(IntPtr hSourceProcess, IntPtr hSource, IntPtr hTargetProcess, out IntPtr lpTarget, uint dwDesiredAccess, bool bInheritHandle, uint dwOptions);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CancelIoEx(IntPtr hFile, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFOEX lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CreateProcessW(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetExitCodeProcess(IntPtr hProcess, out UInt32 lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CreatePipe(out IntPtr hReadPipe, out IntPtr hWritePipe, ref SECURITY_ATTRIBUTES lpPipeAttributes, int nSize);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr SecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(IntPtr hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int CreatePseudoConsole(COORD size, IntPtr hInput, IntPtr hOutput, uint dwFlags, out IntPtr phPC);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int ClosePseudoConsole(IntPtr hPC);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr handle, out uint mode);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool FreeConsole();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        private static extern bool FlushFileBuffers(IntPtr hFile);

        private System.IO.Stream stdin_stream;
        private System.IO.StreamReader stdin;
        private bool bad_stdin;

        public System.String ReadUntilLine(System.String delimeter)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            while (true)
            {
                System.String line = stdin.ReadLine();
                if (line == delimeter)
                {
                    break;
                }
                builder.AppendLine(line);
            }

            return builder.ToString();
        }

        public void main()
        {
            object[] args = new object[] { };

            // .NET is Dumb
            IntPtr hStdIn = GetStdHandle(STD_INPUT_HANDLE);
            stdin_stream = new System.IO.FileStream(hStdIn, FileAccess.Read, false);
            stdin = new System.IO.StreamReader(stdin_stream);
            bad_stdin = false; // GetFileType(hStdIn) != 0x0003;

            System.Console.WriteLine("READY");

            while (true)
            {
                try
                {
                    String line = "";
                    try
                    {
                        line = stdin.ReadLine();
                    } catch (Exception e)
                    {
                        stdin_stream = System.Console.OpenStandardInput();
                        stdin = new System.IO.StreamReader(stdin_stream);
                        bad_stdin = false;
                    }
                    var method = GetType().GetMethod(line);
                    if (method == null) continue;
                    method.Invoke(this, args);
                } catch ( Exception e )
                {
                    System.Console.WriteLine("E:S2:EXCEPTION:" + e.Message);
                    break;
                }
            }
        }

        public void process()
        {
            IntPtr stdin_read, stdin_write;
            IntPtr stdout_read, stdout_write;
            IntPtr stderr_read, stderr_write;
            SECURITY_ATTRIBUTES pSec = new SECURITY_ATTRIBUTES();
            STARTUPINFO pInfo = new STARTUPINFO();
            PROCESS_INFORMATION childInfo = new PROCESS_INFORMATION();
            System.String command = stdin.ReadLine();

            pSec.nLength = Marshal.SizeOf(pSec);
            pSec.bInheritHandle = 1;
            pSec.lpSecurityDescriptor = IntPtr.Zero;

            if (!CreatePipe(out stdin_read, out stdin_write, ref pSec, BUFFER_SIZE_PIPE))
            {
                System.Console.WriteLine("E:IN");
                return;
            }

            if (!CreatePipe(out stdout_read, out stdout_write, ref pSec, BUFFER_SIZE_PIPE))
            {
                System.Console.WriteLine("E:OUT");
                return;
            }

            if (!CreatePipe(out stderr_read, out stderr_write, ref pSec, BUFFER_SIZE_PIPE))
            {
                System.Console.WriteLine("E:ERR");
                return;
            }

            pInfo.cb = Marshal.SizeOf(pInfo);
            pInfo.hStdError = stderr_write;
            pInfo.hStdOutput = stdout_write;
            pInfo.hStdInput = stdin_read;
            pInfo.dwFlags |= (Int32)STARTF_USESTDHANDLES;

            if (!CreateProcessW(null, command, IntPtr.Zero, IntPtr.Zero, true, 0, IntPtr.Zero, null, ref pInfo, out childInfo))
            {
                System.Console.WriteLine("E:PROC");
                return;
            }

            CloseHandle(stdin_read);
            CloseHandle(stdout_write);
            CloseHandle(stderr_write);

            System.Console.WriteLine(childInfo.hProcess);
            System.Console.WriteLine(stdin_write);
            System.Console.WriteLine(stdout_read);
            System.Console.WriteLine(stderr_read);
        }

        public void ppoll()
        {
            IntPtr hProcess = new IntPtr(System.UInt32.Parse(stdin.ReadLine()));
            System.UInt32 result = WaitForSingleObject(hProcess, 0);

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

            if (!GetExitCodeProcess(hProcess, out result))
            {
                System.Console.WriteLine("E");
            }

            System.Console.WriteLine(result);
        }

        public void kill()
        {
            IntPtr hProcess = new IntPtr(System.UInt32.Parse(stdin.ReadLine()));
            UInt32 code = System.UInt32.Parse(stdin.ReadLine());
            TerminateProcess(hProcess, code);
        }

        public void open()
        {
            System.String filename = stdin.ReadLine();
            System.String mode = stdin.ReadLine();
            uint desired_access = GENERIC_READ;
            uint creation_disposition = OPEN_EXISTING;
            IntPtr handle;

            if (mode.Contains("r"))
            {
                desired_access |= GENERIC_READ;
            }
            if (mode.Contains("w"))
            {
                desired_access |= GENERIC_WRITE;
                creation_disposition = TRUNCATE_EXISTING;
            }

            handle = CreateFile(filename, desired_access, 0, IntPtr.Zero, creation_disposition, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);

            if (handle == (new IntPtr(-1)))
            {
                int error = Marshal.GetLastWin32Error();
                System.Console.Write("E:");
                System.Console.WriteLine(error);
                return;
            }

            System.Console.WriteLine(handle);
        }

        public void read()
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

            if (!ReadFile(handle, buffer, count, out nreceived, IntPtr.Zero))
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

        public void write()
        {
            System.String line;
            IntPtr handle;
            uint count;
            uint nwritten;

            line = stdin.ReadLine();
            handle = new IntPtr(System.UInt32.Parse(line));
            line = stdin.ReadLine();
            count = System.UInt32.Parse(line);

            byte[] buffer = new byte[count];

            count = (uint)stdin_stream.Read(buffer, 0, (int)count);

            if (!WriteFile(handle, buffer, count, out nwritten, IntPtr.Zero))
            {
                System.Console.WriteLine("0");
                return;
            }

            System.Console.WriteLine(nwritten);
            return;
        }

        public void close()
        {
            IntPtr handle = new IntPtr(System.UInt32.Parse(stdin.ReadLine()));
            CloseHandle(handle);
        }

        public void powershell()
        {
            System.IO.MemoryStream script;
            try
            {
                var stream = new System.IO.MemoryStream(System.Convert.FromBase64String(stdin.ReadLine()));
                var gz = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress);
                script = new System.IO.MemoryStream();
                gz.CopyTo(script);
            } catch ( Exception e )
            {
                System.Console.WriteLine("E:DECODE");
                return;
            }

            Runspace rs = RunspaceFactory.CreateRunspace();
            rs.Open();

            PowerShell ps = PowerShell.Create();
            ps.Runspace = rs;

            ps.AddScript(System.Text.Encoding.UTF8.GetString(script.ToArray()));

            Collection<PSObject> results = ps.Invoke();
            rs.Close();

            foreach( var item in results)
            {
                System.Console.WriteLine(item.ToString());
            }
        }

        public void csharp()
        {
            var cp = new System.CodeDom.Compiler.CompilerParameters()
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
            };

            while (true)
            {
                System.String line = stdin.ReadLine();
                if (line == "/* ENDASM */") break;
                cp.ReferencedAssemblies.Add(line);
            }

            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.Core.dll");
            cp.ReferencedAssemblies.Add("System.Dynamic.dll");
            cp.ReferencedAssemblies.Add("Microsoft.CSharp.dll");

            var r = new Microsoft.CSharp.CSharpCodeProvider().CompileAssemblyFromSource(cp, ReadUntilLine("/* ENDBLOCK */"));
            if (r.Errors.HasErrors)
            {
                return;
            }

            var obj = r.CompiledAssembly.CreateInstance("command");
            obj.GetType().GetMethod("main").Invoke(obj, new object[] { });
        }

        public void interactive()
        {
            UInt32 rows = System.UInt32.Parse(stdin.ReadLine());
            UInt32 cols = System.UInt32.Parse(stdin.ReadLine());

            SpawnConPtyShell(rows, cols, "powershell.exe");

            System.Console.WriteLine("");
            System.Console.WriteLine("INTERACTIVE_COMPLETE");
        }

        private static void CreatePipes(ref IntPtr InputPipeRead, ref IntPtr InputPipeWrite, ref IntPtr OutputPipeRead, ref IntPtr OutputPipeWrite)
        {
            SECURITY_ATTRIBUTES pSec = new SECURITY_ATTRIBUTES();
            pSec.nLength = Marshal.SizeOf(pSec);
            pSec.bInheritHandle = 1;
            pSec.lpSecurityDescriptor = IntPtr.Zero;
            if (!CreatePipe(out InputPipeRead, out InputPipeWrite, ref pSec, BUFFER_SIZE_PIPE))
                throw new InvalidOperationException("Could not create the InputPipe");
            if (!CreatePipe(out OutputPipeRead, out OutputPipeWrite, ref pSec, BUFFER_SIZE_PIPE))
                throw new InvalidOperationException("Could not create the OutputPipe");
        }

        private static void InitConsole(ref IntPtr oldStdIn, ref IntPtr oldStdOut, ref IntPtr oldStdErr)
        {
            oldStdIn = GetStdHandle(STD_INPUT_HANDLE);
            oldStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            oldStdErr = GetStdHandle(STD_ERROR_HANDLE);
            IntPtr hStdout = CreateFile("CONOUT$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
            IntPtr hStdin = CreateFile("CONIN$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
            SetStdHandle(STD_OUTPUT_HANDLE, hStdout);
            SetStdHandle(STD_ERROR_HANDLE, hStdout);
            SetStdHandle(STD_INPUT_HANDLE, hStdin);
        }

        private static void RestoreStdHandles(IntPtr oldStdIn, IntPtr oldStdOut, IntPtr oldStdErr)
        {
            SetStdHandle(STD_OUTPUT_HANDLE, oldStdOut);
            SetStdHandle(STD_ERROR_HANDLE, oldStdErr);
            SetStdHandle(STD_INPUT_HANDLE, oldStdIn);
        }

        private static void EnableVirtualTerminalSequenceProcessing()
        {
            uint outConsoleMode = 0, inConsoleMode = 0;
            IntPtr hStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            IntPtr hStdIn = GetStdHandle(STD_INPUT_HANDLE);
            if (!GetConsoleMode(hStdOut, out outConsoleMode))
            {
                throw new InvalidOperationException("Could not get console mode");
            }
            outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
            if (!SetConsoleMode(hStdOut, outConsoleMode))
            {
                throw new InvalidOperationException("Could not enable virtual terminal processing");
            }
        }

        private static int CreatePseudoConsoleWithPipes(ref IntPtr handlePseudoConsole, ref IntPtr ConPtyInputPipeRead, ref IntPtr ConPtyOutputPipeWrite, uint rows, uint cols)
        {
            int result = -1;
            EnableVirtualTerminalSequenceProcessing();
            COORD consoleCoord = new COORD();
            consoleCoord.X = (short)cols;
            consoleCoord.Y = (short)rows;
            result = CreatePseudoConsole(consoleCoord, ConPtyInputPipeRead, ConPtyOutputPipeWrite, 0, out handlePseudoConsole);
            return result;
        }

        private static STARTUPINFOEX ConfigureProcessThread(IntPtr handlePseudoConsole, IntPtr attributes)
        {
            IntPtr lpSize = IntPtr.Zero;
            bool success = InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref lpSize);
            if (success || lpSize == IntPtr.Zero)
            {
                throw new InvalidOperationException("Could not calculate the number of bytes for the attribute list. " + Marshal.GetLastWin32Error());
            }
            STARTUPINFOEX startupInfo = new STARTUPINFOEX();
            startupInfo.StartupInfo.cb = Marshal.SizeOf(startupInfo);
            startupInfo.lpAttributeList = Marshal.AllocHGlobal(lpSize);
            success = InitializeProcThreadAttributeList(startupInfo.lpAttributeList, 1, 0, ref lpSize);
            if (!success)
            {
                throw new InvalidOperationException("Could not set up attribute list. " + Marshal.GetLastWin32Error());
            }
            success = UpdateProcThreadAttribute(startupInfo.lpAttributeList, 0, attributes, handlePseudoConsole, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero);
            if (!success)
            {
                throw new InvalidOperationException("Could not set pseudoconsole thread attribute. " + Marshal.GetLastWin32Error());
            }
            return startupInfo;
        }

        private static PROCESS_INFORMATION RunProcess(ref STARTUPINFOEX sInfoEx, string commandLine)
        {
            PROCESS_INFORMATION pInfo = new PROCESS_INFORMATION();
            SECURITY_ATTRIBUTES pSec = new SECURITY_ATTRIBUTES();
            int securityAttributeSize = Marshal.SizeOf(pSec);
            pSec.nLength = securityAttributeSize;
            SECURITY_ATTRIBUTES tSec = new SECURITY_ATTRIBUTES();
            tSec.nLength = securityAttributeSize;
            bool success = CreateProcess(null, commandLine, ref pSec, ref tSec, false, EXTENDED_STARTUPINFO_PRESENT, IntPtr.Zero, null, ref sInfoEx, out pInfo);
            if (!success)
            {
                throw new InvalidOperationException("Could not create process. " + Marshal.GetLastWin32Error());
            }
            return pInfo;
        }

        private static PROCESS_INFORMATION CreateChildProcessWithPseudoConsole(IntPtr handlePseudoConsole, string commandLine)
        {
            STARTUPINFOEX startupInfo = ConfigureProcessThread(handlePseudoConsole, (IntPtr)PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE);
            PROCESS_INFORMATION processInfo = RunProcess(ref startupInfo, commandLine);
            return processInfo;
        }

        private bool SpawnConPtyShell(uint rows, uint cols, string commandLine)
        {
            IntPtr InputPipeRead = new IntPtr(0);
            IntPtr InputPipeWrite = new IntPtr(0);
            IntPtr OutputPipeRead = new IntPtr(0);
            IntPtr OutputPipeWrite = new IntPtr(0);
            IntPtr handlePseudoConsole = new IntPtr(0);
            IntPtr oldStdIn = new IntPtr(0);
            IntPtr oldStdOut = new IntPtr(0);
            IntPtr oldStdErr = new IntPtr(0);
            bool newConsoleAllocated = false;
            PROCESS_INFORMATION childProcessInfo = new PROCESS_INFORMATION();

            CreatePipes(ref InputPipeRead, ref InputPipeWrite, ref OutputPipeRead, ref OutputPipeWrite);
            InitConsole(ref oldStdIn, ref oldStdOut, ref oldStdErr);
            if ( GetProcAddress(GetModuleHandle("kernel32"), "CreatePseudoConsole") == IntPtr.Zero)
            {
                STARTUPINFO sInfo = new STARTUPINFO();
                sInfo.cb = Marshal.SizeOf(sInfo);
                sInfo.dwFlags |= (Int32)STARTF_USESTDHANDLES;
                sInfo.hStdInput = InputPipeRead;
                sInfo.hStdOutput = OutputPipeWrite;
                sInfo.hStdError = OutputPipeWrite;
                CreateProcessW(null, commandLine, IntPtr.Zero, IntPtr.Zero, true, 0, IntPtr.Zero, null, ref sInfo, out childProcessInfo);
            }
            else
            {
                if (GetConsoleWindow() == IntPtr.Zero)
                {
                    AllocConsole();
                    ShowWindow(GetConsoleWindow(), SW_HIDE);
                    newConsoleAllocated = true;
                }
                int pseudoConsoleCreationResult = CreatePseudoConsoleWithPipes(ref handlePseudoConsole, ref InputPipeRead, ref OutputPipeWrite, rows, cols);
                if (pseudoConsoleCreationResult != 0)
                {
                    Console.WriteLine("E:CREATE_PTY");
                    return false;
                }
                childProcessInfo = CreateChildProcessWithPseudoConsole(handlePseudoConsole, commandLine);
            }
            // Note: We can close the handles to the PTY-end of the pipes here
            // because the handles are dup'ed into the ConHost and will be released
            // when the ConPTY is destroyed.
            if (InputPipeRead != IntPtr.Zero) CloseHandle(InputPipeRead);
            if (OutputPipeWrite != IntPtr.Zero) CloseHandle(OutputPipeWrite);
            //Threads have better performance than Tasks
            Thread thThreadReadPipeWriteSocket = null;
            Thread thReadSocketWritePipe = null;

            thThreadReadPipeWriteSocket = new Thread(pipe_thread);
            thReadSocketWritePipe = new Thread(pipe_thread);
            thReadSocketWritePipe.Start(new object[] { OutputPipeRead, oldStdOut, "stdout", oldStdIn });
            thThreadReadPipeWriteSocket.Start(new object[] { oldStdIn, InputPipeWrite, "stdin" });

            WaitForSingleObject(childProcessInfo.hProcess, INFINITE);
            //cleanup everything
            thThreadReadPipeWriteSocket.Abort();
            thReadSocketWritePipe.Abort();

            RestoreStdHandles(oldStdIn, oldStdOut, oldStdErr);
            if (newConsoleAllocated)
                FreeConsole();
            CloseHandle(childProcessInfo.hThread);
            CloseHandle(childProcessInfo.hProcess);
            if (handlePseudoConsole != IntPtr.Zero) ClosePseudoConsole(handlePseudoConsole);
            if (InputPipeWrite != IntPtr.Zero) CloseHandle(InputPipeWrite);
            if (OutputPipeRead != IntPtr.Zero) CloseHandle(OutputPipeRead);
            return true;
        }

        private void pipe_thread(object dumb)
        {
            object[] parms = (object[])dumb;
            IntPtr read = (IntPtr)parms[0];
            IntPtr write = (IntPtr)parms[1];
            String name = (String)parms[2];
            uint bufsz = 16 * 1024;
            byte[] bytes = new byte[bufsz];
            bool read_success = false, write_success = true;
            uint nsent = 0;
            uint nread = 0;

            try
            {
                do
                {
                    read_success = ReadFile(read, bytes, bufsz, out nread, IntPtr.Zero);
                    if (!read_success) System.Threading.Thread.Sleep(100);
                    if (nread != 0 && read_success)
                    {
                        if( name == "stdout")
                        {
                            CancelIoEx((IntPtr)parms[3], IntPtr.Zero);
                        }
                        write_success = WriteFile(write, bytes, nread, out nsent, IntPtr.Zero);
                        FlushFileBuffers(write);
                    }
                    else write_success = true;
                } while (write_success);
            }
            finally
            {
            }
        }

    }

}
