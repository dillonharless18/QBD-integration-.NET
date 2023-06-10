using oneXerpQB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB
{
    public class PurchaseOrderData
    {
        public string VendorName { get; set; }
        public DateTime OrderDate { get; set; }
        public List<PurchaseOrderItem> Items { get; set; }
        public string PurchaseOrderId { get; set; }
    }
}
