using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InCubeLibrary
{
    public class UserPrivileges
    {
        public Dictionary<Menus, string> MenusAccess;
        public Dictionary<IntegrationField,FieldItem> UpdateFieldsAccess;
        public Dictionary<IntegrationField, FieldItem> SendFieldsAccess;
        public Dictionary<IntegrationField, FieldItem> SpecialFunctionsAccess;

        public string Organizations;
        public int UserOrganizationID;
        public string UserOrgCode;
        
        public UserPrivileges()
        {
            UserOrganizationID = 0;
            UserOrgCode = "";
            Organizations = "";
            MenusAccess = new Dictionary<Menus, string>();
            UpdateFieldsAccess = new Dictionary<IntegrationField, FieldItem>();
            SendFieldsAccess = new Dictionary<IntegrationField, FieldItem>();
            SpecialFunctionsAccess = new Dictionary<IntegrationField, FieldItem>();
        }
    }
}
