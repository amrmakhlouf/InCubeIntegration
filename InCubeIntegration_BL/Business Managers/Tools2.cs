using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using InCubeLibrary;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace InCubeIntegration_BL
{
    public static class Tools2
    {
        public static T GetJsonData<T>(string propertyName, string json)
        {
            using (var stringReader = new StringReader(json))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                var serializer = new JsonSerializer();
                if (propertyName == "" || propertyName == "noroot")
                    return serializer.Deserialize<T>(jsonReader);
                while (jsonReader.Read())
                {
                    if (jsonReader.TokenType == JsonToken.PropertyName
                        && (string)jsonReader.Value == propertyName)
                    {
                        jsonReader.Read();
                        return serializer.Deserialize<T>(jsonReader);
                    }
                }
                return default(T);
            }
        }

        private static List<string> GetPermutations(List<int> Sizes)
        {
            int count = 1;
            for (int i = 0; i < Sizes.Count; i++)
            {
                count *= Sizes[i];
            }

            List<string> perm = new List<string>();
            for (int j = 0; j < count; j++)
            {
                perm.Add("");
            }
            int multiply = count;

            for (int x = 0; x < Sizes.Count; x++)
            {
                int num = Sizes[x];
                multiply /= num;
                int value = 0;
                for (int k = 1; k <= count; k++)
                {
                    perm[k - 1] = perm[k - 1] += value.ToString() + (x == Sizes.Count - 1 ? "" : ":");
                    if (k % multiply == 0)
                        value++;
                    if (value == num)
                        value = 0;
                }
            }
            return perm;
        }
        public static DataTable GetAPIRequestTable<T>(string url, Dictionary<string, string> Params, string userName, string password, int timeout, string rootName)
        {
            DataTable dtData = null;

            //Get json response
            string json = RunHTTPrequest(url, Params, userName, password, timeout);
            //Deserialize to object array
            T[] obj = GetObjectArray<T>(json, rootName);
            //Convert array to datatable
            dtData = ToDataTable<T>(obj.ToList<T>());

            return dtData;
        }

        public static string RunHTTPrequest(string url, Dictionary<string, string> Params, string username, string password, int timeoutseconds)
        {
            string responseJSON = "";
            try
            {
                foreach (KeyValuePair<string, string> param in Params)
                {
                    url = url.Replace(param.Key, param.Value);
                }

                HttpMessageHandler handler = new HttpClientHandler() { };
                var httpClient = new HttpClient(handler)
                {
                    BaseAddress = new Uri(url),
                    Timeout = new TimeSpan(0, 0, timeoutseconds)
                };

                //httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
                httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

                var byteArray = Encoding.ASCII.GetBytes(username + ":" + password);
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                
                HttpResponseMessage response = httpClient.GetAsync(url).Result;
                responseJSON = string.Empty;

                using (StreamReader stream = new StreamReader(response.Content.ReadAsStreamAsync().Result, System.Text.Encoding.GetEncoding(864)))
                {
                    responseJSON = stream.ReadToEnd();
                    Boolean result = false;
                    if (result = responseJSON.Contains("ERROR"))
                    {
                        int firstStringPosition = responseJSON.IndexOf("\"" + "Status_Description" + "\":" + "\"") + ("\"" + "Status_Description" + "\":" + "\"").Length;
                        int secondStringPosition = responseJSON.IndexOf(".\"" + "}}");
                        responseJSON = responseJSON.Substring(firstStringPosition,
                            secondStringPosition - firstStringPosition);
                        throw new System.ArithmeticException(responseJSON);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return responseJSON;


        }

        private static T[] GetObjectArray<T>(string JsonContent, string JsonRoot)
        {

            T[] obj = null;
            obj = GetJsonData<T[]>(JsonRoot, JsonContent);
            return obj;

        }



   


        public static DataTable ToDataTable<T>(this IList<T> data)
        {
            PropertyDescriptorCollection mainProp = TypeDescriptor.GetProperties(typeof(T));
            PropertyDescriptorCollection arrProp = null;
            DataTable table = new DataTable();

            //Create table columns
            for (int i = 0; i < mainProp.Count; i++)
            {
                PropertyDescriptor prop = mainProp[i];
                if (prop.PropertyType.IsArray && data.Count > 0 && mainProp[i].GetValue(data[0]) != null)
                {
                    arrProp = TypeDescriptor.GetProperties(((object[])(mainProp[i].GetValue(data[0])))[0].GetType());
                    for (int j = 0; j < arrProp.Count; j++)
                    {
                        table.Columns.Add(arrProp[j].Name, arrProp[j].PropertyType);
                    }
                }
                else
                    table.Columns.Add(prop.Name, prop.PropertyType);
            }

            try
            {
                foreach (T item in data)
                {
                    //Values array length is same as table columns
                    object[] values = new object[table.Columns.Count];
                    //Dictionary to contain all lists of array values, each array with starting index in tables columns
                    Dictionary<int, List<object[]>> All_Lists = new Dictionary<int, List<object[]>>();
                    //Count of values obtained from arrays
                    int subvalues = 0;
                    //Loop through main properties
                    for (int i = 0; i < mainProp.Count; i++)
                    {
                        try
                        {
                            PropertyDescriptor prop = mainProp[i];
                            if (prop.PropertyType.IsArray && mainProp[i].GetValue(item) != null)
                            {
                                //if array, fill a list of objects for each array line
                                List<object[]> list = new List<object[]>();
                                arrProp = TypeDescriptor.GetProperties(((object[])(mainProp[i].GetValue(data[0])))[0].GetType());

                                foreach (object obj in ((object[])(mainProp[i].GetValue(item))))
                                {
                                    object[] sub = new object[(arrProp != null ? arrProp.Count : 0)];

                                    for (int j = 0; j < arrProp.Count; j++)
                                    {
                                        try
                                        {
                                            sub[j] = arrProp[j].GetValue(obj);
                                        }
                                        catch (Exception)
                                        {
                                            sub[j] = DBNull.Value;
                                        }
                                    }
                                    list.Add(sub);
                                }
                                //Add the array object to list of arrays
                                All_Lists.Add(i + subvalues, list);
                                subvalues += arrProp.Count - 1;
                            }
                            else //if it not array, the values is filled directly in corresponding index with skipping columns for array values
                                values[i + subvalues] = mainProp[i].GetValue(item);
                        }
                        catch (Exception ex)
                        {
                            values[i] = DBNull.Value;
                        }
                    }

                    //This part is for copying array values to data table row, might duplicate lines if arrays has more than one object
                    if (All_Lists.Count > 0)
                    {
                        //Understand size which is the multiplication of objects count per array
                        List<int> Sizes = new List<int>();
                        foreach (List<object[]> o in All_Lists.Values)
                        {
                            Sizes.Add(o.Count);
                        }
                        List<string> permutations = GetPermutations(Sizes);

                        //Fill rows as per all possible permutations
                        for (int k = 0; k < permutations.Count; k++)
                        {
                            string[] indexes = permutations[k].Split(new char[] { ':' });
                            List<int> keys = new List<int>(All_Lists.Keys);
                            for (int m = 0; m < indexes.Length; m++)
                            {
                                int start = keys[m];
                                List<object[]> objectList = All_Lists[keys[m]];
                                int objectNum = int.Parse(indexes[m]);
                                objectList[objectNum].CopyTo(values, start);
                            }
                            //Add row to table for each permutation
                            table.Rows.Add(values);
                        }
                    }
                    else //No arrays, just add one row
                        table.Rows.Add(values);
                }
            }
            catch (Exception ex)
            {


            }
            return table;
        }


        public static string GetJsonFromDataTable(DataRow drHeader, string DetailsName, DataTable dtDetails)
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

        public static string GetJsonFromDataRow(DataRow dr)
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

        public static Result CallPostFunction(string URL, string requestjson, string username, string password, ref string responseJson)
        {
            Result res = Result.UnKnown;
            try
            
            
            
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(URL);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Accept = "application/json";
                httpWebRequest.Method = "POST";
                var byteArray = Encoding.ASCII.GetBytes(username + ":" + password);
                httpWebRequest.Headers["Authorization"] = "Basic "+  (Convert.ToBase64String(byteArray));
                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(requestjson);
                }
                ServicePointManager.SecurityProtocol =   SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                ServicePointManager.Expect100Continue = true;
                httpWebRequest.Timeout = -1;
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    responseJson = streamReader.ReadToEnd();
                    res = Result.Success;
                    int firstStringPosition = responseJson.IndexOf("\"" + "Status_Description" + "\":" + "\"") + ("\"" + "Status_Description" + "\":" + "\"").Length;
                    int secondStringPosition = responseJson.IndexOf(".\"" + "}}}");
                    responseJson = responseJson.Substring(firstStringPosition,
                        secondStringPosition - firstStringPosition);
                }
            }
            catch (Exception ex)
            {
                res = Result.Failure;
                responseJson = ex.Message.ToString();
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }


            return res;
        }



    }




}
