namespace stagetwo
{
    class Reflection
    {
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

    }
}
