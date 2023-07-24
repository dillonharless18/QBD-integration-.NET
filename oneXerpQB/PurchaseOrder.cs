using oneXerpQB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB
{
    public class PurchaseOrder : OneXerpObject
    {
        public string VendorName { get; set; }
        public DateTime OrderDate { get; set; }
        public List<PurchaseOrderLineItem> Items { get; set; }
        public string PurchaseOrderId { get; set; } // Likely not in use as we use the oneXerpId to store the POId from oneXerp.
    }
}
