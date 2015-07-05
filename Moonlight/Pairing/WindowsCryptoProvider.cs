namespace Moonlight
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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;
    using Windows.Security.Cryptography;
    using Windows.Security.Cryptography.Certificates;
    using Windows.Storage;
    using Windows.Storage.Streams;

    /// <summary>
    /// Functions for signing and verifying data
    /// </summary>
    public class WindowsCryptoProvider
    {
        private X509Certificate cert;
        private AsymmetricCipherKeyPair keyPair; 
        private byte[] pemCertBytes;

        private static Object certLock = new Object();
        private static Task keypairGenerationTask;

        /// <summary>
        /// Initialize keys for use in pairing
        /// </summary>
        public async Task InitializeCryptoProviderKeys()
        {
            // Return if we have one loaded
            if (cert != null)
            {
                return;
            }

            // No loaded cert yet, let's see if we have one on disk
            if (await LoadCertKeyPair())
            {
                // Got one
                return;
            }

            if (keypairGenerationTask != null)
            {
                // Another thread is already generating a key pair
                // Let's wait for them to finish.
                // FIXME: There's a little race here where it could be
                // set to null below after we read it but before we await it
                await keypairGenerationTask;
            }

            // We don't have a cert yet - generate a new key pair
            keypairGenerationTask = GenerateCertKeyPair();
            await keypairGenerationTask;
            keypairGenerationTask = null;

            // Load the generated pair
            await LoadCertKeyPair();
        }

        /// <summary>
        /// Get client X509 Certificate
        /// </summary>
        /// <returns>The client's X509 certificate </returns>
        public async Task<X509Certificate> GetClientCertificate()
        {
            await InitializeCryptoProviderKeys();
            return cert;
        }

        public async Task<byte[]> GetPemCertBytes()
        {
            await InitializeCryptoProviderKeys();
            return pemCertBytes;
        }

        /// <summary>
        /// Get keypair
        /// </summary>
        /// <returns>The keypair </returns>
        public async Task<AsymmetricCipherKeyPair> GetKeyPair()
        {
            await InitializeCryptoProviderKeys();
            return keyPair;
        }

        /// <summary>
        /// Generate a cert/key pair
        /// </summary>
        private async Task GenerateCertKeyPair()
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
                Debug.WriteLine(ex.Message);
                return;
            }

            await SaveCertKeyPair();
        }

        /// <summary>
        /// Generate RSA keys to use in pairing crypto
        /// </summary>
        /// <param name="keySize">Key size in bits</param>
        /// <returns>The generated RSA key pair</returns>
        private static AsymmetricCipherKeyPair GenerateKeys(int keySize)
        {
            var gen = new RsaKeyPairGenerator();
            var secureRandom = new SecureRandom();
            var keyGenParam = new KeyGenerationParameters(secureRandom, keySize);
            gen.Init(keyGenParam);
            return gen.GenerateKeyPair();
        }

        #region Cert Store

        /// <summary>
        /// Add a cert to the store
        /// </summary>
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

        #region Persistent Crypto Settings

        /// <summary>
        /// Save the cert key pair to the device so it can be reused
        /// </summary>
        private async Task SaveCertKeyPair()
        {
            var settings = ApplicationData.Current.RoamingSettings;
            PemWriter certWriter = new PemWriter(new StringWriter());
            PemWriter keyWriter = new PemWriter(new StringWriter());

            keyWriter.WriteObject(keyPair);
            keyWriter.Writer.Flush();

            // Add the cert to the cert store. This changes the fingerprint so we read it
            // back before we serialize it so we have the up to date cert.
            await AddToWinCertStore();

            // Read the modified cert from the cert store
            IEnumerable<Certificate> certificates = await CertificateStores.FindAllAsync(new CertificateQuery { FriendlyName = "Limelight-Client" });
            cert = new X509CertificateParser().ReadCertificate(certificates.Single().GetCertificateBlob().AsStream());

            certWriter.WriteObject(cert);
            certWriter.Writer.Flush();

            // Line endings MUST be UNIX for the PC to accept the cert properly
            string keyStr = keyWriter.Writer.ToString();
            keyStr = keyStr.Replace("\r\n", "\n");

            string certStr = certWriter.Writer.ToString();
            certStr = certStr.Replace("\r\n", "\n");

            settings.Values["cert"] = certStr;
            settings.Values["key"] = keyStr;
        }

        /// <summary>
        /// Load the cert/key pair from memory
        /// </summary>
        /// <returns>Value indicating success</returns>
        private async Task<bool> LoadCertKeyPair()
        {
            var settings = ApplicationData.Current.RoamingSettings;
            IEnumerable<Certificate> certificates = await CertificateStores.FindAllAsync(new CertificateQuery { FriendlyName = "Limelight-Client" });
            if (settings.Values.ContainsKey("cert") && settings.Values.ContainsKey("key") && certificates.Count() > 0)
            {
                string certStr = ApplicationData.Current.RoamingSettings.Values["cert"].ToString();
                string keyStr = ApplicationData.Current.RoamingSettings.Values["key"].ToString();

                PemReader certReader = new PemReader(new StringReader(certStr));
                PemReader keyReader = new PemReader(new StringReader(keyStr));

                keyPair = (AsymmetricCipherKeyPair)keyReader.ReadObject();
                cert = (X509Certificate)certReader.ReadObject();
                pemCertBytes = System.Text.Encoding.UTF8.GetBytes(certStr);

                return true;
            }
            // We didn't load the cert/key pair properly
            else
            {
                return false;
            }
        }
        #endregion Persistent Crypto Settings
    }
}