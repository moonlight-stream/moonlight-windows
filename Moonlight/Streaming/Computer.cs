using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Moonlight.Streaming
{
    /// <summary>
    /// Computer object containing name and IP address
    /// </summary>
    public class Computer
    {
        public string Name { get; set; }
        public string IpAddress { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Computer"/> class.
        /// </summary>
        /// <param name="name">Computer display name</param>
        /// <param name="ipAddress">Computer IP address</param>
        public Computer(string name, string ipAddress)
        {
            this.IpAddress = ipAddress;
            this.Name = name;
        }

        /// <summary>
        /// Computer information as a string
        /// </summary>
        /// <returns>String containing name and IP</returns>
        public override string ToString()
        {
            return this.Name + " " + this.IpAddress;
        }
    }
}
