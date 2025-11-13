using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using InCubeLibrary;
using System.Web;
using System.Net;

namespace InCubeIntegration_BL
{
    public class APIManager : System.IDisposable
    {
        public APIManager ()
        {

        }
        public string GetJsonFromDataTable(string Namespace, DataTable dtData, int RowIndex)
        {
            string jsonData = "";
            try
            {
                int StartIndex = RowIndex;
                int EndIndex = RowIndex;
                if (RowIndex == -1)
                {
                    StartIndex = 0;
                    EndIndex = dtData.Rows.Count - 1;
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                if (Namespace == "")
                    sb.AppendLine(" [");
                else
                    sb.AppendLine(string.Format("\"{0}\": [", Namespace));
                
                for (int i = StartIndex; i <= EndIndex; i++)
                {
                    sb.AppendLine("    {");
                    for (int j = 0; j < dtData.Columns.Count; j++)
                    {
                        sb.Append(string.Format("        \"{0}\": \"{1}\"", dtData.Columns[j].ColumnName, dtData.Rows[i][j]));
                        if (j < dtData.Columns.Count - 1)
                            sb.AppendLine(",");
                    }
                    sb.AppendLine();
                    sb.Append("    }");
                    if (i < EndIndex)
                        sb.AppendLine(",");
                }
                sb.AppendLine();
                sb.AppendLine(" ]");
                sb.Append("}");
                jsonData = sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return jsonData;
        }
        public string GetJsonFromDataRow(DataRow dr)
        {
            string jsonData = "";
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");

                for (int i = 0; i < dr.Table.Columns.Count; i++)
                {
                    sb.Append(string.Format("   \"{0}\": \"{1}\"", dr.Table.Columns[i].ColumnName, dr[i]));
                    if (i < dr.Table.Columns.Count - 1)
                        sb.AppendLine(",");
                }

                sb.AppendLine();
                sb.Append("}");
                jsonData = sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return jsonData;
        }
        public string GetJsonFromDataTable(DataRow drHeader, string DetailsName, DataTable dtDetails)
        {
            string jsonData = "";
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("{");
                for (int i = 0; i < drHeader.Table.Columns.Count; i++)
                {
                    string column = drHeader.Table.Columns[i].ColumnName;
                    sb.AppendLine(string.Format("    \"{0}\": \"{1}\",", column, drHeader[column]));
                }
                sb.AppendLine(string.Format("    \"{0}\": [", DetailsName));

                for (int j = 0; j < dtDetails.Rows.Count; j++)
                {
                    sb.AppendLine("        {");
                    for (int k = 0; k < dtDetails.Columns.Count; k++)
                    {
                        sb.Append(string.Format("            \"{0}\": \"{1}\"", dtDetails.Columns[k].ColumnName, dtDetails.Rows[j][k]));
                        if (k < dtDetails.Columns.Count - 1)
                            sb.AppendLine(",");
                    }
                    sb.AppendLine();
                    sb.Append("        }");
                    if (j < dtDetails.Rows.Count - 1)
                        sb.AppendLine(",");
                }
                sb.AppendLine();
                sb.AppendLine("    ]");
                sb.Append("}");

                jsonData = sb.ToString();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return jsonData;
        }
        public Result CallPostFunction(string URL, string requestjson, ref string responseJson)
        {
            Result res = Result.UnKnown;
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(URL);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(requestjson);
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    responseJson = streamReader.ReadToEnd();
                    res = Result.Success;
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public void Dispose()
        {
            
        }
    }
}
