using System;
using System.Collections.Generic;
using System.Text;

namespace GroupeesDownload.Models
{
    public class DownloadFile
    {
        public string PlatformName { get; set; }
        public string Url { get; set; }

        public override string ToString()
        {
            return PlatformName;
        }
    }
}
