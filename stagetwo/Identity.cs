using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace stagetwo
{
    class Identity
    {
        public static WindowsImpersonationContext impersonated;

        public static bool Impersonate(int token)
        {
            IntPtr hToken = new IntPtr(token);

            impersonated = WindowsIdentity.Impersonate(hToken);
            PowerShell.run("[System.Security.Principal.WindowsIdentity]::Impersonate(" + hToken.ToString() + ")", 1);

            return true;
        }

        public static bool RevertToSelf()
        {
            // Revert to self
            WindowsIdentity.Impersonate(new IntPtr(0));
            PowerShell.run("[System.Security.Principal.WindowsIdentity]::Impersonate(0)", 1);

            return true;
        }
    }
}
