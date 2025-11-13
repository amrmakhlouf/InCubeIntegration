using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace InCubeLibrary
{
    public class CoreGeneral
    {
        private static CoreGeneral _general;
        public static CoreGeneral Common
        {
            get
            {
                if (_general == null)
                {
                    _general = new CoreGeneral();
                }
                return _general;
            }
        }

        public CoreGeneral()
        {
            userPrivileges = new UserPrivileges();
            CurrentSession = new Session();
            GeneralConfigurations = new Configurations();
            OrganizationConfigurations = new Dictionary<int, Configurations>();
            StartupPath = Application.StartupPath;
        }

        public Dictionary<string, Queue> Queues;
        public UserPrivileges userPrivileges;
        public Session CurrentSession;
        public Configurations GeneralConfigurations;
        public Dictionary<int, Configurations> OrganizationConfigurations;
        public bool IsTesting = false;
        public string StartupPath = "";
        public string FormatVersionNumber(string versionNumber)
        {
            string formattedVersionNumber = "";
            try
            {
                string[] versionArray = versionNumber.Split(new char[] {'.'});
                for (int i=0;i<versionArray.Length;i++)
                {
                    formattedVersionNumber += versionArray[i].PadLeft((i == 0 || i == 3) ? 4 : 2, '0') + ".";
                }
                formattedVersionNumber = formattedVersionNumber.Substring(0, formattedVersionNumber.Length - 1);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return formattedVersionNumber;
        }
        public long GetLongVersionNumber(string versionNumber)
        {
            long longVerNo = 0;
            try
            {
                string[] versionArray = versionNumber.Split(new char[] { '.' });
                versionNumber = "";
                for (int i = 0; i < versionArray.Length; i++)
                {
                    versionNumber += versionArray[i].PadLeft(i == 0 || i == 3 ? 4 : 2, '0');
                }
                longVerNo = long.Parse(versionNumber);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return longVerNo;
        }
    }
}
