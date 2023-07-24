using Xunit;
using oneXerpQB;
using System;
using Microsoft.Extensions.Configuration;
using System.IO;
using QBFC16Lib;


namespace oneXerpQB.Tests
{
    public class QuickBooksClientIntegrationTests
    {

        private readonly string _vendorName = "Test Vendor";

        [Fact]
        public void CreatePurchaseOrder_ValidData_CreatesPurchaseOrderInQuickBooks()
        {
            // Arrange
            
            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            var qbCompanyFilePath = configuration["QuickBooks:CompanyFilePath"];

            var purchaseOrderData = new PurchaseOrder
            {
                VendorName = "401K Administrator",
                OrderDate = DateTime.Now,
                Items = new System.Collections.Generic.List<PurchaseOrderLineItem>
                {
                    new PurchaseOrderLineItem
                    {
                        ItemName = "Installation Labor",
                        Quantity = 5,
                        Rate = 10.0
                    }
                }
            };

            var quickBooksClient = new QuickBooksClient(qbCompanyFilePath);

            // Act
            IResponse result = quickBooksClient.CreatePurchaseOrder(purchaseOrderData);

            // Assert
            Assert.True(result.StatusCode == 0);
        }


        [Fact]
        public void CreateVendor_ValidData_CreatesVendorInQuickBooks()
        {
            // Arrange
            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            var qbCompanyFilePath = configuration["QuickBooks:CompanyFilePath"];

            var vendorAddress = new Address(
                    "123 Vendor St.",
                    "",
                    "",
                    "",
                    "",
                    "Test Vendor City",
                    "NC",
                    "12345",
                    "Vendor Country",
                    "Test Notes"
                )
            {
                // ... any additional fields your Address might have ...
            };

            var vendorData = new Vendor(
                _vendorName,
                _vendorName,
                vendorAddress,
                "123-456-7890"
            );

            var quickBooksClient = new QuickBooksClient(qbCompanyFilePath);

            // Act
            bool result = quickBooksClient.CreateVendor(vendorData);

            // Assert
            Assert.True(result);
        }

        // TODO Add a test for Delete Vendor here

        [Fact]
        public void GetVendorListId_ValidData_GetsVendor()
        {
            // Arrange
            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            var qbCompanyFilePath = configuration["QuickBooks:CompanyFilePath"];

            var quickBooksClient = new QuickBooksClient(qbCompanyFilePath);

            // Act
            var listId = quickBooksClient.GetVendorListIdByName(_vendorName);

            // Assert
            Assert.NotNull(listId);
        }
    }

}
