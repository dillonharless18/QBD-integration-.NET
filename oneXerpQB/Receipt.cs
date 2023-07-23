using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB
{
    public class Receipt
    {
        public string AccountName { get; set; }
        public DateTime ReceiptDate { get; set; }
        public List<ReceiptLineItem> Items { get; set; }

        public Receipt( string accountName, DateTime receiptDate, List<ReceiptLineItem> items )
        {
            AccountName = accountName;
            ReceiptDate = receiptDate;
            Items = items;

        }
    }
}
