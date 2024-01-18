using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace erpQB
{
    public class Receipt : ErpObject
    {
        public string AccountName { get; set; }
        public DateTime ReceiptDate { get; set; }
        public List<ReceiptLineItem> Items { get; set; }
        public string VendorName { get; set; }

        public string QuickbooksPOTxnId { get; set; }

        public string erpPOId { get; set; }

        public Receipt( string accountName, DateTime receiptDate, List<ReceiptLineItem> items, string vendorName )
        {
            AccountName = accountName;
            ReceiptDate = receiptDate;
            Items = items;
            VendorName = vendorName;

        }
    }
}
