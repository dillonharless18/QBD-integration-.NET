using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB
{
    /**
     *
     * This clas is the base class for messages sent back to oneXerp's queue
     * 
     * It has two fields that map an id from oneXerp to the one created in quickbooks
     * These fields can be used to represent POs, Vendors, and Receipts, depending on the situation.
     * 
     * Other classes will extend this as needed. 
     * For example: 
     *      When simply creating a PO we need to include a map of the line items' ids from oneXerp to their corresponding LineItemIds that quickbooks created as part of the PO creation process
     *      vs.
     *      When creating and receiving a PO in full, we need to also return the receipt's TxnId that was created by quickbooks so oneXerp can tie it a PO.
     *          Question here: how do we want to handle this? Currently in oneXerp we just have a status that determines if something is receieved or not.
     *      vs. 
     *      When purely receiving line items on a PO, we may need to return line items on a receipt object with details of qty and such, depending on whether oneXerp stored that information prior.
     *                        
     */
    public class EgressMessage
    {
        public string _oneXerpId { get; set; }
        public string _quickbooksId { get; set; }

        public EgressMessage(string oneXerpId, string quickbooksId)
        {
            _oneXerpId = oneXerpId;
            _quickbooksId = quickbooksId;
        }
    }
}
