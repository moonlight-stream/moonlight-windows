namespace Limelight
{
    using Org.BouncyCastle.Asn1.X509;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Generators;
    using Org.BouncyCastle.Math;
    using Org.BouncyCastle.OpenSsl;
    using Org.BouncyCastle.Pkcs;
    using Org.BouncyCastle.Security;
    using Org.BouncyCastle.X509;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Windows.Security.Cryptography.Certificates;
    using Windows.Storage;
    using Windows.Storage.Streams;
    using Windows.UI.Xaml.Controls;
    using System.Runtime.InteropServices.WindowsRuntime;
    using Windows.Security.Cryptography;

    /// <summary>
    /// Functions for signing and verifying data
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private X509Certificate cert = null;
        private AsymmetricCipherKeyPair keyPair; 
        private byte[] pemCertBytes;

        /// <summary>
        /// Get client X509 Certificate
        /// </summary>
        /// <returns>The client's X509 certificate </returns>
        public X509Certificate getClientCertificate()
        {
            // Use a lock here to ensure only one guy will be generating or loading
            // the certificate and key at a time
            lock (certLock)
            {
                // Return a loaded cert if we have one
                if (cert != null)
                {
                    return cert;
                }

                // No loaded cert yet, let's see if we have one on disk
                if (LoadCertKeyPair())
                {
                    // Got one
                    return cert;
                }

                // We don't have a cert yet - generate a new key pair
                GenerateCertKeyPair();

                // Load the generated pair
                LoadCertKeyPair();
                return cert;
            }
        }

        /// <summary>
        /// Generate a cert/key pair
        /// </summary>
        private void GenerateCertKeyPair()
        {
            // Generate RSA key pair
            RsaKeyPairGenerator r = new RsaKeyPairGenerator();
            r.Init(new KeyGenerationParameters(new SecureRandom(), 2048));
            keyPair = r.GenerateKeyPair();

            // Generate the X509 certificate
            X509V3CertificateGenerator certGen = new X509V3CertificateGenerator();
            X509Name dnName = new X509Name("CN=NVIDIA GameStream Client");

            certGen.SetSerialNumber(BigInteger.ValueOf(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond));
            certGen.SetSubjectDN(dnName);
            certGen.SetIssuerDN(dnName); // use the same
            // Expires in 20 years
            certGen.SetNotBefore(DateTime.Now);
            certGen.SetNotAfter(DateTime.Now.AddYears(20));
            certGen.SetPublicKey(keyPair.Public);
            certGen.SetSignatureAlgorithm("SHA1withRSA");

            try
            {
                cert = certGen.Generate(keyPair.Private);

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
            }

            Task.Run(async () => await SaveCertKeyPair()).Wait(); 
        }

        private X509Certificate extractPlainCert(XmlQuery q, String tag)
        {
            String certHexString = q.XmlAttribute(tag);
            byte[] certBytes = HexToBytes(certHexString);
            String certText = Encoding.UTF8.GetString(certBytes, 0, certBytes.Length);

            PemReader certReader = new PemReader(new StringReader(certText));
            return (X509Certificate)certReader.ReadObject();
        }

        #region Sign and Verify

        /// <summary>
        /// Verify signature
        /// </summary>
        /// <returns>Boolean indicating success</returns>
        public bool VerifySignature(byte[] data, byte[] expectedSignature, X509Certificate cert)
        {
            /* Init alg */
            ISigner signer = SignerUtilities.GetSigner("SHA256withRSA");

            /* Populate key */
            signer.Init(false, cert.GetPublicKey());

            /* Calculate the signature and see if it matches */
            signer.BlockUpdate(data, 0, data.Length);
            return signer.VerifySignature(expectedSignature);
        }

        /// <summary>
        /// Sign data using SHA256 with RSA
        /// </summary>
        /// <param name="data">The data to sign</param>
        /// <param name="key">Private key to sign with</param>
        /// <returns>The signature</returns>
        private byte[] SignData(byte[] data)
        {
            ISigner sig = SignerUtilities.GetSigner("SHA256withRSA");
            sig.Init(true, keyPair.Private);

            /* Calc the signature */
            sig.BlockUpdate(data, 0, data.Length);
            byte[] signature = sig.GenerateSignature();

            return signature;
        }

        private static AsymmetricCipherKeyPair GenerateKeys(int keySize)
        {
            var gen = new RsaKeyPairGenerator();
            var secureRandom = new SecureRandom();
            var keyGenParam = new KeyGenerationParameters(secureRandom, keySize);
            gen.Init(keyGenParam);
            return gen.GenerateKeyPair();
        }
        #endregion Sign and Verify

        #region Cert Store
        private async Task AddToWinCertStore()
        {
            Pkcs12Store store = new Pkcs12Store();
            string friendlyName = "Limelight-Client";
            var certEntry = new X509CertificateEntry(cert);
            store.SetCertificateEntry(friendlyName, certEntry);

            var keyEntry = new AsymmetricKeyEntry(keyPair.Private);
            store.SetKeyEntry(friendlyName, keyEntry, new[] { certEntry }); 

            // Copy the Pkcs12Store to a stream using an arbitrary password
            const string password = "password";
            var stream = new MemoryStream();
            store.Save(stream, password.ToCharArray(), new SecureRandom());

            // Write to .PFX string
            byte[] arr = stream.ToArray();

            IBuffer buf = arr.AsBuffer();
            string pfx = CryptographicBuffer.EncodeToBase64String(buf);

            await CertificateEnrollmentManager.ImportPfxDataAsync(pfx, password, ExportOption.NotExportable, KeyProtectionLevel.NoConsent, InstallOptions.None, friendlyName);
        }
        #endregion Cert Store
    }
}