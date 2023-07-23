using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB.Tests
{
    public class MockQuickBooksClient : IQuickBooksClient
    {
        public List<PurchaseOrder> CreatedPurchaseOrders { get; } = new List<PurchaseOrder>();
        public List<Vendor> CreatedVendors { get; } = new List<Vendor>();

        // Add this property to control the behavior of CreatePurchaseOrder
        public bool ShouldCreatePurchaseOrderSucceed { get; set; } = true;
        public bool ShouldCreateVendorSucceed { get; set; } = true;

        public bool CreatePurchaseOrder(PurchaseOrder purchaseOrderData)
        {
            // Add the purchase order data to the list if the creation should succeed
            if (ShouldCreatePurchaseOrderSucceed)
            {
                CreatedPurchaseOrders.Add(purchaseOrderData);
            }

            // Return the value of ShouldCreatePurchaseOrderSucceed
            return ShouldCreatePurchaseOrderSucceed;
        }

        public bool ReceivePurchaseOrder(string purchaseOrderId)
        {
            return ShouldCreatePurchaseOrderSucceed;
        }

        public bool ReceivePurchaseOrderLineItems(string purchaseOrderId, List<PurchaseOrderLineItem> lineItems)
        {
            return ShouldCreatePurchaseOrderSucceed;
        }

        public bool CreateVendor(Vendor vendorData)
        {
            // Add the purchase order data to the list if the creation should succeed
            if (ShouldCreateVendorSucceed)
            {
                CreatedVendors.Add(vendorData);
            }

            // Return the value of ShouldCreatePurchaseOrderSucceed
            return ShouldCreateVendorSucceed;
        }

    }

}
