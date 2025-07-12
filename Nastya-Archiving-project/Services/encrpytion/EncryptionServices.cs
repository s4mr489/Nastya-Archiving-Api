using System.Security.Cryptography;
using System.Text;

namespace Nastya_Archiving_project.Services.encrpytion
{
    public class EncryptionServices : IEncryptionServices
    {
        // Static Key (similar to VB)
        public static string strKey = "@#AhMeD61SaMiR78YoSiF83AhMeD86#@";

        // Private variables for encryption
        private static readonly byte[] bytIV = { 115, 97, 109, 105, 114, 113, 97, 115, 105, 109, 109, 111, 104, 97, 109, 100 };
        private const char chrKeyFill = 'X';
        private const string strTextErrorString = "#ERROR - {0}";
        private const int intMinSalt = 4;
        private const int intMaxSalt = 8;
        private const int intHashSize = 16;
        private const int intKeySize = 32;

        public string EncryptString256Bit(string strPlainText)
        {
            try
            {
                byte[] bytPlainText;
                byte[] bytKey;
                byte[] bytEncoded;
                using (MemoryStream objMemoryStream = new MemoryStream())
                {
                    using (RijndaelManaged objRijndaelManaged = new RijndaelManaged())
                    {
                        strPlainText = strPlainText.Replace("\0", string.Empty);
                        bytPlainText = Encoding.UTF8.GetBytes(strPlainText);
                        bytKey = ConvertKeyToBytes(strKey);
                        using (CryptoStream objCryptoStream = new CryptoStream(objMemoryStream, objRijndaelManaged.CreateEncryptor(bytKey, bytIV), CryptoStreamMode.Write))
                        {
                            objCryptoStream.Write(bytPlainText, 0, bytPlainText.Length);
                            objCryptoStream.FlushFinalBlock();
                            bytEncoded = objMemoryStream.ToArray();
                        }
                    }
                }
                return Convert.ToBase64String(bytEncoded);
            }
            catch (Exception ex)
            {
                return string.Format(strTextErrorString, ex.Message);
            }
        }

        // Decrypt string using 256-bit decryption
        public string DecryptString256Bit(string strCryptText)
        {
            string output = "";
            try
            {
                byte[] bytCryptText = Convert.FromBase64String(strCryptText);
                byte[] bytKey = ConvertKeyToBytes(strKey);
                byte[] bytTemp = new byte[bytCryptText.Length];
                using (MemoryStream objMemoryStream = new MemoryStream(bytCryptText))
                {
                    using (RijndaelManaged objRijndaelManaged = new RijndaelManaged())
                    {
                        using (CryptoStream objCryptoStream = new CryptoStream(objMemoryStream, objRijndaelManaged.CreateDecryptor(bytKey, bytIV), CryptoStreamMode.Read))
                        {
                            objCryptoStream.Read(bytTemp, 0, bytTemp.Length);
                        }
                    }
                }
                output = Encoding.UTF8.GetString(bytTemp).Replace("\0", string.Empty);
            }
            catch (Exception)
            {
                output = "Error";
            }
            return output;
        }

        // Compute MD5 hash with optional salt
        public string ComputeMD5Hash(string strPlainText, byte[] bytSalt = null)
        {
            try
            {
                byte[] bytPlainTextArray = Encoding.UTF8.GetBytes(strPlainText);
                using (HashAlgorithm hash = new MD5CryptoServiceProvider())
                {
                    if (bytSalt == null)
                    {
                        Random rand = new Random();
                        int intSaltSize = rand.Next(intMinSalt, intMaxSalt);
                        bytSalt = new byte[intSaltSize];
                        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                        {
                            rng.GetNonZeroBytes(bytSalt);
                        }
                    }
                    byte[] bytPlainTextWithSalt = new byte[bytPlainTextArray.Length + bytSalt.Length];
                    bytPlainTextArray.CopyTo(bytPlainTextWithSalt, 0);
                    bytSalt.CopyTo(bytPlainTextWithSalt, bytPlainTextArray.Length);

                    byte[] bytHash = hash.ComputeHash(bytPlainTextWithSalt);
                    byte[] bytHashWithSalt = new byte[bytHash.Length + bytSalt.Length];
                    bytHash.CopyTo(bytHashWithSalt, 0);
                    bytSalt.CopyTo(bytHashWithSalt, bytHash.Length);
                    return Convert.ToBase64String(bytHashWithSalt);
                }
            }
            catch (Exception ex)
            {
                return string.Format(strTextErrorString, ex.Message);
            }
        }

        // Verify MD5 hash
        public bool VerifyHash(string strPlainText, string strHashValue)
        {
            try
            {
                byte[] bytWithSalt = Convert.FromBase64String(strHashValue);
                if (bytWithSalt.Length < intHashSize) return false;

                byte[] bytSalt = new byte[bytWithSalt.Length - intHashSize];
                Array.Copy(bytWithSalt, intHashSize, bytSalt, 0, bytSalt.Length);
                string strExpectedHashString = ComputeMD5Hash(strPlainText, bytSalt);
                return strHashValue.Equals(strExpectedHashString);
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Concat byte arrays
        private byte[] ConcatBytes(byte[] bytA, byte[] bytB)
        {
            byte[] bytX = new byte[bytA.Length + bytB.Length];
            bytA.CopyTo(bytX, 0);
            bytB.CopyTo(bytX, bytA.Length);
            return bytX;
        }

        // Convert key string to byte array of fixed size
        private byte[] ConvertKeyToBytes(string strKey)
        {
            try
            {
                int intLength = strKey.Length;
                if (intLength < intKeySize)
                    strKey += new string(chrKeyFill, intKeySize - intLength);
                else
                    strKey = strKey.Substring(0, intKeySize);
                return Encoding.UTF8.GetBytes(strKey);
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Encrypt files using AES
        public string Encrypt(string inputFilePath, string outputFilePath)
        {
            string EncryptionKey = "2023NastyaAhmed";
            string result = "0";
            try
            {
                using (Aes encryptor = Aes.Create())
                {
                    var pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6E, 0x20, 0x4D, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);

                    using (FileStream fs = new FileStream(outputFilePath, FileMode.Create))
                    {
                        using (CryptoStream cs = new CryptoStream(fs, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            using (FileStream fsInput = new FileStream(inputFilePath, FileMode.Open))
                            {
                                int data;
                                while ((data = fsInput.ReadByte()) != -1)
                                {
                                    cs.WriteByte((byte)data);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            return result;
        }

        // Decrypt files using AES
        public string Decrypt(string inputFilePath, string outputFilePath)
        {
            string EncryptionKey = "2023NastyaAhmed";
            string result = "0";
            try
            {
                using (Aes encryptor = Aes.Create())
                {
                    var pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6E, 0x20, 0x4D, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);

                    using (FileStream fs = new FileStream(inputFilePath, FileMode.Open))
                    {
                        using (CryptoStream cs = new CryptoStream(fs, encryptor.CreateDecryptor(), CryptoStreamMode.Read))
                        {
                            using (FileStream fsOutput = new FileStream(outputFilePath, FileMode.Create))
                            {
                                int data;
                                while ((data = cs.ReadByte()) != -1)
                                {
                                    fsOutput.WriteByte((byte)data);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            return result;
        }
    }
}
