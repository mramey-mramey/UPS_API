using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ups_api_jul1.Models
{
    public class CsvRow
    {
        public string OriginZip { get; set; }

        public string DestinationZip { get; set; }

        public string PickupDate { get; set; }

        public string PickupTime { get; set; }

        public int Weight { get; set; }

        public int TotalPackages { get; set; }

        public string ErrorMsg { get; set; }
    }
}