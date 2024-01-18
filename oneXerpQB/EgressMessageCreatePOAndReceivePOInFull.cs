using erpQB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace erpQB
{
    public class EgressMessageCreateAndReceivePOInFull : EgressMessage
    {
        string quickbooksReceiptId { get; set; }
        Dictionary<string, string> poLineItems { get; set; } // Used to map POLineItem erp Ids to the POLineItem ListIds created by quickbooks

        // Using this while we wait to figure out if we need to match the poLineItems to the ItemList ListIds in quickbooks for the response message to erp
        public EgressMessageCreateAndReceivePOInFull(string erpId, string quickbooksId, string quickbooksReceiptId)
            : base(erpId, quickbooksId)
        {
            this.quickbooksReceiptId = quickbooksReceiptId; // Likely no receipt was created prior to this action in erp, so erp needs to create one and link it to this id.
        }

        public EgressMessageCreateAndReceivePOInFull(string erpId, string quickbooksId, string quickbooksReceiptId, Dictionary<string, string> poLineItems)
            : base(erpId, quickbooksId)
        {
            this.quickbooksReceiptId = quickbooksReceiptId; // Likely no receipt was created prior to this action in erp, so erp needs to create one and link it to this id.
            this.poLineItems = poLineItems;
        }
        
    }

}
