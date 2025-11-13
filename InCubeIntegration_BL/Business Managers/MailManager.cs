using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.IO;

namespace InCubeIntegration_BL
{
    public class MailManager : IDisposable
    {
        InCubeDatabase db;
        InCubeQuery incubeQuery;

        public MailManager()
        {
            db = new InCubeDatabase();
            db.Open("InCube", "MailManager");
        }
        public Result GetActiveSenderProfiles(ref DataTable dtSenderProfiles)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, @"SELECT SP.SenderProfileID ID,SP.ProfileName,SP.Host,SP.Port,SP.MailAddress,SP.DisplayName
,CASE SP.EnableSSL WHEN 1 THEN 'Yes' ELSE 'No' END EnableSSL
FROM Int_MailSenderProfile SP
WHERE SP.IsDeleted = 0
ORDER BY SP.SenderProfileID");
                InCubeErrors err = incubeQuery.Execute();
                if (err == InCubeErrors.Success)
                {
                    dtSenderProfiles = incubeQuery.GetDataTable();
                    if (dtSenderProfiles != null && dtSenderProfiles.Rows.Count > 0)
                    {
                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
                    }
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public int GetMaxSenderProfileID()
        {
            int ID = 0;
            try
            {
                incubeQuery = new InCubeQuery(db, "SELECT ISNULL(MAX(SenderProfileID),0)+1 FROM Int_MailSenderProfile");
                object field = null;
                incubeQuery.ExecuteScalar(ref field);
                ID = Convert.ToInt16(field);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return ID;
        }
        public Result GetSenderProfileDetails(int ID, ref string Name, ref string Host, ref string MailAddress, ref string DisplayName, ref string Password, ref int Port, ref bool EnableSSL)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, "SELECT * FROM Int_MailSenderProfile WHERE SenderProfileID = " + ID);
                InCubeErrors err = incubeQuery.Execute();
                if (err == InCubeErrors.Success)
                {
                    DataTable dtDetails = incubeQuery.GetDataTable();
                    if (dtDetails != null && dtDetails.Rows.Count == 1)
                    {
                        Name = dtDetails.Rows[0]["ProfileName"].ToString();
                        Host = dtDetails.Rows[0]["Host"].ToString();
                        Port = Convert.ToInt16(dtDetails.Rows[0]["Port"]);
                        MailAddress = dtDetails.Rows[0]["MailAddress"].ToString();
                        DisplayName = dtDetails.Rows[0]["DisplayName"].ToString();
                        Password = dtDetails.Rows[0]["Password"].ToString();
                        InCubeSecurityClass cls = new InCubeSecurityClass();
                        Password = cls.DecryptData(Password);
                        EnableSSL = Convert.ToBoolean(dtDetails.Rows[0]["EnableSSL"]);

                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
                    }
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result SaveSenderProfile(int ID, string Name, string Host, int Port, string MailAddress, string DisplayName, string Password, bool EnableSSL)
        {
            Result res = Result.UnKnown;
            try
            {
                InCubeSecurityClass cls = new InCubeSecurityClass();
                Password = cls.EncryptData(Password);

                incubeQuery = new InCubeQuery(db, "SELECT COUNT(*) FROM Int_MailSenderProfile WHERE SenderProfileID = " + ID);
                object fieldID = null;
                incubeQuery.ExecuteScalar(ref fieldID);
                if (fieldID.ToString() == "1")
                {
                    incubeQuery = new InCubeQuery(db, string.Format("UPDATE Int_MailSenderProfile SET ProfileName = '{0}', Host = '{1}', Port = {2}, MailAddress = '{3}', DisplayName = '{4}', Password = '{5}', EnableSSL = {6} WHERE SenderProfileID = {7}"
                        , Name.Replace("'", "''"), Host, Port, MailAddress, DisplayName, Password, EnableSSL ? 1 : 0, ID));
                }
                else
                {
                    incubeQuery = new InCubeQuery(db, string.Format("INSERT INTO Int_MailSenderProfile (SenderProfileID,ProfileName,Host,Port,MailAddress,DisplayName,Password,EnableSSL,IsDeleted) VALUES ({0},'{1}','{2}',{3},'{4}','{5}','{6}',{7},0)"
                        , ID, Name.Replace("'", "''"), Host, Port, MailAddress, DisplayName, Password, EnableSSL ? 1 : 0));
                }
                if (incubeQuery.ExecuteNonQuery() == InCubeErrors.Success)
                    res = Result.Success;
                else
                    res = Result.Failure;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result SendTestMail(string Name, string Host, int Port, string MailAddress, string DisplayName, string Password, bool EnableSSL, ref string ErrorMessage)
        {
            Result res = Result.UnKnown;
            try
            {
                string Subject = "Soinc Integration Test Mail";
                string Body = "This mail was sent to test defining mail sender profile (" + Name + ")";
                List<string> ToList = new List<string>();
                ToList.Add(MailAddress);
                List<string> CCList = new List<string>();
                res = SendMail(Host, Port, MailAddress, DisplayName, Password, EnableSSL, Subject, Body, new Dictionary<string, string>(), ToList, CCList, ref ErrorMessage);
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                ErrorMessage = ex.Message;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result SendMail(string Host, int Port, string MailAddress, string DisplayName, string Password, bool EnableSSL, string Subject, string Body, Dictionary<string,string> Attachments, List<string> ToList, List<string> CCList, ref string ErrorMessage)
        {
            Result res = Result.UnKnown;
            try
            {
                MailMessage mail = new MailMessage();
                foreach (string Address in ToList)
                    mail.To.Add(Address);
                foreach (string Address in CCList)
                    mail.CC.Add(Address);

                SmtpClient client = new SmtpClient(Host, Port);
                client.Credentials = new NetworkCredential(MailAddress, Password);
                client.EnableSsl = EnableSSL;
                mail.IsBodyHtml = true;
                mail.From = new MailAddress(MailAddress, DisplayName);
                mail.Body = Body.Replace("\r\n", "<br>");

                //Subject
                int startIndex = Subject.IndexOf('[');
                int endIndex = Subject.LastIndexOf(']');
                if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                {
                    string replacePart = Subject.Substring(startIndex, endIndex - startIndex + 1);
                    string param = Subject.Substring(startIndex + 1, endIndex - startIndex - 1);
                    Subject = Subject.Replace(replacePart, DateTime.Now.ToString(param));
                }
                mail.Subject = Subject;

                //Attachments
                foreach (KeyValuePair<string,string> attach in Attachments)
                {
                    Attachment a = new Attachment(attach.Key);
                    a.Name = attach.Value;
                    mail.Attachments.Add(a);
                }
                
                client.Send(mail);

                res = Result.Success;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                ErrorMessage = ex.Message;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private void CreateAttchmentsDirectories(ref string attachmentsdirectory, ref string tempdirectory)
        {
            try
            {
                attachmentsdirectory = CoreGeneral.Common.StartupPath + "\\Attachments";
                if (!Directory.Exists(attachmentsdirectory))
                {
                    Directory.CreateDirectory(attachmentsdirectory);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        public Result SendMails(int MailTemplateID, int TriggerID)
        {
            Result res = Result.UnKnown;
            try
            {
                string Host = "", MailAddress = "", DisplayName = "", Password = "", Subject = "", Body = "", Name = "", Header = "", Footer = "", ErrorMessage = "";
                int Port = 0, SenderProfileID = 0;
                string attachmentsdirectory = "", tempdirectory = "";
                bool EnableSSL = false;
                Dictionary<string, string> Attachments = new Dictionary<string, string>();
                List<string> ToList = new List<string>();
                List<string> CCList = new List<string>();
                DataTable dtMails = new DataTable();
                StringBuilder sb = new StringBuilder();
                DataTable dtBodyDetails = new DataTable();
                DataTable dtAttachments = new DataTable();

                incubeQuery = new InCubeQuery(db, string.Format("SELECT MailNo FROM Int_PreparedMails WHERE TriggerID = {0}", TriggerID));
                if (incubeQuery.Execute() != InCubeErrors.Success)
                    return Result.Failure;
                dtMails = incubeQuery.GetDataTable();
                
                if (dtMails != null && dtMails.Rows.Count > 0)
                {
                    res = GetMailTemplateDetails(MailTemplateID, ref Name, ref SenderProfileID, ref Subject, ref Header, ref Footer, ref ToList, ref CCList);
                    if (res != Result.Success)
                        return Result.Failure;

                    res = GetSenderProfileDetails(SenderProfileID, ref Name, ref Host, ref MailAddress, ref DisplayName, ref Password, ref Port, ref EnableSSL);
                    if (res != Result.Success)
                        return Result.Failure;

                    CreateAttchmentsDirectories(ref attachmentsdirectory, ref tempdirectory);
                    
                    foreach (DataRow dr in dtMails.Rows)
                    {
                        Result resSend = Result.UnKnown;
                        int MailNo = int.Parse(dr["MailNo"].ToString());
                        try
                        {
                            dtBodyDetails = new DataTable();
                            incubeQuery = new InCubeQuery(db, string.Format("SELECT LineText FROM Int_MailBody WHERE TriggerID = {0} AND MailNo = {1} ORDER BY [LineNo] ASC", TriggerID, MailNo));
                            if (incubeQuery.Execute() != InCubeErrors.Success)
                            {
                                resSend = Result.Invalid;
                                continue;
                            }
                            dtBodyDetails = incubeQuery.GetDataTable();

                            dtAttachments = new DataTable();
                            incubeQuery = new InCubeQuery(db, string.Format("SELECT AttachmentID,AttachmentType,QueryString,FileName FROM Int_MailAttachments WHERE TriggerID = {0} AND MailNo = {1}", TriggerID, MailNo));
                            if (incubeQuery.Execute() != InCubeErrors.Success)
                            {
                                resSend = Result.Invalid;
                                continue;
                            }
                            dtAttachments = incubeQuery.GetDataTable();

                            sb = new StringBuilder();
                            sb.AppendLine(Header);
                            sb.AppendLine();
                            foreach (DataRow drLine in dtBodyDetails.Rows)
                            {
                                sb.AppendLine(drLine["LineText"].ToString());
                            }
                            sb.AppendLine();
                            sb.Append(Footer);
                            Body = sb.ToString();

                            Attachments = new Dictionary<string, string>();
                            foreach (DataRow drAttach in dtAttachments.Rows)
                            {
                                int AttachmentID = Convert.ToInt32(drAttach["AttachmentID"]);
                                int AttachType = Convert.ToInt32(drAttach["AttachmentType"]);
                                string QueryString = drAttach["QueryString"].ToString();
                                string FileName = drAttach["FileName"].ToString();
                                int rowsFound = 0;
                                string AttachmentPath = "", AttachmentName = "";
                                if (AttachType == AttachmentType.ExcelFromQuery.GetHashCode())
                                {
                                    incubeQuery = new InCubeQuery(db, QueryString);
                                    if (incubeQuery.Execute() != InCubeErrors.Success)
                                    {
                                        resSend = Result.Invalid;
                                        break;
                                    }
                                    DataTable dtExcelData = incubeQuery.GetDataTable();
                                    if (dtExcelData != null && dtExcelData.Rows.Count > 0)
                                    {
                                        rowsFound = dtExcelData.Rows.Count;
                                    }

                                    if (rowsFound > 0)
                                    {
                                        DataSet ds = new DataSet("ds");
                                        ds.Tables.Add(dtExcelData);
                                        AttachmentPath = attachmentsdirectory + "\\" + TriggerID.ToString() + "_" + MailNo.ToString() + "_" + AttachmentID.ToString() + ".xlsx";
                                        int startIndex = FileName.IndexOf('[');
                                        int endIndex = FileName.LastIndexOf(']');
                                        if (startIndex != -1 && endIndex != -1 && endIndex > startIndex)
                                        {
                                            string replacePart = FileName.Substring(startIndex, endIndex - startIndex + 1);
                                            string param = FileName.Substring(startIndex + 1, endIndex - startIndex - 1);
                                            AttachmentName = FileName.Replace(replacePart, DateTime.Now.ToString(param)) + ".xlsx";
                                        }
                                        else
                                        {
                                            AttachmentName = FileName + ".xlsx";
                                        }
                                        ExcelManager.CreateExcelDocument(ds, AttachmentPath);
                                        Attachments.Add(AttachmentPath, AttachmentName);
                                    }
                                }
                                incubeQuery = new InCubeQuery(db, string.Format(@"UPDATE Int_MailAttachments SET RowsFound = {0}, AttachmentPath = '{1}', AttachmentName = '{5}' WHERE TriggerID = {2} AND MailNo = {3} AND AttachmentID = {4}", rowsFound, AttachmentPath, TriggerID, MailNo, AttachmentID, AttachmentName));
                                incubeQuery.ExecuteNonQuery();
                            }

                            if (resSend == Result.UnKnown)
                                resSend = SendMail(Host, Port, MailAddress, DisplayName, Password, EnableSSL, Subject, Body, Attachments, ToList, CCList, ref ErrorMessage);
                        }
                        catch (Exception ex)
                        {
                            resSend = Result.Failure;
                            ErrorMessage = ex.Message;
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        }
                        incubeQuery = new InCubeQuery(db, string.Format("UPDATE Int_PreparedMails SET ResultID = {0}, SendingTime = GETDATE(), ErrorMessage = '{1}' WHERE TriggerID = {2} AND MailNo = {3}", resSend.GetHashCode(), ErrorMessage, TriggerID, MailNo));
                        incubeQuery.ExecuteNonQuery();
                    }
                }
                else
                {
                    res = Result.NoRowsFound;
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result DeleteSenderProfile(int ID)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, "UPDATE Int_MailSenderProfile SET IsDeleted = 1 WHERE SenderProfileID = " + ID);
                if (incubeQuery.ExecuteNonQuery() == InCubeErrors.Success)
                    res = Result.Success;
                else
                    res = Result.Failure;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetActiveMailTemplates(ref DataTable dtMailTemplates)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, @"SELECT MT.MailTemplateID,MT.TemplateName,P.ProfileName,MT.Subject,R.RecipientAddress
FROM Int_MailTemplate MT
INNER JOIN Int_MailSenderProfile P ON P.SenderProfileID = MT.SenderProfileID
INNER JOIN Int_MailTemplateRecipients R ON R.MailTemplateID = MT.MailTemplateID
WHERE MT.IsDeleted = 0
ORDER BY MT.MailTemplateID");
                InCubeErrors err = incubeQuery.Execute();
                if (err == InCubeErrors.Success)
                {
                    dtMailTemplates = incubeQuery.GetDataTable();
                    if (dtMailTemplates != null && dtMailTemplates.Rows.Count > 0)
                    {
                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
                    }
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetDistinctActiveMailTemplates(ref DataTable dtMailTemplates)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, @"SELECT MailTemplateID, TemplateName FROM Int_MailTemplate WHERE IsDeleted = 0");
                InCubeErrors err = incubeQuery.Execute();
                if (err == InCubeErrors.Success)
                {
                    dtMailTemplates = incubeQuery.GetDataTable();
                    if (dtMailTemplates != null && dtMailTemplates.Rows.Count > 0)
                    {
                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
                    }
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public int GetMaxMailTemplateID()
        {
            int ID = 0;
            try
            {
                incubeQuery = new InCubeQuery(db, "SELECT ISNULL(MAX(MailTemplateID),0)+1 FROM Int_MailTemplate");
                object field = null;
                incubeQuery.ExecuteScalar(ref field);
                ID = Convert.ToInt16(field);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return ID;
        }
        public Result DeleteMailTemplate(int ID)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, "UPDATE Int_MailTemplate SET IsDeleted = 1 WHERE MailTemplateID = " + ID);
                if (incubeQuery.ExecuteNonQuery() == InCubeErrors.Success)
                    res = Result.Success;
                else
                    res = Result.Failure;
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public Result GetMailTemplateDetails(int ID, ref string Name, ref int SenderProfileID, ref string Subject, ref string Header, ref string Footer, ref List<string> To, ref List<string> CC)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, @"SELECT MT.TemplateName,MT.MailTemplateID,MT.SenderProfileID,MT.Subject,MT.Header,MT.Footer
,R.RecipientAddress,R.RecipientType
FROM Int_MailTemplate MT
INNER JOIN Int_MailTemplateRecipients R ON R.MailTemplateID = MT.MailTemplateID
WHERE MT.MailTemplateID = " + ID);
                InCubeErrors err = incubeQuery.Execute();
                if (err == InCubeErrors.Success)
                {
                    DataTable dtDetails = incubeQuery.GetDataTable();
                    if (dtDetails != null && dtDetails.Rows.Count >= 1)
                    {
                        Name = dtDetails.Rows[0]["TemplateName"].ToString();
                        SenderProfileID = Convert.ToInt16(dtDetails.Rows[0]["SenderProfileID"]);
                        Subject = dtDetails.Rows[0]["Subject"].ToString();
                        Header = dtDetails.Rows[0]["Header"].ToString();
                        Footer = dtDetails.Rows[0]["Footer"].ToString();
                        foreach (DataRow dr in dtDetails.Rows)
                        {
                            if ((MailRecipientType)int.Parse(dr["RecipientType"].ToString()) == MailRecipientType.To)
                                To.Add(dr["RecipientAddress"].ToString());
                            if ((MailRecipientType)int.Parse(dr["RecipientType"].ToString()) == MailRecipientType.CC)
                                CC.Add(dr["RecipientAddress"].ToString());
                        }

                        res = Result.Success;
                    }
                    else
                    {
                        res = Result.NoRowsFound;
                    }
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
        public Result SaveMailTemplate(int ID, string Name, int SenderProfileID, string Subject, string Header, string Footer, List<string> To, List<string> CC)
        {
            try
            {
                incubeQuery = new InCubeQuery(db, "SELECT COUNT(*) FROM Int_MailTemplate WHERE MailTemplateID = " + ID);
                object fieldID = null;
                if (incubeQuery.ExecuteScalar(ref fieldID) != InCubeErrors.Success)
                    return Result.Failure;

                if (fieldID.ToString() == "1")
                {
                    incubeQuery = new InCubeQuery(db, string.Format("UPDATE Int_MailTemplate SET TemplateName = '{0}', SenderProfileID = {1}, Subject = '{2}', Header = '{3}', Footer = '{4}' WHERE MailTemplateID = {5}"
                        , Name.Replace("'", "''"), SenderProfileID, Subject, Header, Footer, ID));
                    if (incubeQuery.ExecuteNonQuery() != InCubeErrors.Success)
                        return Result.Failure;
                    incubeQuery = new InCubeQuery(db, "DELETE FROM Int_MailTemplateRecipients WHERE MailTemplateID = " + ID);
                    if (incubeQuery.ExecuteNonQuery() != InCubeErrors.Success)
                        return Result.Failure;
                }
                else
                {
                    incubeQuery = new InCubeQuery(db, string.Format("INSERT INTO Int_MailTemplate (MailTemplateID,TemplateName,SenderProfileID,Subject,Header,Footer,IsDeleted) VALUES ({0},'{1}',{2},'{3}','{4}','{5}',0)"
                        , ID, Name.Replace("'", "''"), SenderProfileID, Subject, Header, Footer));
                    if (incubeQuery.ExecuteNonQuery() != InCubeErrors.Success)
                        return Result.Failure;
                }
                foreach (string mail in To)
                {
                    incubeQuery = new InCubeQuery(db, string.Format("INSERT INTO Int_MailTemplateRecipients (MailTemplateID,RecipientAddress,RecipientType) VALUES ({0},'{1}',{2})"
                    , ID, mail, MailRecipientType.To.GetHashCode()));
                    if (incubeQuery.ExecuteNonQuery() != InCubeErrors.Success)
                        return Result.Failure;
                }
                foreach (string mail in CC)
                {
                    incubeQuery = new InCubeQuery(db, string.Format("INSERT INTO Int_MailTemplateRecipients (MailTemplateID,RecipientAddress,RecipientType) VALUES ({0},'{1}',{2})"
                    , ID, mail, MailRecipientType.CC.GetHashCode()));
                    if (incubeQuery.ExecuteNonQuery() != InCubeErrors.Success)
                        return Result.Failure;
                }
                return Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }
        public void Dispose()
        {
            try
            {
                if (incubeQuery != null)
                    incubeQuery.Close();

                if (db != null)
                    db.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}