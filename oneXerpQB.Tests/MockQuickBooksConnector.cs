using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB.Tests
{
    public class MockQuickBooksConnector : IQuickBooksConnector
    {
        public List<PurchaseOrderData> CreatedPurchaseOrders { get; } = new List<PurchaseOrderData>();

        // Add this property to control the behavior of CreatePurchaseOrder
        public bool ShouldCreatePurchaseOrderSucceed { get; set; } = true;

        public bool CreatePurchaseOrder(PurchaseOrderData purchaseOrderData)
        {
            // Add the purchase order data to the list if the creation should succeed
            if (ShouldCreatePurchaseOrderSucceed)
            {
                CreatedPurchaseOrders.Add(purchaseOrderData);
            }

            // Return the value of ShouldCreatePurchaseOrderSucceed
            return ShouldCreatePurchaseOrderSucceed;
        }
    }

}
