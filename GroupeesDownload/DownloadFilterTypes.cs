using System;
using System.Collections.Generic;
using System.Text;

namespace GroupeesDownload
{
    [Flags]
    public enum DownloadFilterTypes
    {
        None = 0,
        Games = 1 << 0,
        Music = 1 << 1,
        Others = 1 << 2,
        All = Games | Music | Others
    }
}
