using System;
using System.Collections.Generic;
using System.Reflection;

namespace stagetwo
{
    class Reflection
    {
        public static List<Type> loaded_plugins = new List<Type>();

        public void compile(System.IO.StreamReader stdin)
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

            var r = new Microsoft.CSharp.CSharpCodeProvider().CompileAssemblyFromSource(cp, StageTwo.ReadUntilLine("/* ENDBLOCK */"));
            if (r.Errors.HasErrors)
            {
                return;
            }

            var obj = r.CompiledAssembly.CreateInstance("command");
            obj.GetType().GetMethod("main").Invoke(obj, new object[] { });
        }

        public static object load(string obj)
        {
            byte[] assembly_data = Convert.FromBase64String(obj);
            Assembly assembly = Assembly.Load(assembly_data);

            Type plugin = assembly.GetType("Plugin");
            if ( plugin == null)
            {
                throw new Protocol.ProtocolError(-1, "no Plugin class found");
            }

            var entry = plugin.GetMethod("entry", BindingFlags.Public | BindingFlags.Static);
            if (entry != null)
            {
                entry.Invoke(null, new object[] { typeof(Reflection).Assembly });
            }

            loaded_plugins.Add(plugin);

            return (loaded_plugins.Count - 1);
        }

        public static object call(int id, string method, object[] args)
        {
            if( id < 0 || id >= loaded_plugins.Count ){
                throw new Protocol.ProtocolError(-1, "invalid assembly id");
            }

            return loaded_plugins[id].GetMethod(
                method, 
                BindingFlags.Public | BindingFlags.Static
            ).Invoke(null, args);
        }

    }
}
