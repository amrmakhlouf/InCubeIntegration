using System;
using System.Collections.Generic;
using System.Text;

namespace InCubeIntegration_DAL
{
    public class InCubeSecurityGroupClass
    {
        private long SecurityGroupID;
        private string Description;
        private InCubeSecurityClass SecuritySession;
        private Exception CurrentException;

        public InCubeSecurityGroupClass(InCubeSecurityClass securitySession)
        {
            SecuritySession = securitySession;
        }
        public Exception GetCurrentException()
        {
            return CurrentException;
        }
        public long GetSecurityGroupID()
        {
            return SecurityGroupID;
        }
        public void SetSecurityGroupID(long securityGroupID)
        {
            SecurityGroupID = securityGroupID;
        }
        public InCubeErrors GetDescription(long securityGroupID, InCubeLanguage language, ref string description)
        {
            return GetDescription(securityGroupID, language, language.GetCurrentLanguage(), ref description);
        }
        public InCubeErrors GetDescription(long securityGroupID, InCubeLanguage language, InCubeLanguages languageID, ref string description)
        {
            InCubeRow securityGroupRow = new InCubeRow();
            InCubeErrors err;
            try
            {
                err = SecuritySession.SecurityGroupTable.FindFirst("SecurityGroupID=" + securityGroupID, ref securityGroupRow);
                if (err != InCubeErrors.Success) { CurrentException = SecuritySession.SecurityGroupTable.GetCurrentException(); return err; }
                err = language.GetLanguageString(SecuritySession.SecurityGroupTable, languageID, General.Common.DescriptionFeild, ref description);
                Description = description;
                return err;
            }
            catch (Exception ex)
            {
                General.Common.ErrorLogger.LogError(((System.Reflection.MemberInfo)(this.GetType())).Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace);
                CurrentException = ex;
                return InCubeErrors.Error;
            }
           
        }
        public InCubeErrors SetDescription(long securityGroupID, InCubeLanguage language, string description)
        {
            return SetDescription(securityGroupID, language, language.GetCurrentLanguage(), description);
        }
        public InCubeErrors SetDescription(long securityGroupID, InCubeLanguage language, InCubeLanguages languageID, string description)
        {
            InCubeRow securityGroupRow = new InCubeRow();
            InCubeErrors err;
            try
            {
                err = SecuritySession.SecurityGroupTable.FindFirst("SecurityGroupID=" + securityGroupID, ref securityGroupRow);
                if (err != InCubeErrors.Success) { CurrentException = SecuritySession.SecurityGroupTable.GetCurrentException(); return err; }
                err = language.SetLanguageString(SecuritySession.SecurityGroupTable, languageID,General.Common.DescriptionFeild , description);
                return err;
            }
            catch (Exception ex)
            {
                General.Common.ErrorLogger.LogError(((System.Reflection.MemberInfo)(this.GetType())).Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace);
                CurrentException = ex;
                return InCubeErrors.Error;
            }
        }
        public InCubeErrors Get(InCubeLanguage language, long securityGroupID)
        {
            return Get(language, securityGroupID, language.GetCurrentLanguage());
        }
        public InCubeErrors Get(InCubeLanguage language, long securityGroupID, InCubeLanguages languageID)
        {
            InCubeRow securityGroupRow = new InCubeRow();
            InCubeErrors err;
            object field = new object();
            try
            {
                err = SecuritySession.SecurityGroupTable.FindFirst("SecurityGroupID=" + securityGroupID, ref securityGroupRow);
                if (err != InCubeErrors.Success) { CurrentException = SecuritySession.SecurityGroupTable.GetCurrentException(); return err; }
                err = securityGroupRow.GetField("SecurityGroupID", ref field);
                if (err != InCubeErrors.Success) { CurrentException = securityGroupRow.GetCurrentException(); return err; }
                SecurityGroupID = int.Parse(field.ToString());
                return language.GetLanguageString(SecuritySession.SecurityGroupTable, languageID, General.Common.DescriptionFeild, ref Description);
            }
            catch (Exception ex)
            {
                General.Common.ErrorLogger.LogError(((System.Reflection.MemberInfo)(this.GetType())).Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace);
                CurrentException = ex;
                return InCubeErrors.Error;
            }
           
        }
        public InCubeErrors Add(InCubeLanguage language)
        {
            return Add(language, SecurityGroupID, language.GetCurrentLanguage(), Description);
        }
        public InCubeErrors Add(InCubeLanguage language, long securityGroupID, InCubeLanguages languageID, string description)
        {
            InCubeRow securityGroupRow = new InCubeRow();
            object field = new object();
            InCubeErrors err;
            try
            {
                err = SecuritySession.SecurityGroupTable.FindFirst("SecurityGroupID=" + securityGroupID, ref securityGroupRow);
                switch (err)
                {
                    case InCubeErrors.Success:
                        break;
                    case InCubeErrors.DBNoMoreRows:
                        err = SecuritySession.SecurityGroupTable.New(ref securityGroupRow);
                        if (err != InCubeErrors.Success) { CurrentException = SecuritySession.SecurityGroupTable.GetCurrentException(); return err; }
                        field = securityGroupID;
                        err = securityGroupRow.SetField("SecurityGroupID", field);
                        if (err != InCubeErrors.Success) { CurrentException = securityGroupRow.GetCurrentException(); return err; }
                        err = SecuritySession.SecurityGroupTable.Insert(securityGroupRow);
                        if (err != InCubeErrors.Success) { CurrentException = SecuritySession.SecurityGroupTable.GetCurrentException(); return err; }
                        break;
                    default:
                        CurrentException = SecuritySession.SecurityGroupTable.GetCurrentException();
                        return err;
                }
                return language.SetLanguageString(SecuritySession.SecurityGroupTable, languageID,General.Common.DescriptionFeild, description);
            }
            catch (Exception ex)
            {
                General.Common.ErrorLogger.LogError(((System.Reflection.MemberInfo)(this.GetType())).Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace);
                CurrentException = ex;
                return InCubeErrors.Error;
            }
            
        }
        public InCubeErrors Update(InCubeLanguage language, long securityGroupID, InCubeLanguages languageID, string description)
        {
            InCubeRow securityGroupRow = new InCubeRow();
            object field = new object();
            InCubeErrors err;
            try
            {
                err = SecuritySession.SecurityGroupTable.FindFirst("SecurityGroupID=" + SecurityGroupID, ref securityGroupRow);
                if (err != InCubeErrors.Success) { CurrentException = SecuritySession.SecurityGroupTable.GetCurrentException(); return err; }
                field = securityGroupID;
                err = securityGroupRow.SetField("SecurityGroupID", field);
                if (err != InCubeErrors.Success) { CurrentException = securityGroupRow.GetCurrentException(); return err; }
                err = SecuritySession.SecurityGroupTable.Update(securityGroupRow);
                if (err != InCubeErrors.Success) { CurrentException = SecuritySession.SecurityGroupTable.GetCurrentException(); return err; }
                SecurityGroupID = securityGroupID;
                err = language.SetLanguageString(SecuritySession.SecurityGroupTable, languageID, General.Common.DescriptionFeild, description);
                Description = description;
                return err;
            }
            catch (Exception ex)
            {
                General.Common.ErrorLogger.LogError(((System.Reflection.MemberInfo)(this.GetType())).Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace);
                CurrentException = ex;
                return InCubeErrors.Error;
            }
           
        }
        public InCubeErrors Delete()
        {
            return Delete(SecurityGroupID);
        }
        public InCubeErrors Delete(long securityGroupID)
        {
            InCubeRow securityGroupRow = new InCubeRow();
            InCubeErrors err;
            try
            {
                err = SecuritySession.SecurityGroupTable.FindFirst("SecurityGroupID=" + SecurityGroupID, ref securityGroupRow);
                if (err != InCubeErrors.Success) { CurrentException = SecuritySession.SecurityGroupTable.GetCurrentException(); return err; }
                err = SecuritySession.SecurityGroupTable.Delete(securityGroupRow);
                if (err != InCubeErrors.Success) CurrentException = SecuritySession.SecurityGroupTable.GetCurrentException();
                return err;
            }
            catch (Exception ex)
            {
                General.Common.ErrorLogger.LogError(((System.Reflection.MemberInfo)(this.GetType())).Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace);
                CurrentException = ex;
                return InCubeErrors.Error;
            }
            
        }
    }
}
