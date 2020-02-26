using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkaNetCore.Config
{
    public class AppSettings
    {
        public string MonitorTool { get; set; }     // none , win, azure

        public string MonitorToolCon { get; set; }

    }
}
