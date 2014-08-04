namespace Limelight_new.Crypto
{
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Security;
    using System;
    using System.Diagnostics;
    using System.Text;
    using Windows.Security.Cryptography;
    using Windows.Security.Cryptography.Core;
    using Windows.Storage.Streams;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// Performs pairing with the streaming machine
    /// </summary>
    public sealed partial class MainPage : Page
    {
        /* 
        #region Sign and Verify
        private static string Sign(String data, String privateModulusHexString, String privateExponentHexString)
        {
            // Make the key 
            RsaKeyParameters key = MakeKey(privateModulusHexString, privateExponentHexString, true);


            ISigner sig = SignerUtilities.GetSigner("SHA256withRSA");
            sig.Init(true, key);

            // Get the bytes to be signed from the string 
            var bytes = Encoding.UTF8.GetBytes(data);

            //Calc the signatur
            sig.BlockUpdate(bytes, 0, bytes.Length);
            byte[] signature = sig.GenerateSignature();

            var signedString = Convert.ToBase64String(signature);

            return signedString;
        }

        private static bool Verify(String data, String expectedSignature, String publicModulusHexString, String publicExponentHexString)
        {
            // Make the key
            RsaKeyParameters key = MakeKey(publicModulusHexString, publicExponentHexString, false);

            // Init alg 
            ISigner signer = SignerUtilities.GetSigner("SHA256withRSA");

            // Populate key 
            signer.Init(false, key);

            // Get the signature into bytes
            var expectedSig = Convert.FromBase64String(expectedSignature);

            // Get the bytes to be signed from the string
            var msgBytes = Encoding.UTF8.GetBytes(data);

            // Calculate the signature and see if it matches
            signer.BlockUpdate(msgBytes, 0, msgBytes.Length);
            return signer.VerifySignature(expectedSig);
        }
        #endregion Sign and Verify */
    

        #region AES Encrypt/Decrypt

        private static byte[] encryptAesEcb(byte[] message, CryptographicKey key)
        {

            IBuffer encrypted;
            IBuffer iv = null;
            IBuffer data = CryptographicBuffer.CreateFromByteArray(message);
            String algName = SymmetricAlgorithmNames.AesEcb;

            // Encrypt the data.
            try
            {
                encrypted = CryptographicEngine.Encrypt(key, data, iv);
            }
            catch (ArgumentException)
            {
                Debug.WriteLine("An invalid key size was selected for the given algorithm.\n"); 
                return null;
            }

            byte[] encryptedMsg = new byte[encrypted.Length];
            CryptographicBuffer.CopyToByteArray(encrypted, out encryptedMsg);
            return encryptedMsg;
        }

        private static byte[] decrypt(byte[] encrypted, CryptographicKey key)
        {
            IBuffer encryptedBuf = CryptographicBuffer.CreateFromByteArray(encrypted);
            IBuffer iv = null; 

            IBuffer decryptedBuf = CryptographicEngine.Decrypt(key, encryptedBuf, iv);
            byte[] decrypted = new byte[decryptedBuf.Length];

            CryptographicBuffer.CopyToByteArray(decryptedBuf, out decrypted);

            return decrypted; 
        }

        #endregion AES Encrypt/Decrypt

        #region Crypto Helpers

        /// <summary>
        /// Generate a cryptographically secure random number
        /// </summary>
        /// <param name="length">Length of the desired number in bytes</param>
        /// <returns></returns>
        private byte[] generateRandomBytes(uint length)
        {
            IBuffer buffer = CryptographicBuffer.GenerateRandom(length);
            byte[] rand = new byte[length];
            CryptographicBuffer.CopyToByteArray(buffer, out rand);
            return rand;
        }

        /// <summary>
        /// Combine the PIN with the salt
        /// </summary>
        /// <param name="salt">The salt</param>
        /// <param name="pin">The PIN</param>
        /// <returns></returns>
        private static byte[] saltPin(byte[] salt, string pin)
        {
            byte[] saltedPin = new byte[salt.Length + pin.Length];
            Array.Copy(salt, 0, saltedPin, 0, salt.Length);
            Array.Copy(pin.ToCharArray(), 0, saltedPin, salt.Length, pin.Length);
            return saltedPin;
        }

        /// <summary>
        /// Hash message using SHA1
        /// </summary>
        /// <param name="data">The data to hash</param>
        /// <returns></returns>
        private static IBuffer toSHA1Bytes(byte[] data)
        {
            IBuffer messageBuffer = CryptographicBuffer.ConvertStringToBinary(data.ToString(), BinaryStringEncoding.Utf8);

            // Create a HashAlgorithmProvider object that opens SHA1.
            HashAlgorithmProvider algProv = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);

            // Hash the message
            IBuffer hashedBuffer = algProv.HashData(messageBuffer);

            // Verify that the hash length equals the length specified for the algorithm.
            if (hashedBuffer.Length != algProv.HashLength)
            {
                throw new Exception("There was an error creating the hash");
            }
            return hashedBuffer; 
        }

        /// <summary>
        /// Generate an AES ECB key
        /// </summary>
        /// <param name="data">The data to base the key on</param>
        /// <returns>The AES key</returns>
        private CryptographicKey GenerateAesKey(byte[] data)
        {
            CryptographicKey key;
            // Create an SymmetricKeyAlgorithmProvider object with AES ECB
            SymmetricKeyAlgorithmProvider Algorithm = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesEcb);

            // Generate a symmetric key.
            IBuffer keyMaterial = toSHA1Bytes(data);
            try
            {
                key = Algorithm.CreateSymmetricKey(keyMaterial);
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine(ex.Message); 
                return null;
            }
            return key;
        }

        #endregion Crypto Helpers

    }

}
