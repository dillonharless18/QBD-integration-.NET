using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB.Tests
{
    public class MockQuickBooksClient : IQuickBooksClient
    {
        public List<PurchaseOrderData> CreatedPurchaseOrders { get; } = new List<PurchaseOrderData>();
        public List<VendorData> CreatedVendors { get; } = new List<VendorData>();

        // Add this property to control the behavior of CreatePurchaseOrder
        public bool ShouldCreatePurchaseOrderSucceed { get; set; } = true;
        public bool ShouldCreateVendorSucceed { get; set; } = true;

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

        public bool ReceivePurchaseOrder(string purchaseOrderId)
        {
            return ShouldCreatePurchaseOrderSucceed;
        }

        public bool ReceivePurchaseOrderLineItems(string purchaseOrderId, List<PurchaseOrderItem> lineItems)
        {
            return ShouldCreatePurchaseOrderSucceed;
        }

        public bool CreateVendor(VendorData vendorData)
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
