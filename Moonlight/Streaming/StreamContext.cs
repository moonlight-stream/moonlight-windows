using Moonlight_common_binding;
using System;
using System.Collections.Generic;
using System.Text;

namespace Moonlight.Streaming
{
    public class StreamContext
    {
        public Computer computer { get; set; }
        public int appId { get; set; }
        public MoonlightStreamConfiguration streamConfig { get; set; }

        public StreamContext(Computer computer, int appId, MoonlightStreamConfiguration streamConfig)
        {
            this.computer = computer;
            this.appId = appId;
            this.streamConfig = streamConfig;
        }
    }
}
