using oneXerpQB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB
{
    public class EgressMessageCreateAndReceivePOInFull : EgressMessage
    {
        string quickbooksReceiptId { get; set; }
        Dictionary<string, string> poLineItems { get; set; } // Used to map POLineItem oneXerp Ids to the POLineItem ListIds created by quickbooks

        // Using this while we wait to figure out if we need to match the poLineItems to the ItemList ListIds in quickbooks for the response message to oneXerp
        public EgressMessageCreateAndReceivePOInFull(string oneXerpId, string quickbooksId, string quickbooksReceiptId)
            : base(oneXerpId, quickbooksId)
        {
            this.quickbooksReceiptId = quickbooksReceiptId; // Likely no receipt was created prior to this action in oneXerp, so oneXerp needs to create one and link it to this id.
        }

        public EgressMessageCreateAndReceivePOInFull(string oneXerpId, string quickbooksId, string quickbooksReceiptId, Dictionary<string, string> poLineItems)
            : base(oneXerpId, quickbooksId)
        {
            this.quickbooksReceiptId = quickbooksReceiptId; // Likely no receipt was created prior to this action in oneXerp, so oneXerp needs to create one and link it to this id.
            this.poLineItems = poLineItems;
        }
        
    }

}
