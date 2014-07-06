using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml.Linq;
namespace Limelight
{
    /// <summary>
    /// XmlQuery object contains an XML string and methods to parse it
    /// </summary>
    public class XmlQuery : IDisposable
    {
        private ManualResetEvent completeEvent;
        private Uri uri;
        private XDocument rawXml;
        private string rawXmlString;
        private string err = null;

        #region Public Methods
        /// <summary>
        ///  Initializes a new instance of the <see cref="XmlQuery"/> class. 
        /// </summary>
        /// <param name="url">URL of XML page</param>
        public XmlQuery(string url)
        {
            uri = new Uri(url);
            completeEvent = new ManualResetEvent(false);
            GetXml(); 
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

        /// <summary>
        /// Returns the XML error message for the caller
        /// </summary>
        /// <returns>Error message string. If no error, returns null</returns>
        public string GetErrorMessage()
        {
            return err; 
        }
        #endregion Public Methods

        #region Private Methods
        /// <summary>
        /// Gets the Xml as a string from the URL provided
        /// </summary>
        /// <returns>The server info XML as a string</returns>
        private void GetXml()
        {
            Debug.WriteLine(uri);
            if (rawXmlString == null)
            {
                WebClient client = new WebClient();
                client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(XmlCallback);
                client.DownloadStringAsync(uri);

                // Wait for the callback to complete
                completeEvent.WaitOne();
                // We don't need this anymore - dispose
                completeEvent.Dispose();
            }
            // If no error occured, convert the string to a more easily-parsable XDocument
            if (err == null)
            {
                this.rawXml = XDocument.Parse(rawXmlString);
            }
        }

        /// <summary>
        /// Sets the xmlString field to the XML string we just downloaded
        /// </summary>
        private void XmlCallback(object sender, DownloadStringCompletedEventArgs e)
        {
            // If an error occurred downloading the XML, fail
            if (e.Error != null) {
                ExitFailure(e.Error.Message);
            }
                // Otherwise, get the XML String
            else
            {
                this.rawXmlString = e.Result;
            }
            // Unblock the thread
            completeEvent.Set();
        }
        /// <summary>
        /// If XML query fails, set an error message for the caller
        /// </summary>
        /// <param name="failureMessage">Error message</param>
        private void ExitFailure(string failureMessage)
        {
            Debug.WriteLine("Failed: " + failureMessage);
            err = failureMessage; 
        }
        #endregion Private Methods

        #region IDisposable implementation
        /// <summary>
        /// Dispose of the ManualResetEvent
        /// </summary>
        protected virtual void Dispose(bool managed)
        {
            if (managed)
            {
                completeEvent.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable implementation
    }
}