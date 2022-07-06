using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TNTWSSample.TNTWebReference;

namespace ups_api_jul1.Models
{
    public class ApiResults
    {
        public List<TimeInTransitResponse> ApiResponse { get; set; }

        public List<CsvRow> ParsedData { get; set; }
    }
}