using Xunit;
using oneXerpQB;
using System;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace oneXerpQB.Tests
{
    public class QuickBooksConnectorIntegrationTests
    {
        [Fact]
        public void CreatePurchaseOrder_ValidData_CreatesPurchaseOrderInQuickBooks()
        {
            // Arrange
            
            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            var qbCompanyFilePath = configuration["QuickBooks:CompanyFilePath"];

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

            var quickBooksConnector = new QuickBooksClient(qbCompanyFilePath);

            // Act
            bool result = quickBooksConnector.CreatePurchaseOrder(purchaseOrderData);

            // Assert
            Assert.True(result);
        }
    }
}
