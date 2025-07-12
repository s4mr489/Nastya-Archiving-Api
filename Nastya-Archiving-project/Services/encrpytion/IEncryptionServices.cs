namespace Nastya_Archiving_project.Services.encrpytion
{
    public interface IEncryptionServices
    {
       
        public string EncryptString256Bit(string strPlainText);
        public string DecryptString256Bit(string strCryptText);
        public string ComputeMD5Hash(string strPlainText, byte[] bytSalt = null);
        public bool VerifyHash(string strPlainText, string strHashValue);
        public string Encrypt(string inputFilePath, string outputFilePath);
        public string Decrypt(string inputFilePath, string outputFilePath);
    }
}
