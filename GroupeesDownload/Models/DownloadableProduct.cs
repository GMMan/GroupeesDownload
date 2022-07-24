using System;
using System.Collections.Generic;
using System.Text;

namespace GroupeesDownload.Models
{
    class DownloadableProduct
    {
        public string Name { get; set; }
        public List<DownloadFile> Files { get; set; } = new List<DownloadFile>();

        public override string ToString()
        {
            return Name ?? "Game/Music";
        }
    }
}
