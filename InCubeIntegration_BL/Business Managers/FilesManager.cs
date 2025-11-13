using InCubeIntegration_DAL;
using InCubeLibrary;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Data.SqlClient;

namespace InCubeIntegration_BL
{
    public class FilesManager : IDisposable
    {
        InCubeDatabase db;
        InCubeQuery incubeQuery;
        SqlCommand command;
        public FilesManager(bool OpenDB)
        {
            if (OpenDB)
            {
                db = new InCubeDatabase();
                db.Open("InCube", "FilesManager");
            }
        }
        public Result GetActiveFilesJobs(ref DataTable dtFilesJobs)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, @"SELECT JobID, JobName, CASE JobType WHEN 1 THEN 'Delete' WHEN 2 THEN 'Move' WHEN 3 THEN 'Copy' END JobType
, SourceFolder, FileExtension
FROM Int_FilesManagementJobs
WHERE IsDeleted = 0
ORDER BY JobID");
                InCubeErrors err = incubeQuery.Execute();
                if (err == InCubeErrors.Success)
                {
                    dtFilesJobs = incubeQuery.GetDataTable();
                    if (dtFilesJobs != null && dtFilesJobs.Rows.Count > 0)
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
        public Result GetFilesJobDetails(int ID, ref string Name, ref string SourceFolder, ref string FileExtension, ref string DestFolder, ref int JobTypeID, ref long Age, ref int AgeUnitID, ref bool KeepDirectoryStructure, ref ComparisonOperator Compare)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, "SELECT * FROM Int_FilesManagementJobs WHERE JobID = " + ID);
                InCubeErrors err = incubeQuery.Execute();
                if (err == InCubeErrors.Success)
                {
                    DataTable dtDetails = incubeQuery.GetDataTable();
                    if (dtDetails != null && dtDetails.Rows.Count == 1)
                    {
                        Name = dtDetails.Rows[0]["JobName"].ToString();
                        SourceFolder = dtDetails.Rows[0]["SourceFolder"].ToString();
                        FileExtension = dtDetails.Rows[0]["FileExtension"].ToString();
                        DestFolder = dtDetails.Rows[0]["DestinationFolder"].ToString();
                        JobTypeID = Convert.ToInt16(dtDetails.Rows[0]["JobType"]);
                        Age = Convert.ToInt64(dtDetails.Rows[0]["ModifyAge"]);
                        AgeUnitID = Convert.ToInt16(dtDetails.Rows[0]["AgeTimeUnit"]);
                        KeepDirectoryStructure = Convert.ToBoolean(dtDetails.Rows[0]["KeepDirectoryStructure"]);
                        Compare = (ComparisonOperator)Convert.ToInt16(dtDetails.Rows[0]["ComparisonOperator"]);
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
        public int GetMaxFilesJobID()
        {
            int ID = 0;
            try
            {
                incubeQuery = new InCubeQuery(db, "SELECT ISNULL(MAX(JobID),0)+1 FROM Int_FilesManagementJobs");
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
        public Result DeleteFilesJob(int ID)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db, "UPDATE Int_FilesManagementJobs SET IsDeleted = 1 WHERE JobID = " + ID);
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
        public Result SaveFilesJob(int ID, string Name, string SourceFolder, string FileExtension, string DestFolder, int JobTypeID, int AgeUnitID, long Age, bool KeepDirectoryStructure, ComparisonOperator compare)
        {
            try
            {
                incubeQuery = new InCubeQuery(db, "SELECT COUNT(*) FROM Int_FilesManagementJobs WHERE JobID = " + ID);
                object fieldID = null;
                if (incubeQuery.ExecuteScalar(ref fieldID) != InCubeErrors.Success)
                    return Result.Failure;

                if (fieldID.ToString() == "1")
                {
                    incubeQuery = new InCubeQuery(db, string.Format("UPDATE Int_FilesManagementJobs SET JobName = '{0}', JobType = {1}, SourceFolder = '{2}', FileExtension = '{3}', ModifyAge = {4}, AgeTimeUnit = {5}, DestinationFolder = '{6}', KeepDirectoryStructure = {7}, ComparisonOperator = {8} WHERE JobID = {9}"
                        , Name.Replace("'", "''"), JobTypeID, SourceFolder.Replace("'", "''"), FileExtension.Replace("'", "''"), Age, AgeUnitID, DestFolder.Replace("'", "''"), KeepDirectoryStructure ? 1 : 0, compare.GetHashCode(), ID));
                    if (incubeQuery.ExecuteNonQuery() != InCubeErrors.Success)
                        return Result.Failure;
                }
                else
                {
                    incubeQuery = new InCubeQuery(db, string.Format(@"INSERT INTO Int_FilesManagementJobs (JobID,JobName,JobType,SourceFolder,FileExtension,ModifyAge,AgeTimeUnit,DestinationFolder,IsDeleted,KeepDirectoryStructure,ComparisonOperator) 
                        VALUES ({0},'{1}',{2},'{3}','{4}',{5},{6},'{7}',0,{8},{9})"
                        , ID, Name.Replace("'", "''"), JobTypeID, SourceFolder.Replace("'", "''"), FileExtension.Replace("'", "''"), Age, AgeUnitID, DestFolder.Replace("'", "''"), KeepDirectoryStructure ? 1 : 0, compare.GetHashCode()));
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
        public Result ValidateDirectory(string Path)
        {
            try
            {
                if (Path.StartsWith("[Integration Directory]"))
                    Path = Path.Replace("[Integration Directory]", CoreGeneral.Common.StartupPath);
                if (!Path.EndsWith("\\"))
                    Path += "\\";

                DirectoryInfo di = new DirectoryInfo(Path);
                if (di.Exists)
                    return Result.Success;
                else
                    return Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }
        public string GenerateMapImageURL(decimal Latitude, decimal Longitude, int ZoomLevel, int Width, int Height)
        {
            string url = "";
            try
            {
                url = string.Format(@"https://maps.googleapis.com/maps/api/staticmap?center=&zoom={2}&size={3}x{4}&maptype=roadmap&markers=color:red%7Clabel:%7C{0},{1}&key=AIzaSyDQ2hiPWT0x2Lk5v-TLi9J5RZHsXTxcPvE", Latitude, Longitude, ZoomLevel, Width, Height);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return url;
        }
        public Result SaveTransactionMapImage(string TransactionID, int CustomerID, int OutletID, decimal Latitude, decimal Longitude)
        {
            Result res = Result.UnKnown;
            try
            {
                WebClient client = new WebClient();
                string URL = GenerateMapImageURL(Latitude, Longitude, 15, 707, 253);
                Stream stream = client.OpenRead(URL);
                int byt = 0;
                System.Collections.Generic.List<byte> list = new System.Collections.Generic.List<byte>();
                while (byt != -1)
                {
                    byt = stream.ReadByte();
                    if (byt != -1)
                    {
                        list.Add((byte)byt);
                    }
                }
                byte[] image = list.ToArray();
                
                string Query = string.Format("INSERT INTO tbl_TransactionImage (TransactionID,CustomerID,OutletID,Image) VALUES ('{0}',{1},{2},@Image)", TransactionID, CustomerID, OutletID);
                command = new SqlCommand(Query, db.GetConnection());
                command.Parameters.Add("@Image", SqlDbType.Image);
                command.Parameters[0].Value = image;
                command.ExecuteNonQuery();
                stream.Flush();
                stream.Close();
                client.Dispose();

                res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        private Result DownloadImage(string URL, string FileName)
        {
            Result res = Result.UnKnown;
            try
            {
                WebClient client = new WebClient();
                Stream stream = client.OpenRead(URL);
                Bitmap bitmap = new Bitmap(stream);

                if (bitmap != null)
                {
                    bitmap.Save(FileName, ImageFormat.Bmp);
                }

                stream.Flush();
                stream.Close();
                client.Dispose();
                res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                res = Result.Failure;
            }
            return res;
        }
        public void Dispose()
        {
            try
            {
                if (incubeQuery != null)
                    incubeQuery.Close();

                if (db != null)
                    db.Dispose();

                if (command != null)
                    command.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
    }
}