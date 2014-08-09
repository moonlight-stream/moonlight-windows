namespace Limelight
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
        private static char[] hexArray = "0123456789ABCDEF".ToCharArray();

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

            //Calc the signature
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

        private static byte[] EncryptAes(byte[] message, CryptographicKey key)
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

        private static byte[] DecryptAes(byte[] encrypted, CryptographicKey key)
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
        private byte[] GenerateRandomBytes(uint length)
        {
            IBuffer buffer = CryptographicBuffer.GenerateRandom(length);
            byte[] rand = new byte[length];
            CryptographicBuffer.CopyToByteArray(buffer, out rand);
            return rand;
        }


        /// <summary>
        /// Convert a byte array to a hexidecimal string
        /// </summary>
        /// <param name="bytes">Byte array to convert</param>
        /// <returns>Resulting hexidecimal string</returns>
        private static string bytesToHex(byte[] bytes)
        {
            char[] hexChars = new char[bytes.Length * 2];
	        for ( int j = 0; j < bytes.Length; j++ ) {
	            int v = bytes[j] & 0xFF;
	            hexChars[j * 2] = hexArray[v >> 4];
	            hexChars[j * 2 + 1] = hexArray[v & 0x0F];
	        }
	        return new String(hexChars);
        }

        private static byte[] HexToBytes(string s)
        {
            int len = s.Length;
            byte[] data = new byte[len / 2];
            // TODO make this work in C#
            for (int i = 0; i < len; i += 2)
            {
               // data[i / 2] = (byte)((Character.digit(s.charAt(i), 16) << 4)
                //                     + Character.digit(s.charAt(i + 1), 16));
            }
            return data;
        }


        /// <summary>
        /// Combine the PIN with the salt
        /// </summary>
        /// <param name="salt">The salt</param>
        /// <param name="pin">The PIN</param>
        /// <returns>Salted PIN</returns>
        private static byte[] SaltPin(byte[] salt, string pin)
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
        private static IBuffer ToSHA1Bytes(byte[] data)
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
            IBuffer keyMaterial = ToSHA1Bytes(data);
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


       #region Challenges
        private bool Challenges(string uniqueId)
        {
        // Generate a random challenge and encrypt it with our AES key
		byte[] randomChallenge = GenerateRandomBytes(16);
		byte[] encryptedChallenge = EncryptAes(randomChallenge, aesKey);

		// Send the encrypted challenge to the server
		XmlQuery challengeResp = new XmlQuery(nv.baseUrl + 
				"/pair?uniqueid="+uniqueId+"&devicename=roth&updateState=1&clientchallenge="+bytesToHex(encryptedChallenge));
        // If we're not paired, there's a problem. 
		if (!challengeResp.XmlAttribute("paired").Equals("1")) {
            try
            {
                challengeResp = new XmlQuery(nv.baseUrl + "/unpair?uniqueid=" + uniqueId);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error hitting unpair URL " + e.StackTrace);
            }
			return false;
		}

            // Decode the server's response and subsequent challenge
            byte[] encServerChallengeResponse = HexToBytes(challengeResp.XmlAttribute("challengeresponse"));
            byte[] decServerChallengeResponse = DecryptAes(encServerChallengeResponse, aesKey);

            byte[] serverResponse, serverChallenge;
            //Array.ConstrainedCopy(decServerChallengeResponse, 0, serverResponse, 20, serverResponse.Length + decServerChallengeResponse.Length);
            //Array.ConstrainedCopy(decServerChallengeResponse, 20, serverChallenge, 36, serverChallenge.Length + decServerChallengeResponse.Length);


            // Using another 16 bytes secret, compute a challenge response hash using the secret, our cert sig, and the challenge
            byte[] clientSecret = GenerateRandomBytes(16);
           // byte[] challengeRespHash = ToSHA1Bytes(concatBytes(concatBytes(serverChallenge, cert.getSignature()), clientSecret));
            //byte[] challengeRespEncrypted = EncryptAes(challengeRespHash, aesKey);
            XmlQuery secretResp = null; //new XmlQuery(nv.baseUrl +
                    //"/pair?uniqueid=" + uniqueId + "&devicename=roth&updateState=1&serverchallengeresp=" + bytesToHex(challengeRespEncrypted));
            if (!secretResp.XmlAttribute("paired").Equals("1"))
            {
                Unpair(); 
                return false;
            }

            // Get the server's signed secret
            byte[] serverSecretResp = HexToBytes(secretResp.XmlAttribute("pairingsecret"));
            byte[] serverSecret; byte[] serverSignature; 
            //Array.Copy(serverSecretResp, 0, serverSecret, 0, 16);
           //Array.Copy(serverSecretResp, 0, serverSignature, 0, 256);

            /* TODO this section
            // Ensure the authenticity of the data
            if (!verifySignature(serverSecret, serverSignature, serverCert))
            {
                // Cancel the pairing process
                Unpair(); 
                // Looks like a MITM
                return false;
            }

            // Ensure the server challenge matched what we expected (aka the PIN was correct)
            byte[] serverChallengeRespHash = ToSHA1Bytes(concatBytes(concatBytes(randomChallenge, serverCert.getSignature()), serverSecret));
            if (!Array.Equals(serverChallengeRespHash, serverResponse))
            {
                // Cancel the pairing process
                Unpair(); 
                // Probably got the wrong PIN
                return false;
            }

            // Send the server our signed secret
            byte[] clientPairingSecret = concatBytes(clientSecret, signData(clientSecret, pk));
            XmlQuery clientSecretResp = new XmlQuery(nv.baseUrl +
                    "/pair?uniqueid=" + uniqueId + "&devicename=roth&updateState=1&clientpairingsecret=" + bytesToHex(clientPairingSecret));
            if (!clientSecretResp.XmlAttribute("paired").Equals("1"))
            {
                Unpair();
                return false; 
            }

            // Do the initial challenge (seems neccessary for us to show as paired)
            XmlQuery pairChallenge; 
            try
            {
                pairChallenge = new XmlQuery(nv.baseUrl + "/pair?uniqueid=" + uniqueId + "&devicename=roth&updateState=1&phrase=pairchallenge");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Pair challenge failed " + e.StackTrace);
                return false; 
            }
            if (!pairChallenge.XmlAttribute("paired").Equals("1"))
            {
                Unpair();
                return false; 
            } */
            return true; 
        } 

        private void Unpair()
        {
            XmlQuery unpair;
            try
            {
                unpair = new XmlQuery(nv.baseUrl + "/unpair?uniqueid=" + nv.GetDeviceName());
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error hitting unpair URL " + e.StackTrace);
            }
        }

        private static byte[] concatBytes(byte[] a, byte[] b)
        {
            byte[] c = new byte[a.Length + b.Length];
            Array.Copy(a, 0, c, 0, a.Length);
            Array.Copy(b, 0, c, a.Length, b.Length);
            return c;
        }
    #endregion Challenges

    }
}
