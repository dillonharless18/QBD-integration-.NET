using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB
{
    public class PurchaseOrderItem
    {
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public double Rate { get; set; }

        public double ReceivedQuantity { get; set; } // Used when receiving partial POs - The amount to add to the line item's "QtyReceived" column.
        public string LineId { get; set; } // Used when receiving partial POs - The TxnLineId of the line item when the PO was created.
        
    }
}
