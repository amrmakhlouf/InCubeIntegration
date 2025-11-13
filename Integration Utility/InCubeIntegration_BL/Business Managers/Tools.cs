using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Data;
using System.ComponentModel;
using InCubeLibrary;

namespace InCubeIntegration_BL
{
   public static class Tools
    {
        public static T GetJsonData<T>(string propertyName, string json)
        {
            using (var stringReader = new StringReader(json))
            using (var jsonReader = new JsonTextReader(stringReader))
            {
                var serializer = new JsonSerializer();
                if (propertyName == "")
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

        public static DataTable GetRequestTable<T>(string URL, string body, string JsonAtt, string CertificatePath, string CertificateKey, WebHeaderCollection webHeader = null)
        {
            T[] x = GetRequest<T>(URL, body, JsonAtt, CertificatePath, CertificateKey,webHeader);
            return ToDataTable<T>(x.ToList<T>());
        }
        public static DataTable GetRequestTable<T>(string URL, string body, string JsonAtt, string user, string passowrd, string Method, WebHeaderCollection webHeader = null)
        {
            T[] x = GetRequest<T>(URL, body, JsonAtt,   user,   passowrd,   Method,webHeader);
            return ToDataTable<T>(x.ToList<T>());
        }
        public static T[] GetRequest<T>(string URL, string body, string JsonAtt, string CertificatePath, string CertificateKey, WebHeaderCollection webHeader = null)
        {
            return    GetRequest<T>(  URL,   body,   JsonAtt,   CertificatePath,   CertificateKey,"","","POST",webHeader);
        }
        public static T[] GetRequest<T>(string URL, string body, string JsonAtt, string user, string passowrd, string Method, WebHeaderCollection webHeader = null)
        {
            return GetRequest<T>(URL, body, JsonAtt, "", "", user, passowrd, Method,webHeader);

        }
      
        public static T[] GetRequest<T>(string URL, string body, string JsonAtt, string CertificatePath, string CertificateKey, string user, string passowrd, string Method,WebHeaderCollection webHeader=null)
        {
            string resp = "";
            try
            {
                X509Certificate2 x = CertificatePath.Trim()=="" ? null: new X509Certificate2(CertificatePath, CertificateKey);
              
                using (var client2 = new MyWebClient(x,Method,user,passowrd,webHeader))
                {
                   if (body.Trim()!="") resp = Encoding.UTF8.GetString(client2.UploadData(URL, Encoding.UTF8.GetBytes(body)));//GetData(ss);//
                    else resp = Encoding.UTF8.GetString(client2.DownloadData(URL));//GetData(ss);//
                    T[] obj = null;
                    if(JsonAtt=="")
                    { obj =new T[] { GetJsonData<T>(JsonAtt, resp)}; }
                    else
                    obj = GetJsonData<T[]>(JsonAtt, resp);
                    if (obj == null) throw new Exception("Data Retrev error " + resp);
                    return obj;
                }
            }
            catch (Exception ex)
            {
               Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message + "\r\n\r\n" + ex.StackTrace, LoggingType.Error, LoggingFiles.InCubeLog);
               throw new Exception("Call WS error:\r\n" + resp+"\r\n *******Body*******\r\n"+body);
            }
            return null;
        }


        public static DataTable ToDataTable<T>(this IList<T> data)
        {
            PropertyDescriptorCollection props =
                TypeDescriptor.GetProperties(typeof(T));
            PropertyDescriptorCollection props2 = null;
            DataTable table = new DataTable();
             
            for (int i = 0; i < props.Count; i++)
            {
                PropertyDescriptor prop = props[i];
                if (prop.PropertyType.IsArray && data.Count>0 && props[i].GetValue(data[0])!=null)
                {
                    props2 =TypeDescriptor.GetProperties (((object[])(props[i].GetValue(data[0])))[0].GetType());
                    for (int j = 0; j < props2.Count; j++)
                    {
                        table.Columns.Add(props2[j].Name, props2[j].PropertyType);
                    }
                }
                else
                table.Columns.Add(prop.Name, prop.PropertyType);
            }
            object[] values = new object[props.Count + (props2 != null ? props2.Count-1 : 0)];
            object[] sub = new object[ (props2 != null ? props2.Count : 0)];
            List<object[]> list = null;
            int start = -1;
            try
            {


                foreach (T item in data)
                {
                    start = -1;
                    for (int i = 0; i < values.Length; i++)
                    {
                        try
                        {
                            PropertyDescriptor prop = props[i];
                            if (prop.PropertyType.IsArray && props[i].GetValue(item) != null)
                            {
                                start = i;
                                list = new List<object[]>();
                                props2 = TypeDescriptor.GetProperties(((object[])(props[i].GetValue(data[0])))[0].GetType());
                                foreach (object obj in ((object[])(props[i].GetValue(item))))
                                {
                                    sub = new object[(props2 != null ? props2.Count : 0)];

                                    for (int j = 0; j < props2.Count; j++)
                                    {
                                        try
                                        {
                                            sub[j] = props2[j].GetValue(obj);

                                        }
                                        catch (Exception)
                                        {

                                            sub[j] = DBNull.Value;
                                        }
                                    }
                                    list.Add(sub);
                                }
                            }
                            else
                                values[i] = props[i].GetValue(item);
                        }
                        catch (Exception ex)
                        {

                            values[i] = DBNull.Value;
                        }

                    }
                    if (list != null && list.Count > 0)
                    {
                        // List<object> li = new List<object>(values);
                        foreach (object[] obj in list)
                        {
                            obj.CopyTo(values, start);
                            table.Rows.Add(values);
                        }
                    }
                    else
                        table.Rows.Add(values);
                }
            }
            catch (Exception ex)
            {

              
            }
            return table;
        }
    }

    public class MyWebClient : WebClient
    {
        X509Certificate2 certificate;
        string _Method; string _User; string _Passowrd;
        WebHeaderCollection _Headers = null;
        public MyWebClient(X509Certificate2 certificate)
            : base()
        {
            this.certificate = certificate;
            _Method = "POST";
        }

        public MyWebClient()
         : base()
        {
            this.certificate = null;
            _Method = "POST";
        }
        public MyWebClient(X509Certificate2 certificate,string Method, string User, string Passowrd, WebHeaderCollection webHeader)
       : base()
        {
            this.certificate = certificate;
           this._Method = Method;
            _User = User;
            _Passowrd = Passowrd;
            _Headers = webHeader;
        }
        public MyWebClient(string Method)
     : base()
        {
            this.certificate = null;
            this._Method = Method;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(address);
            if (_User.Trim() != "")
            {
                //request.Credentials = new NetworkCredential(_User, _Passowrd);
                string authInfo = _User + ":" + _Passowrd;
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                request.Headers["Authorization"] = "Basic " + authInfo;
                
            }
            if(_Headers!=null)
            {
                for (int i = 0; i < _Headers.Count; i++)
                    request.Headers[_Headers.Keys[i]]= _Headers.GetValues(i)[0].ToString();
            }
            request.Timeout = 1200000;
                request.Method = _Method.ToUpper();
            request.ContentType = "application/json; charset=utf-8";
            if (certificate != null)
            {
                request.ClientCertificates.Add(certificate);
                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate (Object obj, X509Certificate X509certificate, X509Chain chain, System.Net.Security.SslPolicyErrors errors)
                {
                    return true;
                };
            }
            request.Credentials = this.Credentials;

            return request;
        }
    }

}
