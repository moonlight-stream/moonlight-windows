using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zeroconf; 

namespace Limelight
{
    public class Computer
    {
        public string name {get; set;}
        public string ipAddress { get; set; }
        public Computer(string name, string ipAddress)
        {
            this.ipAddress = ipAddress;
            this.name = name; 
        }

        public override string ToString()
        {
            return (this.name + " " + this.ipAddress);
        }
    }
}
