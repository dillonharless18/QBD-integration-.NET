using oneXerpQB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB
{
    public class Transaction
    {
        public string TransactionId { get; set; }
        public Dictionary<string, Receipt> Receipts { get; set; }

        public Transaction()
        {
            Receipts = new Dictionary<string, Receipt>();
        }
    }

}
