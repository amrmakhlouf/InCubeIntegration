using InCubeIntegration_DAL;
using InCubeLibrary;
using SAP.Middleware.Connector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Xml;
using System.Drawing;
using System.IO;

namespace InCubeIntegration_BL
{
    public class IntegrationDelMonteUS : IntegrationBase
    {
        InCubeDatabase db_Image;
        InCubeQuery incubeQuery = null;
        SqlCommand sqlCMD = null;
        public IntegrationDelMonteUS(long CurrentUserID, ExecutionManager ExecManager)
            : base(ExecManager)
        {
            try
            {
                db_Image = new InCubeDatabase();
                if (db_Image.Open("ImagesDB", "ImagesDB") != InCubeErrors.Success)
                    Initialized = false;
            }
            catch (Exception ex)
            {
                Initialized = false;
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
        }
        private Result ExportSignaure()
        {
            Result res = Result.UnKnown;
            string ImagesPath = "", ImagesURL = "";

            //Signature
            try
            {
                incubeQuery = new InCubeQuery(string.Format(@"SELECT T.TransactionID, T.CustomerID, T.OutletID
FROM [Transaction]  T
INNER JOIN TransSignature S ON S.TransactionID = T.TransactionID AND S.CustomerID = T.CustomerID AND S.OutletID = T.OutletID
WHERE T.DeliveryChargesID IS NULL
AND T.TransactionDate >= '{0}' AND T.TransactionDate < DATEADD(DD,1,'{1}')"
, Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.ToString("yyyy-MM-dd")), db_vms);
                if (incubeQuery.Execute() != InCubeErrors.Success)
                    return Result.Failure;
                DataTable dtSignature = incubeQuery.GetDataTable();

                if (dtSignature.Rows.Count > 0)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(CoreGeneral.Common.StartupPath + "\\DataSources.xml");
                    ImagesPath = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'ImagesDestination']/Data").InnerText;
                    ImagesURL = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'ImagesURL']/Data").InnerText;

                    for (int i = 0; i < dtSignature.Rows.Count; i++)
                    {
                        try
                        {
                            string TransID = dtSignature.Rows[i]["TransactionID"].ToString();
                            string CustID = dtSignature.Rows[i]["CustomerID"].ToString();
                            string OutID = dtSignature.Rows[i]["OutletID"].ToString();

                            incubeQuery = new InCubeQuery(db_vms, string.Format(@"SELECT Signature FROM TransSignature WHERE TransactionID = '{0}' AND CustomerID = {1} AND OutletID = {2}", TransID, CustID, OutID));
                            object imageBytes = null;
                            if (incubeQuery.ExecuteScalar(ref imageBytes) == InCubeErrors.Success && imageBytes != null)
                            {
                                res = SaveImage(ImagesPath, ImagesURL, TransID, CustID, OutID, "Signature", 1, imageBytes);
                                if (res == Result.Success || res == Result.Duplicate)
                                {
                                    incubeQuery = new InCubeQuery(db_vms, string.Format(@"UPDATE [Transaction] SET DeliveryChargesID = 1 WHERE TransactionID = '{0}' AND CustomerID = {1} AND OutletID = {2}", TransID, CustID, OutID));
                                    incubeQuery.ExecuteNonQuery();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        }
                    }
                    return Result.Success;
                }
                else
                    return Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }
        private Result ExportProofs()
        {
            Result res = Result.NotInitialized;
            try
            {
                incubeQuery = new InCubeQuery(string.Format(@"SELECT D.DocumentID, D.CustomerID, D.OutletID, P.FileName, P.ImageSequence, P.RouteHistoryID, P.Sequence, P.ProofMethodID, L.Description Method
FROM ProofValueImage P
INNER JOIN DocumentProofHeader D ON D.RouteHistoryID = P.RouteHistoryID AND D.Sequence = P.Sequence
INNER JOIN ProofMethodLanguage L ON L.ProofMethodID = P.ProofMethodID AND L.LanguageID = 1
WHERE ISNULL(P.Synchronized,0) = 0
AND P.CaptureDate >= '{0}' AND P.CaptureDate < DATEADD(DD,1,'{1}')"
, Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.ToString("yyyy-MM-dd")), db_vms);
                if (incubeQuery.Execute() != InCubeErrors.Success)
                    return Result.Failure;
                DataTable dtProofs = incubeQuery.GetDataTable();

                if (dtProofs.Rows.Count > 0)
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(CoreGeneral.Common.StartupPath + "\\DataSources.xml");
                    string ImagesPath = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'ImagesDestination']/Data").InnerText;
                    string ImagesURL = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'ImagesURL']/Data").InnerText;
                    string ProofImagesPath = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'ProofImagesPath']/Data").InnerText;
                    for (int i = 0; i < dtProofs.Rows.Count; i++)
                    {
                        try
                        {
                            string TransID = dtProofs.Rows[i]["DocumentID"].ToString();
                            string CustID = dtProofs.Rows[i]["CustomerID"].ToString();
                            string OutID = dtProofs.Rows[i]["OutletID"].ToString();
                            string RHID = dtProofs.Rows[i]["RouteHistoryID"].ToString();

                            string Seq = dtProofs.Rows[i]["Sequence"].ToString();
                            string MethodID = dtProofs.Rows[i]["ProofMethodID"].ToString();
                            int ImgSeq = int.Parse(dtProofs.Rows[i]["ImageSequence"].ToString());

                            string label = dtProofs.Rows[i]["Method"].ToString();
                            string FileName = dtProofs.Rows[i]["FileName"].ToString();

                            string FilePath = ProofImagesPath + "\\" + RHID + "\\" + FileName;
                            byte[] img = File.ReadAllBytes(FilePath);
                            object imageBytes = img;

                            res = SaveImage(ImagesPath, ImagesURL, TransID, CustID, OutID, label, ImgSeq, imageBytes);
                            if (res == Result.Success || res == Result.Duplicate)
                            {
                                incubeQuery = new InCubeQuery(db_vms, string.Format(@"UPDATE ProofValueImage SET Synchronized = 1 WHERE RouteHistoryID = {0} AND Sequence = {1} AND ProofMethodID = {2} AND ImageSequence = {3}", RHID, Seq, MethodID, ImgSeq));
                                incubeQuery.ExecuteNonQuery();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                        }
                    }
                    return Result.Success;
                }
                else
                    return Result.NoRowsFound;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                return Result.Failure;
            }
        }
        public override Result ExportImages()
        {
            ExportSignaure();
            ExportProofs();
            return Result.Success;
        }
        public Result ExportImages_Old()
        {
            Result res = Result.UnKnown;
            string ImagesPath = "", ImagesURL = "";
            try
            {
                incubeQuery = new InCubeQuery(string.Format(@"SELECT T.TransactionID, T.CustomerID, T.OutletID
FROM [Transaction]  T
WHERE T.DeliveryChargesID IS NULL AND T.TransactionDate >= '{0}' AND T.TransactionDate < DATEADD(DD,1,'{1}')"
, Filters.FromDate.ToString("yyyy-MM-dd"), Filters.ToDate.ToString("yyyy-MM-dd")), db_vms);
                if (incubeQuery.Execute() != InCubeErrors.Success)
                    return Result.Failure;
                DataTable dtTrans = incubeQuery.GetDataTable();
                if (dtTrans.Rows.Count == 0)
                {
                    return Result.NoRowsFound;
                }
                else
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(CoreGeneral.Common.StartupPath + "\\DataSources.xml");
                    ImagesPath = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'ImagesDestination']/Data").InnerText;
                    ImagesURL = xmlDoc.SelectSingleNode("Connections/Connection[Name = 'ImagesURL']/Data").InnerText;
                }

                for (int i = 0; i < dtTrans.Rows.Count; i++)
                {
                    try
                    {
                        string TransID = dtTrans.Rows[i]["TransactionID"].ToString();
                        string CustID = dtTrans.Rows[i]["CustomerID"].ToString();
                        string OutID = dtTrans.Rows[i]["OutletID"].ToString();
                        int imagesCount = 0;
                        //Signature
                        incubeQuery = new InCubeQuery(db_vms, string.Format(@"SELECT Signature FROM TransSignature WHERE TransactionID = '{0}' AND CustomerID = {1} AND OutletID = {2}", TransID, CustID, OutID));
                        object imageBytes = null;
                        if (incubeQuery.ExecuteScalar(ref imageBytes) == InCubeErrors.Success && imageBytes != null)
                        {
                            res = SaveImage(ImagesPath, ImagesURL, TransID, CustID, OutID, "Signature", 1, imageBytes);
                            if (res == Result.Success || res == Result.Duplicate)
                                imagesCount++;
                        }
                        //Delivery
                        incubeQuery = new InCubeQuery(db_vms, string.Format(@"SELECT Sequence,Image FROM TransactionImage WHERE TransactionID = '{0}' AND CustomerID = {1} AND OutletID = {2}", TransID, CustID, OutID));
                        if (incubeQuery.Execute() == InCubeErrors.Success)
                        {
                            DataTable dtImages = incubeQuery.GetDataTable();
                            foreach (DataRow dr in dtImages.Rows)
                            {
                                res = SaveImage(ImagesPath, ImagesURL, TransID, CustID, OutID, "POD", Convert.ToInt16(dr["Sequence"]), dr["Image"]);
                                if (res == Result.Success || res == Result.Duplicate)
                                    imagesCount++;
                            }
                        }
                        if (imagesCount == 3)
                        {
                            incubeQuery = new InCubeQuery(db_vms, string.Format(@"UPDATE [Transaction] SET DeliveryChargesID = 1 WHERE TransactionID = '{0}' AND CustomerID = {1} AND OutletID = {2}", TransID, CustID, OutID));
                            incubeQuery.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result SaveImage(string ImagesPath, string ImagesURL, string TransactionID, string CustomerID, string OutletID, string Label, int ImageNo, object imageBytes)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db_Image, string.Format(@"SELECT COUNT(*) FROM TransactionImages WHERE TransactionID = '{0}' AND CustomerID = {1} AND OutletID = {2} AND Label = '{3}' AND ImageNo = {4}", TransactionID, CustomerID, OutletID, Label, ImageNo));
                object field = null;
                if (incubeQuery.ExecuteScalar(ref field) != InCubeErrors.Success)
                    return Result.Failure;
                int count = Convert.ToInt32(field);
                if (count > 0)
                    return Result.Duplicate;

                incubeQuery = new InCubeQuery(db_Image, "SELECT ISNULL(MAX(ImageID),0)+1 FROM TransactionImages");
                field = null;
                if (incubeQuery.ExecuteScalar(ref field) != InCubeErrors.Success)
                    return Result.Failure;
                int ImageID = Convert.ToInt32(field);

                string ImageName = ImagesPath + "\\" + ImageID.ToString() + ".png";
                string ImageURL = ImagesURL + "/" + ImageID + ".png";
                //Byte[] data = new Byte[0];

				//Save image file
                byte[] data = (byte[])(imageBytes);
                MemoryStream mem = new MemoryStream(data);
                Image img = Image.FromStream(mem);
                img.Save(ImageName);
                
				//Save image to DB
                string Query = string.Format(@"INSERT INTO TransactionImages (ImageID,TransactionID,CustomerID,OutletID,Label,ImageNo,Extension,ImageData,ImagePath,ImageURL) 
					VALUES ({0},'{1}',{2},{3},'{4}',{5},'{6}',@Image,'{7}','{8}')", ImageID, TransactionID, CustomerID, OutletID, Label, ImageNo, "png", ImageName, ImageURL);
                sqlCMD = new SqlCommand(Query, db_Image.GetConnection());
                sqlCMD.Parameters.Add("@Image", SqlDbType.Image);
                sqlCMD.Parameters[0].Value = imageBytes;
                sqlCMD.ExecuteNonQuery();

                res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        private Result SaveImage_Old(string ImagesPath, string ImagesURL, string TransactionID, string CustomerID, string OutletID, string Label, int ImageNo, object imageBytes)
        {
            Result res = Result.UnKnown;
            try
            {
                incubeQuery = new InCubeQuery(db_Image, string.Format(@"SELECT COUNT(*) FROM TransactionImages WHERE TransactionID = '{0}' AND CustomerID = {1} AND OutletID = {2} AND Label = '{3}' AND ImageNo = {4}", TransactionID, CustomerID, OutletID, Label, ImageNo));
                object field = null;
                if (incubeQuery.ExecuteScalar(ref field) != InCubeErrors.Success)
                    return Result.Failure;
                int count = Convert.ToInt32(field);
                if (count > 0)
                    return Result.Duplicate;

                incubeQuery = new InCubeQuery(db_Image, "SELECT ISNULL(MAX(ImageID),0)+1 FROM TransactionImages");
                field = null;
                if (incubeQuery.ExecuteScalar(ref field) != InCubeErrors.Success)
                    return Result.Failure;
                int ImageID = Convert.ToInt32(field);

                string ImageName = ImagesPath + "\\" + ImageID.ToString() + ".png";
                string ImageURL = ImagesURL + "/" + ImageID + ".png";
                //Byte[] data = new Byte[0];

                //Save image file
                byte[] data = (byte[])(imageBytes);
                MemoryStream mem = new MemoryStream(data);
                Image img = Image.FromStream(mem);
                img.Save(ImageName);

                //Save image to DB
                string Query = string.Format(@"INSERT INTO TransactionImages (ImageID,TransactionID,CustomerID,OutletID,Label,ImageNo,Extension,ImageData,ImagePath,ImageURL) 
					VALUES ({0},'{1}',{2},{3},'{4}',{5},'{6}',@Image,'{7}','{8}')", ImageID, TransactionID, CustomerID, OutletID, Label, ImageNo, "png", ImageName, ImageURL);
                sqlCMD = new SqlCommand(Query, db_Image.GetConnection());
                sqlCMD.Parameters.Add("@Image", SqlDbType.Image);
                sqlCMD.Parameters[0].Value = imageBytes;
                sqlCMD.ExecuteNonQuery();

                res = Result.Success;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
            }
            return res;
        }
        public override void Close()
        {
            if (db_Image != null && db_Image.GetConnection().State == ConnectionState.Open)
            {
                db_Image.Close();
                db_Image.Dispose();
            }
			if (sqlCMD != null)
            {
                sqlCMD.Dispose();
            }
        }
    }
}
