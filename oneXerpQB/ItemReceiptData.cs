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

        public ItemReceiptData()
        {
            Items = new List<ItemReceiptLineData>();
        }
    }
}
