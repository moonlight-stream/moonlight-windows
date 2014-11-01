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
        private bool ranQuery;

        #region Public Methods
        /// <summary>
        ///  Initializes a new instance of the <see cref="XmlQuery"/> class. 
        /// </summary>
        /// <param name="url">URL of XML page</param>
        public XmlQuery(string url)
        {
            uri = new Uri(url);
        }

        /// <summary>
        /// Given a tag, return the first XML attribute contained in this tag as a string
        /// </summary>
        /// <param name="tag">Tag containing the desired attribute</param>
        /// <returns>The first attribute within the given tag</returns>
        public async Task<string> ReadXmlAttribute(string tag)
        {
            // Do the query if we haven't yet
            await GetXml();

            return ReadXmlAttribute(tag, rawXml);
        }

        /// <summary>
        /// Given an XElement and a tag, search within that element for the first attribute contained within the tag
        /// </summary>
        /// <param name="tag">XML tag</param>
        /// <param name="element">XContainer to search within</param>
        /// <returns>The first attribute within the given tag in the XContainer</returns>
        public string ReadXmlAttribute(string tag, XContainer element)
        {
            if (element == null)
            {
                return null;
            }

            var query = from c in element.Descendants(tag) select c;

            // Not found 
            if (query == null)
            {
                return null; 
            }

            return query.FirstOrDefault().Value;
        }

        /// <summary>
        /// From known information, find another attribute in the XElement
        /// </summary>
        /// <param name="tag">Known tag</param>
        /// <param name="attribute">Known attribute</param>
        /// <param name="tagToFind">Tag from within we want to find an attribute</param>
        /// <returns>The found attribute</returns>
        public async Task<string> SearchAttribute(string outerTag, string innerTag, string attribute, string tagToFind)
        {
            // Do the query if we haven't yet
            await GetXml();

            // Get all elements with specified tag
            var query = from c in rawXml.Descendants(outerTag) select c;

            // Look for the one with the attribute we already know
            foreach (XElement x in query){
                if (ReadXmlAttribute(innerTag, x) == attribute)
                {
                    return ReadXmlAttribute(tagToFind, x);
                }
            }
            // Not found
            return null; 
        }

        public async Task Run()
        {
            await GetXml();
        }

        #endregion Public Methods

        #region Private Methods
        /// <summary>
        /// Gets the Xml as a string from the URL provided
        /// </summary>
        /// <returns>The server info XML as a string</returns>
        private async Task GetXml()
        {
            // Return if we've already been here
            if (ranQuery)
            {
                return;
            }
            else
            {
                ranQuery = true;
            }

            HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.Untrusted);
            filter.IgnorableServerCertificateErrors.Add(ChainValidationResult.InvalidName);

            IEnumerable<Certificate> certificates = await CertificateStores.FindAllAsync(new CertificateQuery { FriendlyName = "Limelight-Client" });
            filter.ClientCertificate = certificates.Single();

            client = new Windows.Web.Http.HttpClient(filter);
            Debug.WriteLine(uri);

            try
            {
                rawXmlString = await client.GetStringAsync(uri);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return;
            }

            Debug.WriteLine(rawXmlString);

            // Up to the caller to deal with exceptions resulting here
            this.rawXml = XDocument.Parse(rawXmlString);
        }
        #endregion Private Methods
    }
}