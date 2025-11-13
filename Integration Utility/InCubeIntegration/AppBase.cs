using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Windows.Forms;

namespace InCubeIntegration_App
{
    public class AppBase
    {
        public static void CopySiteFiles(string SiteName, BuildMode Target)
        {
            try
            {
                string[] pathParts = Application.StartupPath.Split(new char[] { '\\' });
                string ApplicationDirectory = "";
                List<string> requiredDirectories = new List<string>();

                //Copy From
                string ExecutionDirectory = Application.StartupPath;
                string ServiceDirectory = ExecutionDirectory.Replace("InCubeIntegration", "InCubeIntegrationWindowsService");
                string GeneralScriptsDirectory = "";
                string CustomerScriptsDirectory = "";
                string DataSourcesPath = "";
                string AppConfigPath = "";
                string ServiceConfigPath = "";
                string tnsNames = "";

                //Copy To
                string SiteDirectory = "";

                // Application Folder
                for (int i = pathParts.Length - 1; i >= 0; i--)
                {
                    if (pathParts[i] == "InCubeIntegration")
                    {
                        for (int j = 0; j <= i; j++)
                        {
                            ApplicationDirectory += pathParts[j] + "\\";
                        }
                        break;
                    }
                }

                //General Scripts Folder
                GeneralScriptsDirectory = ApplicationDirectory + "Scripts\\General";

                //Build Files List
                List<string> BuildFiles = new List<string>();
                BuildFiles.Add("InCubeIntegration.exe");
                BuildFiles.Add("InCubeLibrary.dll");
                BuildFiles.Add("InCubeIntegration_BL.dll");
                BuildFiles.Add("InCubeIntegration_UI.dll");
                BuildFiles.Add("InCubeIntegration_DAL.dll");
                BuildFiles.Add("InCubeIntegrationWindowsService.exe");
                BuildFiles.Add("ICSharpCode.SharpZipLib.dll");
                BuildFiles.Add("DocumentFormat.OpenXml.dll");
                BuildFiles.Add("WindowsBase.dll");

                //Build must be 64-bit for all sites execpt following:
                switch (SiteName.ToLower())
                {
                    case "hassani":
                    case "vodafone":
                    case "qnie":
                    case "ufc":
                        if (IntPtr.Size != 4)
                            return;
                        break;
                }

                //Customer Scrips Folder
                //DataSources Path
                //Site Build Directory

                //From
                CustomerScriptsDirectory = ApplicationDirectory + "Scripts\\" + SiteName;
                string envParam = "";
                if (Target == BuildMode.Test)
                    envParam = " - Test";
                else if (Target == BuildMode.Live2)
                    envParam = " - 2";
                DataSourcesPath = ApplicationDirectory + string.Format("Connections\\{0}\\DataSources{1}.xml", SiteName, envParam);
                AppConfigPath = ApplicationDirectory + string.Format("Connections\\{0}\\App{1}.Config", SiteName, envParam);
                ServiceConfigPath = ApplicationDirectory + string.Format("Connections\\{0}\\Service{1}.Config", SiteName, envParam);
                tnsNames = ApplicationDirectory + string.Format("Connections\\{0}\\tnsnames.ora", SiteName, envParam);
                //To
                SiteDirectory = ApplicationDirectory + string.Format("Builds\\{0} Integration {1} {2}{3}", SiteName, CoreGeneral.Common.GeneralConfigurations.AppVersion, IntPtr.Size == 4 ? "(x86)" : "(x64)", envParam);

                switch (SiteName.ToLower())
                {
                    case "ug":
                    case "abu issa":
                    case "awal":
                        BuildFiles.Add("Oracle.ManagedDataAccess.dll");
                        BuildFiles.Add("tnsnames.ora");
                        break;
                    case "esf":
                    case "jeema":
                    case "alain":
                        //Required Directories
                        requiredDirectories.Add(SiteDirectory + "\\E-Connect");
                        //Extra build Files
                        BuildFiles.Add("Microsoft.Dynamics.GP.eConnect.dll");
                        BuildFiles.Add("Microsoft.Dynamics.GP.eConnect.Serialization.dll");
                        break;
                    case "alainfoodtruck":
                        //Required Directories
                        requiredDirectories.Add(SiteDirectory + "\\E-Connect\\Dairy");
                        requiredDirectories.Add(SiteDirectory + "\\E-Connect\\Poultry");
                        //Extra build Files
                        BuildFiles.Add("Microsoft.Dynamics.GP.eConnect.dll");
                        BuildFiles.Add("Microsoft.Dynamics.GP.eConnect.Serialization.dll");
                        break;
                    case "ufc":
                        BuildFiles.Add("App.Config");
                        BuildFiles.Add("Service.Config");
                        break;
                    case "qnie":
                    case "vodafone":
                        BuildFiles.Add("App.Config");
                        BuildFiles.Add("Service.Config");
                        BuildFiles.Add("sapnco.dll");
                        BuildFiles.Add("sapnco_utils.dll");
                        break;
                    case "cezar":
                    case "pepsipal":
                    case "telelink":
                        BuildFiles.Add("khaled.pfx");
                        BuildFiles.Add("Newtonsoft.Json.dll");
                        break;
                    case "attar":
                        BuildFiles.Add("Newtonsoft.Json.dll");
                        break;
                    case "khraim":
                         BuildFiles.Add("KhraimIntegration.dll");
                        break;
                    case "masafi":
                        BuildFiles.Add("Business.dll");
                        BuildFiles.Add("DataAccess.dll");
                        BuildFiles.Add("integration.xml");
                        BuildFiles.Add("App.Config");
                        BuildFiles.Add("tnsnames.ora");
                        BuildFiles.Add("Oracle.ManagedDataAccess.dll");
                        break;
                    case "hassani":
                        BuildFiles.Add("tnsnames.ora");
                        BuildFiles.Add("Oracle.ManagedDataAccess.dll");
                        break;
                    default:
                        BuildFiles.Add("Oracle.ManagedDataAccess.dll");
                        break;
                }

                //Required Directories
                requiredDirectories.Add(SiteDirectory);
                requiredDirectories.Add(SiteDirectory + "\\Scripts");
                requiredDirectories.Add(ExecutionDirectory + "\\Scripts");

                //Create\Empty required folders

                foreach (string dir in requiredDirectories)
                {
                    DirectoryInfo siteDir = new DirectoryInfo(dir);
                    if (siteDir.Exists)
                    {
                        foreach (FileInfo file in siteDir.GetFiles())
                        {
                            file.Delete();
                        }
                    }
                    else
                    {
                        siteDir.Create();
                    }
                }

                #region(Copy Site Files)

                if (System.IO.File.Exists(DataSourcesPath))
                {
                    System.IO.File.Copy(DataSourcesPath, SiteDirectory + "\\DataSources.xml", true);
                }

                DirectoryInfo scriptsDir = new DirectoryInfo(GeneralScriptsDirectory);
                if (scriptsDir.Exists)
                {
                    foreach (FileInfo file in scriptsDir.GetFiles())
                    {
                        file.CopyTo(ExecutionDirectory + "\\Scripts\\" + file.Name, true);
                        file.CopyTo(SiteDirectory + "\\Scripts\\" + file.Name, true);
                    }
                }

                //DirectoryInfo scriptsCustDir = new DirectoryInfo(CustomerScriptsDirectory);
                //if (scriptsCustDir.Exists)
                //{
                //    foreach (FileInfo file in scriptsCustDir.GetFiles())
                //    {
                //        file.CopyTo(ExecutionDirectory + "\\Scripts\\" + file.Name, true);
                //        file.CopyTo(SiteDirectory + "\\Scripts\\" + file.Name, true);
                //    }
                //}

                foreach (string buildFile in BuildFiles)
                {
                    if (buildFile.Equals("InCube.png"))
                    {
                        if (File.Exists(ExecutionDirectory.Replace("InCubeIntegration", "InCubeIntegration_UI") + "\\Resources\\" + buildFile))
                            File.Copy(ExecutionDirectory.Replace("InCubeIntegration", "InCubeIntegration_UI") + "\\Resources\\" + buildFile, SiteDirectory + "\\" + buildFile);
                    }
                    else if (buildFile == "App.Config")
                    {
                        if (File.Exists(AppConfigPath))
                            File.Copy(AppConfigPath, SiteDirectory + "\\InCubeIntegration.exe.config");
                    }
                    else if (buildFile == "Service.Config")
                    {
                        if (File.Exists(ServiceConfigPath))
                            File.Copy(ServiceConfigPath, SiteDirectory + "\\InCubeIntegrationWindowsService.exe.config");
                    }
                    else if (buildFile == "tnsnames.ora")
                    {
                        if (File.Exists(tnsNames))
                            File.Copy(tnsNames, SiteDirectory + "\\tnsnames.ora");
                    }
                    else if (buildFile.Equals("InCubeIntegrationWindowsService.exe"))
                    {
                        if (File.Exists(ServiceDirectory + "\\" + buildFile))
                            File.Copy(ServiceDirectory + "\\" + buildFile, SiteDirectory + "\\" + buildFile);
                    }
                    else
                    {
                        if (File.Exists(ExecutionDirectory + "\\" + buildFile))
                            File.Copy(ExecutionDirectory + "\\" + buildFile, SiteDirectory + "\\" + buildFile);
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private static string GetInstalledServiceVersion()
        {
            string version = "";
            try
            {
                ServiceController service = new ServiceController("InCubeIntegrationWindowsService");
                version = service.DisplayName;
                version = version.Substring(35);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
            return version;
        }
        public static bool InstallWindowsService()
        {
            try
            {
                string currentVersion = GetInstalledServiceVersion();
                if (currentVersion == "")
                {
                    return ChangeServiceInstallation(true);
                }
                else
                {
                    string ServiceVersion = CoreGeneral.Common.FormatVersionNumber(FileVersionInfo.GetVersionInfo("InCubeIntegrationWindowsService.exe").FileVersion);
                    if (ServiceVersion != currentVersion)
                    {
                        if (ChangeServiceInstallation(false))
                        {
                            return ChangeServiceInstallation(true);
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
                return false;
            }
        }
        public static void CreateInstalltionFile(bool install)
        {
            try
            {
                if (File.Exists("WindowsService" + (install ? "" : "Un") + "Installer.bat"))
                    File.Delete("WindowsService" + (install ? "" : "Un") + "Installer.bat");

                string serviceEXE = string.Empty;
                foreach (string filename in Directory.GetFiles(Directory.GetCurrentDirectory()))
                {
                    if (filename.Contains("InCubeIntegrationWindowsService") && Path.GetExtension(filename) == ".exe")
                    {
                        serviceEXE = Path.GetFileName(filename);
                        break;
                    }
                }

                File.AppendAllText("WindowsService" + (install ? "" : "Un") + "Installer.bat", string.Format(@"@ECHO Installing Service...
@SET PATH=C:\Windows\Microsoft.NET\Framework\v4.0.30319
@InstallUtil {1} ""{0}\{2}""
@ECHO Install Done.
@pause", Application.StartupPath, (install ? "" : "-u"), serviceEXE));
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
        }
        private static bool ChangeServiceInstallation(bool install)
        {
            ProcessStartInfo procInfo;
            bool success = false;
            try
            {
                if (File.Exists("InCubeIntegrationWindowsService.InstallLog"))
                    File.Delete("InCubeIntegrationWindowsService.InstallLog");

                CreateInstalltionFile(install);

                procInfo = new ProcessStartInfo();
                procInfo.UseShellExecute = true;
                procInfo.FileName = "WindowsService" + (install ? "" : "Un") + "Installer.bat";  //The file in that DIR.
                procInfo.WorkingDirectory = @""; //The working DIR.
                procInfo.Verb = "runas";
                Process.Start(procInfo);  //Start that process.

                for (int i = 0; i < 10; i++)
                {
                    System.Threading.Thread.Sleep(1000);
                    if (File.Exists("InCubeIntegrationWindowsService.InstallLog"))
                    {
                        if (install)
                        {
                            if (File.ReadAllText("InCubeIntegrationWindowsService.InstallLog").Contains("Service InCubeIntegrationWindowsService has been successfully installed."))
                            {
                                success = true;
                            }
                        }
                        else
                        {
                            if (File.ReadAllText("InCubeIntegrationWindowsService.InstallLog").Contains("Service InCubeIntegrationWindowsService was successfully removed from the system."))
                            {
                                success = true;
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
            return success;
        }
        public static void RunWindowsService()
        {
            try
            {
                ServiceController service = new ServiceController("InCubeIntegrationWindowsService");
                try
                {
                    if (service.Status == ServiceControllerStatus.Stopped)
                        service.Start();
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.WindowsService);
            }
        }
    }
}