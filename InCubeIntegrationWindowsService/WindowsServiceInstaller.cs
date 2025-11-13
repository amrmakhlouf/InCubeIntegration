using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace InCubeIntegrationWindowsService
{
    [RunInstaller(true)]
    public class WindowsServiceInstaller : Installer
    {
        /// <summary>
        /// Public Constructor for WindowsServiceInstaller.
        /// - Put all of your Initialization code here.
        /// </summary>
        public WindowsServiceInstaller()
        {
            ServiceProcessInstaller serviceProcessInstaller =
                               new ServiceProcessInstaller();
            ServiceInstaller serviceInstaller = new ServiceInstaller();

            //# Service Account Information
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;

            //# Service Information
            string[] dir = System.IO.Directory.GetCurrentDirectory().Split(new char[] { '\\' });
            serviceInstaller.DisplayName = "InCube Integration Service " + InCubeLibrary.CoreGeneral.Common.FormatVersionNumber(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            serviceInstaller.StartType = ServiceStartMode.Automatic;

            //# This must be identical to the WindowsService.ServiceBase name
            //# set in the constructor of WindowsService.cs
            serviceInstaller.ServiceName = "InCube Integration Service " + InCubeLibrary.CoreGeneral.Common.FormatVersionNumber(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
            serviceInstaller.Description = "InCube Integration Service";

            this.Installers.Add(serviceProcessInstaller);
            this.Installers.Add(serviceInstaller);
        }
    }
}