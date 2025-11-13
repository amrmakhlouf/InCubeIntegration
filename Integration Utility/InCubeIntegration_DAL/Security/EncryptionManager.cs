using InCubeLibrary;
using System;
using System.IO;
using System.Security.Cryptography;

namespace InCubeIntegration_DAL.Security
{
    class EncryptionManager
    {
        private System.Text.UTF8Encoding encoder = new System.Text.UTF8Encoding();
        private string _encryptionKey;

        public EncryptionManager(string encryptionKey)
        {
            _encryptionKey = encryptionKey;
        }

        public string EncryptData(string data)
        {
            RijndaelManaged RijndaelCipher = new RijndaelManaged();
            PasswordDeriveBytes secretKey;
            ICryptoTransform encryptor;
            MemoryStream memoryStream = null;
            CryptoStream cryptoStream = null;
            string encryptedData;
            try
            {
                byte[] plainText = System.Text.Encoding.Unicode.GetBytes(data);
                byte[] salt = System.Text.Encoding.ASCII.GetBytes(_encryptionKey.Length.ToString());
                secretKey = new PasswordDeriveBytes(_encryptionKey, salt);
                //Creates a symmetric encryptor object. 
                encryptor = RijndaelCipher.CreateEncryptor(secretKey.GetBytes(32), secretKey.GetBytes(16));
                memoryStream = new System.IO.MemoryStream();
                //Defines a stream that links data streams to cryptographic transformations
                cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
                cryptoStream.Write(plainText, 0, plainText.Length);
                //Writes the final state and clears the buffer
                cryptoStream.FlushFinalBlock();
                byte[] CipherBytes = memoryStream.ToArray();
                encryptedData = Convert.ToBase64String(CipherBytes);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name, System.Reflection.MethodBase.GetCurrentMethod().Name, ex.Message, LoggingType.Error, LoggingFiles.InCubeLog);
                encryptedData = data;
            }
            finally
            {
                if (memoryStream != null) memoryStream.Close();
                if (cryptoStream != null) cryptoStream.Close();
            }
            return encryptedData;
        }
        public string DecryptData(string data)
        {
            RijndaelManaged RijndaelCipher = new RijndaelManaged();
            string decryptedData;
            PasswordDeriveBytes SecretKey;
            ICryptoTransform Decryptor;
            MemoryStream memoryStream = null;
            CryptoStream cryptoStream = null;
            int decryptedCount;
            try
            {
                byte[] EncryptedData = Convert.FromBase64String(data);
                byte[] Salt = System.Text.Encoding.ASCII.GetBytes(_encryptionKey.Length.ToString());
                //Making of the key for decryption
                SecretKey = new PasswordDeriveBytes(_encryptionKey, Salt);
                //Creates a symmetric Rijndael decryptor object.
                Decryptor = RijndaelCipher.CreateDecryptor(SecretKey.GetBytes(32), SecretKey.GetBytes(16));
                memoryStream = new System.IO.MemoryStream(EncryptedData);
                //Defines the cryptographics stream for decryption.THe stream contains decrpted data
                cryptoStream = new CryptoStream(memoryStream, Decryptor, CryptoStreamMode.Read);
                byte[] PlainText = new byte[EncryptedData.Length];
                decryptedCount = cryptoStream.Read(PlainText, 0, PlainText.Length);
                //Converting to string
                decryptedData = System.Text.Encoding.Unicode.GetString(PlainText, 0, decryptedCount);
            }
            catch
            {
                decryptedData = data;
            }
            finally
            {
                if (memoryStream != null) memoryStream.Close();
                if (cryptoStream != null) cryptoStream.Close();
            }
            return decryptedData;
        }
    }
}