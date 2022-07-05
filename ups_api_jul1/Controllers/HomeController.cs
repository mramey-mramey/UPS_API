using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TNTWSSample.TNTWebReference;
using ups_api_jul1.Models;

namespace ups_api_jul1.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(HttpPostedFileBase postedFile)
        {           
            if (postedFile == null)
            {
                return View();
            }

            else
            {
                return View(ProcessCsv(postedFile));
            }            
        }
            
        private List<TimeInTransitResponse> ProcessCsv(HttpPostedFileBase postedFile)
        {
            List<CsvRow> data = new List<CsvRow>();

            string filePath = string.Empty;
            string path = Server.MapPath("~/Uploads/");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            filePath = path + Path.GetFileName(postedFile.FileName);
            string extension = Path.GetExtension(postedFile.FileName);
            postedFile.SaveAs(filePath);

            //Read the contents of CSV file to string list delimited by line
            List<string> csvData = System.IO.File.ReadAllText(filePath).Split(new[] { "\r\n" }, StringSplitOptions.None).ToList();
            csvData.RemoveAt(0); //remove header row

            //parse each line 
            List<string> temp;
            foreach (string line in csvData)
            {
                if (string.IsNullOrEmpty(line) || line == ",,,,,")
                    continue;

                temp = line.Split(',').ToList();
                CsvRow newRow = new CsvRow();

                try
                {
                    //first 3 columns are required
                    newRow.OriginZip = int.Parse(temp[0]);
                    newRow.DestinationZip = int.Parse(temp[1]);
                    newRow.PickupDate = temp[2];

                    //last 3 columns are optional
                    newRow.PickupTime = string.IsNullOrEmpty(temp[3]) ? "18:00:00" : temp[3];
                    newRow.Weight = string.IsNullOrEmpty(temp[4]) ? 10 : int.Parse(temp[4]);
                    newRow.TotalPackages = string.IsNullOrEmpty(temp[5]) ? 1 : int.Parse(temp[5]);
                }

                catch (Exception)
                {
                    newRow.ErrorMsg = "There was an error parsing this line. Please make sure all fields were entered correctly.";
                }

                data.Add(newRow);
            }

            return CalcTimeInTransit(data);
        }

        private List<TimeInTransitResponse> CalcTimeInTransit(List<CsvRow> data)
        {
            List<TimeInTransitResponse> results = new List<TimeInTransitResponse>();

            foreach (CsvRow row in data)
            {               
                if (!string.IsNullOrEmpty(row.ErrorMsg))
                {
                    continue;
                }

                try
                {
                    TimeInTransitService tntService = new TimeInTransitService();
                    TimeInTransitRequest tntRequest = new TimeInTransitRequest();
                    RequestType request = new RequestType();
                    String[] requestOption = { "D" };
                    request.RequestOption = requestOption;
                    tntRequest.Request = request;

                    RequestShipFromType shipFrom = new RequestShipFromType();
                    RequestShipFromAddressType addressFrom = new RequestShipFromAddressType();
                    addressFrom.CountryCode = "US";
                    addressFrom.PostalCode = row.OriginZip.ToString();
                    shipFrom.Address = addressFrom;
                    tntRequest.ShipFrom = shipFrom;

                    RequestShipToType shipTo = new RequestShipToType();
                    RequestShipToAddressType addressTo = new RequestShipToAddressType();
                    addressTo.CountryCode = "US";
                    addressTo.PostalCode = row.DestinationZip.ToString();
                    shipTo.Address = addressTo;
                    tntRequest.ShipTo = shipTo;

                    PickupType pickup = new PickupType();
                    pickup.Date = row.PickupDate;
                    pickup.Time = row.PickupTime;
                    tntRequest.Pickup = pickup;

                    ShipmentWeightType shipmentWeight = new ShipmentWeightType();
                    shipmentWeight.Weight = row.Weight.ToString();
                    CodeDescriptionType unitOfMeasurement = new CodeDescriptionType();
                    unitOfMeasurement.Code = "KGS";
                    unitOfMeasurement.Description = "Kilograms";
                    shipmentWeight.UnitOfMeasurement = unitOfMeasurement;
                    tntRequest.ShipmentWeight = shipmentWeight;

                    tntRequest.TotalPackagesInShipment = "1";
                    InvoiceLineTotalType invoiceLineTotal = new InvoiceLineTotalType();
                    invoiceLineTotal.CurrencyCode = "CAD";
                    invoiceLineTotal.MonetaryValue = "10";
                    tntRequest.InvoiceLineTotal = invoiceLineTotal;
                    tntRequest.MaximumListSize = "1";

                    UPSSecurity upss = new UPSSecurity();
                    UPSSecurityServiceAccessToken upsSvcToken = new UPSSecurityServiceAccessToken();
                    upsSvcToken.AccessLicenseNumber = ConfigurationManager.AppSettings["AccessLicenseNumber"];
                    upss.ServiceAccessToken = upsSvcToken;
                    UPSSecurityUsernameToken upsSecUsrnameToken = new UPSSecurityUsernameToken();
                    upsSecUsrnameToken.Username = ConfigurationManager.AppSettings["ApiUserName"];
                    upsSecUsrnameToken.Password = ConfigurationManager.AppSettings["ApiPassword"];
                    upss.UsernameToken = upsSecUsrnameToken;
                    tntService.UPSSecurityValue = upss;

                    System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls | System.Net.SecurityProtocolType.Tls11; //This line will ensure the latest security protocol for consuming the web service call.
                    TimeInTransitResponse tntResponse = tntService.ProcessTimeInTransit(tntRequest);


                    results.Add(tntResponse);
                }

                catch(Exception)
                {

                }
            }

            return results;
        }
    }
}