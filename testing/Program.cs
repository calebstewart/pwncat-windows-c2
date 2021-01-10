namespace testing
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = System.Management.Automation.Runspaces.RunspaceConfiguration.Create();
            Microsoft.PowerShell.ConsoleShell.Start(config, "Banner Text", "Help Text", new string[] { });
        }
    }
}
