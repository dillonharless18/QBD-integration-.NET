using oneXerpQB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB
{

    public interface IOneXerpClient
    {
        Task<PurchaseOrderData> getPurchaseOrderData(string itemId);
        Task<VendorData> getVendorData(string itemId);

    }

    public class OneXerpClient : IOneXerpClient
    {
        // TODO: Determine if we need this since we decided to send the entire payload to the queue.
        public async Task<PurchaseOrderData> getPurchaseOrderData(string itemId)
        {

            // Return dummy data for now
            return await Task.FromResult(new PurchaseOrderData
            {
                VendorName = "Vendor 1",
                OrderDate = DateTime.Now,
                Items = new List<PurchaseOrderItem>
                    {
                        new PurchaseOrderItem { ItemName = "Item 1", Quantity = 10, Rate = 100.0 },
                        new PurchaseOrderItem { ItemName = "Item 2", Quantity = 5, Rate = 50.0 }
                    }
            });
        }

        // TODO: Determine if we need this since we decided to send the entire payload to the queue.
        public async Task<VendorData> getVendorData(string itemId)
        {

            // Return dummy data for now
            return await Task.FromResult(new VendorData(
                "Test Vendor",
                "Test Vendor",
                new Address(
                    "123 Street",      // Addr1
                    "PO Box 1234",     // Addr2
                    "",                // Addr3
                    "",                // Addr4
                    "",                // Addr5
                    "Test City",       // City
                    "Test State",      // State
                    "12345",           // PostalCode
                    "US",              // Country
                    "Test notes"       // Note
                ),
                "9101234567"           // Phone
            ));
        }

    }
}
