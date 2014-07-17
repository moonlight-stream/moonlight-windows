using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
namespace Limelight_new
{
    /// <summary>
    /// XmlQuery object contains an XML string and methods to parse it
    /// </summary>
    public class XmlQuery
    {
        private Uri uri;
        private XDocument rawXml;
        private string rawXmlString;

        #region Public Methods
        /// <summary>
        ///  Initializes a new instance of the <see cref="XmlQuery"/> class. 
        /// </summary>
        /// <param name="url">URL of XML page</param>
        public XmlQuery(string url)
        {
            uri = new Uri(url);
            Task.Run(async () => await GetXml()); 
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
            Debug.WriteLine(uri);
            if (rawXmlString == null)
            {
                HttpClient client = new HttpClient();
                // Throws HttpClientException if something goes wrong
                rawXmlString = await client.GetStringAsync(uri);
                this.rawXml = XDocument.Parse(rawXmlString);
            }
        }
        #endregion Private Methods

    }
}