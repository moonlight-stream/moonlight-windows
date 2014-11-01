using Limelight_common_binding;
using System;
using System.Collections.Generic;
using System.Text;

namespace Limelight.Streaming
{
    public class StreamContext
    {
        public Computer computer { get; set; }
        public int appId { get; set; }
        public LimelightStreamConfiguration streamConfig { get; set; }

        public StreamContext(Computer computer, int appId, LimelightStreamConfiguration streamConfig)
        {
            this.computer = computer;
            this.appId = appId;
            this.streamConfig = streamConfig;
        }
    }
}
