namespace Limelight
{
    using Org.BouncyCastle.X509;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using Windows.Security.Cryptography;
    using Windows.Security.Cryptography.Core;
    using Windows.Storage.Streams;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// Cryptography used in pairing with the streaming machine
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Object certLock = new Object();
        private static char[] hexArray = "0123456789ABCDEF".ToCharArray();

        #region AES Encrypt/Decrypt

        /// <summary>
        /// Encrypt using AES/ECB
        /// </summary>
        /// <param name="message">The message to encrypt</param>
        /// <param name="key">The key with which to encrypt</param>
        /// <returns>Encrypted data</returns>
        private static byte[] EncryptAes(byte[] message, CryptographicKey key)
        {
            int blockRoundedSize = ((message.Length + 15) / 16) * 16;
            byte[] paddedMessage = new byte[blockRoundedSize];
            Array.Copy(message, paddedMessage, message.Length);

            IBuffer encrypted;
            IBuffer iv = null;
            IBuffer data = CryptographicBuffer.CreateFromByteArray(paddedMessage);
            String algName = SymmetricAlgorithmNames.AesEcb;

            // Encrypt the data.
            try
            {
                encrypted = CryptographicEngine.Encrypt(key, data, iv);
            }
            catch (Exception)
            {
                Debug.WriteLine("An invalid key size was selected for the given algorithm.\n"); 
                return null;
            }

            byte[] encryptedMsg = new byte[encrypted.Length];
            CryptographicBuffer.CopyToByteArray(encrypted, out encryptedMsg);
            return encryptedMsg;
        }

        /// <summary>
        /// Decrypt using AES
        /// </summary>
        /// <param name="encrypted">Data to decrypt</param>
        /// <param name="key">Key with which to decrypt</param>
        /// <returns>Decrypted data</returns>
        private static byte[] DecryptAes(byte[] encrypted, CryptographicKey key)
        {
            int blockRoundedSize = ((encrypted.Length + 15) / 16) * 16;
            byte[] paddedEncrypted = new byte[blockRoundedSize];
            Array.Copy(encrypted, paddedEncrypted, encrypted.Length);

            IBuffer encryptedBuf = CryptographicBuffer.CreateFromByteArray(paddedEncrypted);
            IBuffer iv = null; 

            IBuffer decryptedBuf = CryptographicEngine.Decrypt(key, encryptedBuf, iv);
            byte[] decrypted = new byte[decryptedBuf.Length];

            CryptographicBuffer.CopyToByteArray(decryptedBuf, out decrypted);

            return decrypted; 
        }

        #endregion AES Encrypt/Decrypt

        #region Challenges

        private bool Challenges(string uniqueId)
        {
            // "Please don't do this ever, but it's only okay because Cameron said so" -Cameron Gutman
            getClientCertificate(); 
            // Generate a salt for hashing the PIN
            byte[] salt = GenerateRandomBytes(16);

            string pin = "0000";
            // Combine the salt and pin, then create an AES key from them
            byte[] saltAndPin = SaltPin(salt, pin);
            aesKey = GenerateAesKey(saltAndPin);

            // Send the salt and get the server cert

            XmlQuery getServerCert = new XmlQuery(nv.baseUrl + "/pair?uniqueid=" + uniqueId +
                "&devicename=roth&updateState=1&phrase=getservercert&salt=" + bytesToHex(salt) + "&clientcert=" + bytesToHex(pemCertBytes));
            
            if (!getServerCert.XmlAttribute("paired").Equals("1"))
            {
                Unpair();
                return false; 
            }

            X509Certificate serverCert = extractPlainCert(getServerCert, "plaincert");

            // Generate a random challenge and encrypt it with our AES key
		    byte[] randomChallenge = GenerateRandomBytes(16);
            Debug.WriteLine("Client challenge: " + bytesToHex(randomChallenge));
		    byte[] encryptedChallenge = EncryptAes(randomChallenge, aesKey);

		    // Send the encrypted challenge to the server
		    XmlQuery challengeResp = new XmlQuery(nv.baseUrl + 
				    "/pair?uniqueid="+uniqueId+"&devicename=roth&updateState=1&clientchallenge="+bytesToHex(encryptedChallenge));
            // If we're not paired, there's a problem. 
		    if (!challengeResp.XmlAttribute("paired").Equals("1")) {
                Unpair(); 
			    return false;
		    }

            // Decode the server's response and subsequent challenge
            byte[] encServerChallengeResponse = HexToBytes(challengeResp.XmlAttribute("challengeresponse"));
            byte[] decServerChallengeResponse = DecryptAes(encServerChallengeResponse, aesKey);


            byte[] serverResponse = new byte[20], serverChallenge = new byte[16];
            Array.Copy(decServerChallengeResponse, serverResponse, serverResponse.Length);
            Array.Copy(decServerChallengeResponse, 20, serverChallenge, 0, serverChallenge.Length);
            Debug.WriteLine("serverResponse: " + bytesToHex(serverResponse));
            Debug.WriteLine("server challenge: " + bytesToHex(serverChallenge));



            // Using another 16 bytes secret, compute a challenge response hash using the secret, our cert sig, and the challenge
            byte[] clientSecret = GenerateRandomBytes(16);
            Debug.WriteLine("Client secret: " + bytesToHex(clientSecret));
            Debug.WriteLine("Client sig: " + bytesToHex(cert.GetSignature()));

            byte[] challengeRespHash = ToSHA1Bytes(concatBytes(concatBytes(serverChallenge, cert.GetSignature()), clientSecret));
            Debug.WriteLine("Challenge SHA 1: " + bytesToHex(challengeRespHash));
            byte[] challengeRespEncrypted = EncryptAes(challengeRespHash, aesKey);
            XmlQuery secretResp = new XmlQuery(nv.baseUrl +
                    "/pair?uniqueid=" + uniqueId + "&devicename=roth&updateState=1&serverchallengeresp=" + bytesToHex(challengeRespEncrypted));
            if (!secretResp.XmlAttribute("paired").Equals("1"))
            {
                Unpair(); 
                return false;
            }

            // Get the server's signed secret
            byte[] serverSecretResp = HexToBytes(secretResp.XmlAttribute("pairingsecret"));
            byte[] serverSecret = new byte[16]; byte[] serverSignature = new byte[256]; 
            Array.Copy(serverSecretResp, 0, serverSecret, 0, 16);
            Array.Copy(serverSecretResp, 16, serverSignature, 0, 256);

            // Ensure the authenticity of the data
            if (!VerifySignature(serverSecret, serverSignature, serverCert))
            {
                // Cancel the pairing process
                Unpair(); 
                // Looks like a MITM
                return false;
            }

            // Ensure the server challenge matched what we expected (aka the PIN was correct)
            byte[] serverChallengeRespHash = ToSHA1Bytes(concatBytes(concatBytes(randomChallenge, serverCert.GetSignature()), serverSecret));
            if (!serverChallengeRespHash.SequenceEqual(serverResponse))
            {
                // Cancel the pairing process
                Unpair(); 
                // Probably got the wrong PIN
                return false;
            }

            // Send the server our signed secret
            byte[] clientPairingSecret = concatBytes(clientSecret, SignData(clientSecret));
            XmlQuery clientSecretResp = new XmlQuery(nv.baseUrl +
                    "/pair?uniqueid=" + uniqueId + "&devicename=roth&updateState=1&clientpairingsecret=" + bytesToHex(clientPairingSecret));
            if (!clientSecretResp.XmlAttribute("paired").Equals("1"))
            {
                Unpair();
                return false; 
            }

            // Do the initial challenge (seems neccessary for us to show as paired)
            XmlQuery pairChallenge = new XmlQuery(nv.baseUrl + "/pair?uniqueid=" + uniqueId + "&devicename=roth&updateState=1&phrase=pairchallenge");

            if (!pairChallenge.XmlAttribute("paired").Equals("1"))
            {
                Unpair();
                return false; 
            } 
            return true; 
        } 

        /// <summary>
        /// Unpair from the device
        /// </summary>
        private void Unpair()
        {
            XmlQuery unpair;
            try
            {
                unpair = new XmlQuery(nv.baseUrl + "/unpair?uniqueid=" + nv.GetDeviceName());
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error hitting unpair URL " + e.Message);
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
        #region Helpers
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
            for (int j = 0; j < bytes.Length; j++)
            {
                int v = bytes[j] & 0xFF;
                hexChars[j * 2] = hexArray[v >> 4];
                hexChars[j * 2 + 1] = hexArray[v & 0x0F];
            }
            return new String(hexChars);
        }

        /// <summary>
        /// Converts a hex string to a byte array
        /// </summary>
        /// <param name="hex">Hex string</param>
        /// <returns>Byte array of the hex string</returns>
        private static byte[] HexToBytes(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
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
            Array.Copy(System.Text.Encoding.UTF8.GetBytes(pin), 0, saltedPin, salt.Length, pin.Length);
            return saltedPin;
        }

        /// <summary>
        /// Hash message using SHA1
        /// </summary>
        /// <param name="data">The data to hash</param>
        /// <returns>Byte array containing SHA1 hash</returns>
        private static byte[] ToSHA1Bytes(byte[] data)
        {
            IBuffer messageBuffer = CryptographicBuffer.CreateFromByteArray(data);

            // Create a HashAlgorithmProvider object that opens SHA1.
            HashAlgorithmProvider algProv = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);

            // Hash the message
            IBuffer hashedBuffer = algProv.HashData(messageBuffer);

            // Verify that the hash length equals the length specified for the algorithm.
            if (hashedBuffer.Length != algProv.HashLength)
            {
                throw new Exception("There was an error creating the hash");
            }
            byte[] hashed = new byte[hashedBuffer.Length];
            CryptographicBuffer.CopyToByteArray(hashedBuffer, out hashed);
            return hashed;
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

            byte[] truncatedSHA = new byte[16];
            Debug.WriteLine("Salted pin: " + bytesToHex(data));
            Array.Copy(ToSHA1Bytes(data), truncatedSHA, 16);
            Debug.WriteLine("AES key material: " + bytesToHex(truncatedSHA));
            // Generate a symmetric key from our data
            IBuffer keyMaterial = CryptographicBuffer.CreateFromByteArray(truncatedSHA);
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
        #endregion Helpers

    }
}
