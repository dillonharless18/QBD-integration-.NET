using Xunit;
using oneXerpQB;
using System;

namespace oneXerpQB.Tests
{
    public class QuickBooksConnectorIntegrationTests
    {
        [Fact]
        public void CreatePurchaseOrder_ValidData_CreatesPurchaseOrderInQuickBooks()
        {
            // Arrange
            var purchaseOrderData = new PurchaseOrderData
            {
                VendorName = "401K Administrator",
                OrderDate = DateTime.Now,
                Items = new System.Collections.Generic.List<PurchaseOrderItem>
                {
                    new PurchaseOrderItem
                    {
                        ItemName = "Installation Labor",
                        Quantity = 5,
                        Rate = 10.0
                    }
                }
            };

            var quickBooksConnector = new QuickBooksConnector();

            // Act
            bool result = quickBooksConnector.CreatePurchaseOrder(purchaseOrderData);

            // Assert
            Assert.True(result);
        }
    }
}
