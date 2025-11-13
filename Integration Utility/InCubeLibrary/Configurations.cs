using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace InCubeLibrary
{
    public class Configurations
    {
        public string WS_UserName;
        public string WS_Password;
        public string WS_URL;
        public string AppVersion;
        public string ConditionalSymbol;
        public string SiteSymbol;
        public bool LoginRequired;
        public bool OrganizationOriented;
        public bool WindowsServiceEnabled;
        public string IntegrationFormTitle;
        public Color IntegrationFormBackColor;
        public Menus DefaultMenu;
        public Queues_Mode WS_Queues_Mode;
        public string WS_Machine_Name;
        public int DefaultPaymentTermDays;

        public string CurrencyCode;
        public string AppServerHost;
        public string Name;
        public string User;
        public string Password;
        public string Client;
        public string Language;
        public string SystemNumber;
        public string SystemID;
        public string InboundStagingDB;
        public string OutboundStagingDB;
        public string TAXDTLID;
        public string EXCISEDTLID;
        public string SSL_File;
        public string SSL_Key;
        

        public Configurations()
        {
            WS_UserName = string.Empty;
            WS_Password = string.Empty;
            WS_URL = string.Empty;
            ConditionalSymbol = string.Empty;
            SiteSymbol = string.Empty;
            LoginRequired = false;
            OrganizationOriented = false;
            WindowsServiceEnabled = false;
            IntegrationFormTitle = "";
            IntegrationFormBackColor = Color.Transparent;
            AppServerHost = string.Empty;
            Name = string.Empty;
            User = string.Empty;
            Password = string.Empty;
            Client = string.Empty;
            Language = string.Empty;
            SystemNumber = string.Empty;
            SystemID = string.Empty;
            DefaultMenu = Menus.Manual_Integration;
            InboundStagingDB = string.Empty;
            OutboundStagingDB = string.Empty;
            TAXDTLID = string.Empty;
            EXCISEDTLID = string.Empty;
            SSL_File = string.Empty;
            SSL_Key = string.Empty;
            WS_Queues_Mode = Queues_Mode.TaskID;
            WS_Machine_Name = string.Empty;
        }
    }
}
