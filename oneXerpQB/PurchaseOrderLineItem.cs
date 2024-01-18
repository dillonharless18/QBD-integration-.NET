using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace erpQB
{
    public class PurchaseOrderLineItem : ErpObject
    {
        public string ItemName { get; set; }
        public double Quantity { get; set; }
        public double Rate { get; set; }

        public double ReceivedQuantity { get; set; } // Used when receiving partial POs - The amount to add to the line item's "QtyReceived" column.
        public string QuickbooksItemListId { get; set; } // Used when receiving partial POs - The ListId of the actual Item that was created (or retrieved) when the PO was created.
        
    }
}
