using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB
{
    public class ItemReceiptData
    {
        public string AccountName { get; set; }
        public DateTime ReceiptDate { get; set; }
        public List<ItemReceiptLineData> Items { get; set; }

        public ItemReceiptData( string accountName, DateTime receiptDate, List<ItemReceiptLineData> items )
        {
            AccountName = accountName;
            ReceiptDate = receiptDate;
            Items = items;

        }
    }
}
