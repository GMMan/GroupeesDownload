using System;
using System.Collections.Generic;
using System.Text;

namespace GroupeesDownload.Models
{
    public class Track
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsFavorite { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
