using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB
{
    public class Receipt : OneXerpObject
    {
        public string AccountName { get; set; }
        public DateTime ReceiptDate { get; set; }
        public List<ReceiptLineItem> Items { get; set; }
        public string VendorName { get; set; }

        public string QuickbooksPOTxnId { get; set; }

        public string oneXerpPOId { get; set; }

        public Receipt( string accountName, DateTime receiptDate, List<ReceiptLineItem> items, string vendorName )
        {
            AccountName = accountName;
            ReceiptDate = receiptDate;
            Items = items;
            VendorName = vendorName;

        }
    }
}
