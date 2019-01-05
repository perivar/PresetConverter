using System;
using System.Security.Cryptography;
using System.Text;

namespace PresetConverterProject.NIKontaktNKS
{
    // AES Encryption/Decryption Example in C#
    // https://gist.github.com/magicsih/be06c2f60288b54d9f52856feb96ce8c

    // https://blogs.msdn.microsoft.com/shawnfa/2006/10/09/the-differences-between-rijndael-and-aes/
    // When you need to write managed code that encrypts or decrypts data according to the AES standard, most people just plug the RijndaelManaged class in and go on their way.  After all, Rijndael was the winner of the NIST competition to select the algorithm that would become AES.  However, there are some differences between Rijndael and the official FIPS-197 specification for AES.
    // Namely, Rijndael allows for both key and block sizes to be chosen independently from the set of { 128, 160, 192, 224,  256 } bits.  (And the key size does not in fact have to match the block size).  However, FIPS-197 specifies that the block size must always be 128 bits in AES, and that the key size may be either 128, 192, or 256 bits.  Therefore AES-128, AES-192, and AES-256 are actually:
    // 
    //          Key Size (bits)	Block Size (bits)
    // AES-128	128	            128
    // AES-192	192	            128
    // AES-256	256	            128
    public class Aes
    {
        private static RijndaelManaged rijndael = new RijndaelManaged();
        private static System.Text.UnicodeEncoding unicodeEncoding = new UnicodeEncoding();

        private const int CHUNK_SIZE = 128;

        private void InitializeRijndael()
        {
            rijndael.Mode = CipherMode.CBC;
            rijndael.Padding = PaddingMode.PKCS7;
        }

        public Aes()
        {
            InitializeRijndael();

            rijndael.KeySize = CHUNK_SIZE;
            rijndael.BlockSize = CHUNK_SIZE;

            rijndael.GenerateKey();
            rijndael.GenerateIV();
        }

        public Aes(String base64key, String base64iv)
        {
            InitializeRijndael();

            rijndael.Key = Convert.FromBase64String(base64key);
            rijndael.IV = Convert.FromBase64String(base64iv);
        }

        public Aes(byte[] key, byte[] iv)
        {
            InitializeRijndael();

            rijndael.Key = key;
            rijndael.IV = iv;
        }

        public string Decrypt(byte[] cipher)
        {
            ICryptoTransform transform = rijndael.CreateDecryptor();
            byte[] decryptedValue = transform.TransformFinalBlock(cipher, 0, cipher.Length);
            return unicodeEncoding.GetString(decryptedValue);
        }

        public string DecryptFromBase64String(string base64cipher)
        {
            return Decrypt(Convert.FromBase64String(base64cipher));
        }

        public byte[] EncryptToByte(string plain)
        {
            ICryptoTransform encryptor = rijndael.CreateEncryptor();
            byte[] cipher = unicodeEncoding.GetBytes(plain);
            byte[] encryptedValue = encryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return encryptedValue;
        }

        public string EncryptToBase64String(string plain)
        {
            return Convert.ToBase64String(EncryptToByte(plain));
        }

        public string GetKey()
        {
            return Convert.ToBase64String(rijndael.Key);
        }

        public string GetIV()
        {
            return Convert.ToBase64String(rijndael.IV);
        }

        public override string ToString()
        {
            return "KEY:" + GetKey() + Environment.NewLine + "IV:" + GetIV();
        }

    }
}