using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Windows.Web.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Security.Cryptography.Certificates;
using Windows.Web.Http.Filters;
using System.Collections.Generic;
using Org.BouncyCastle.Pkcs;
namespace Limelight
{
    /// <summary>
    /// XmlQuery object contains an XML string and methods to parse it
    /// </summary>
    public class XmlQuery
    {
        private Uri uri;
        private XDocument rawXml;
        private string rawXmlString;
        private Windows.Web.Http.HttpClient client; 

        #region Public Methods
        /// <summary>
        ///  Initializes a new instance of the <see cref="XmlQuery"/> class. 
        /// </summary>
        /// <param name="url">URL of XML page</param>
        public XmlQuery(string url)
        {
            uri = new Uri(url);
            
            // TODO get rid of this gross .Wait(). Maybe caller should just call GetXml(); 
            Task.Run(async () => await GetXml()).Wait();
        }

        /// <summary>
        /// Given a tag, return the first XML attribute contained in this tag as a string
        /// </summary>
        /// <param name="tag">Tag containing the desired attribute</param>
        /// <returns>The first attribute within the given tag</returns>
        public string XmlAttribute(string tag)
        {
            // TODO handle not found
            var query = from c in rawXml.Descendants(tag) select c;
            string attribute = query.FirstOrDefault().Value;
            return attribute;
        }

        /// <summary>
        /// Given an XElement and a tag, search within that element for the first attribute contained within the tag
        /// </summary>
        /// <param name="tag">XML tag</param>
        /// <param name="element">XElement to search within</param>
        /// <returns>The first attribute within the given tag in the XElement</returns>
        public string XmlAttribute(string tag, XElement element)
        {
            // TODO handle not found
            var query = from c in element.Descendants(tag) select c;
            string attribute = query.FirstOrDefault().Value;
            return attribute;
        }

        /// <summary>
        /// Given a tag, return the first XML attribute contained in this tag as an XElement
        /// </summary>
        /// <param name="tag">Tag containing the desired attribute</param>
        /// <returns>The first attribute within the given tag</returns>
        public XElement XmlAttributeElement(string tag)
        {
            // TODO handle not found
            var query = from c in rawXml.Descendants(tag) select c;
            return query.FirstOrDefault(); 
        }

        #endregion Public Methods

        #region Private Methods
        /// <summary>
        /// Gets the Xml as a string from the URL provided
        /// </summary>
        /// <returns>The server info XML as a string</returns>
        private async Task GetXml()
        {
            HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);

            IEnumerable<Certificate> certificates = await CertificateStores.FindAllAsync(new CertificateQuery { FriendlyName = "Limelight-Client" });
            filter.ClientCertificate = certificates.Single();

            client = new Windows.Web.Http.HttpClient(filter);
            Debug.WriteLine(uri);
            if (rawXmlString == null)
            {
                try
                {
                    rawXmlString = await client.GetStringAsync(uri);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                }
                Debug.WriteLine(rawXmlString);
                try
                {
                    this.rawXml = XDocument.Parse(rawXmlString);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                    Debug.WriteLine(ex.InnerException);
                }

            }
        }
        #endregion Private Methods
    }
}