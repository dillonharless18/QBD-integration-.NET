using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace erpQB
{
    /**
     * This class represents the messages that come in
     * from erp. 
     * 
     * The reason it extends ErpObject is because
     * As of 7.23.2023 there are two ways to assume a erpId:
     * 
     * actionType: one of the following strings: CREATE_PO, CREATE_PO_AND_RECEIVE_PO_IN_FULL, RECEIVE_PO, RECEIVE_PO_LINE_ITEMS, CREATE_VENDOR
     * body: The body of the message. It extends Erp object, assuming the existence of a erpId. The body may or may not have
     *       further nested objects with their own erpIds, such as LineItems.
     *       
     */
    public class IngressMessage<T> where T : ErpObject
    {
        public string actionType { get; set; }
        public T body { get; set; }
    }

}
