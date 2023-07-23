using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB
{
    /**
     * This class represents the messages that come in
     * from oneXerp. 
     * 
     * The reason it extends OneXerpObject is because
     * As of 7.23.2023 there are two ways to assume a oneXerpId:
     * 
     * actionType: one of the following strings: CREATE_PO, CREATE_PO_AND_RECEIVE_PO_IN_FULL, RECEIEVE_PO, RECEIVE_PO_LINE_ITEMS, CREATE_VENDOR
     * body: The body of the message. It extends OneXerp object, assuming the existence of a oneXerpId. The body may or may not have
     *       further nested objects with their own oneXerpIds, such as LineItems.
     *       
     */
    public class IngressMessage
    {
        public string actionType { get; set; }
        public OneXerpObject body { get; set; } // extends OneXerpObject so it has a oneXerpId
    }
}
