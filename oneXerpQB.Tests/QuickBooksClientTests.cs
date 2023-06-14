using Xunit;
using oneXerpQB;
using System.Collections.Generic;

namespace oneXerpQB.Tests
{
    public class QuickBooksClientTests
    {
        // ...

        [Fact]
        public void CreatePurchaseOrder_FailureScenario_ReturnsFalseAndDoesNotAddPurchaseOrder()
        {
            // Arrange
            var mockQuickBooksClient = new MockQuickBooksClient();
            mockQuickBooksClient.ShouldCreatePurchaseOrderSucceed = false;

            var purchaseOrderData = new PurchaseOrderData
            {
                VendorName = "Test Vendor",
                OrderDate = new System.DateTime(2023, 03, 17),
                Items = new List<PurchaseOrderItem>
                {
                    new PurchaseOrderItem  { ItemName = "Test Item", Quantity = 5, Rate = 10.0 }
                }
            };

            // Act
            bool result = mockQuickBooksClient.CreatePurchaseOrder(purchaseOrderData);

            // Assert
            Assert.False(result, "CreatePurchaseOrder should return false in a failure scenario.");
            Assert.Empty(mockQuickBooksClient.CreatedPurchaseOrders);
        }
    }
}
