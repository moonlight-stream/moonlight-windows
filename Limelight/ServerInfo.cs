using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Xml.Linq;
using System.Linq; 
namespace Limelight
{
    /// <summary>
    /// ServerInfo object contains an XML string and methods to parse it
    /// </summary>
    public class ServerInfo : IDisposable
    {
        private ManualResetEvent completeEvent;
        private Uri uri;
        private XDocument rawXml;
        private string xmlString; 

        /// <summary>
        ///  Initializes a new instance of the <see cref="ServerInfo"/> class. 
        /// </summary>
        /// <param name="url">URL of server info</param>
        public ServerInfo(string url)
        {
            uri = new Uri(url);
            completeEvent = new ManualResetEvent(false);
            GetXml();
        }
        // TODO this is obsolete in Windows Phone 8.1 - use XmlDocument instead
        /// <summary>
        /// Gets the Xml as a string from the URL provided
        /// </summary>
        /// <returns>The server info XML as a string</returns>
        private void GetXml()
        {
            lock (this)
            {
                if (xmlString == null)
                {
                    WebClient client = new WebClient();
                    client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(XmlCallback);
                    client.DownloadStringAsync(uri);
                    // Wait for the callback to complete
                    completeEvent.WaitOne();
                    completeEvent.Dispose(); 
                }
            }
            this.rawXml = XDocument.Parse(xmlString); 
        }

        public string XmlAttribute(string tag)
        {
            var query = from c in rawXml.Descendants(tag) select c;
            string attribute = query.FirstOrDefault().Value;
            return attribute;
        }

        /// <summary>
        /// Sets the xmlString field to the XML string we just downloaded
        /// </summary>
        private void XmlCallback(object sender, DownloadStringCompletedEventArgs e)
        {
            // TODO error check

            this.xmlString = e.Result;
  
            completeEvent.Set();
        }

        #region IDisposable implementation

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