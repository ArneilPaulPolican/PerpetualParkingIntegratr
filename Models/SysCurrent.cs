using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerpetualIntegrator.Models
{
    class SysCurrent
    {
        public String POSConnectionString { get; set; }
        public String ParkingConnectionString { get; set; }
        public String DefaultItemCode { get; set; }
        public String DefaultCustomerCode { get; set; }
        public Int64 LastExportChargeIdNumber { get; set; }
        public Boolean UseLastExportChargeIdNumber { get; set; }
    }
}
