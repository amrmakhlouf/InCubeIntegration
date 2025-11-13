using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InCubeLibrary
{
    public class Session
    {
        public Session()
        {

        }
        public int UserID = 0;
        public int EmployeeID = 0;
        public string EmployeeName = "";
        public string UserName = "";
        public string Password = "";
        public DateTime LoginTime = DateTime.Now;
        public int SessionID = 0;
        public LoginType loginType;
        public bool LoggedOut = false;
    }
}
