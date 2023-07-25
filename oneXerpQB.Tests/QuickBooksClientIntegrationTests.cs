using Xunit;
using oneXerpQB;
using System;
using Microsoft.Extensions.Configuration;
using System.IO;
using QBFC16Lib;
using Xunit.Abstractions;
using System.Linq;

namespace oneXerpQB.Tests
{
    public class QuickBooksClientIntegrationTests
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

            var purchaseOrderData = new PurchaseOrder
            {
                VendorName = "401K Administrator",
                OrderDate = DateTime.Now,
                Items = new System.Collections.Generic.List<PurchaseOrderLineItem>
                {
                    new PurchaseOrderLineItem
                    {
                        ItemName = "Nonexistent Item",
                        Quantity = 5,
                        Rate = 10.0
                    }
                },
                oneXerpId = $"TstCreate"
            };

            var quickBooksClient = new QuickBooksClient(qbCompanyFilePath);

            // Act
            IResponse result = quickBooksClient.CreatePurchaseOrder(purchaseOrderData);

            // Assert
            Assert.True(result.StatusCode == 0);
        }

        [Fact]
        public void CreatePurchaseOrder_ReceivePurchaseOrder_ValidData_CreatesAndReceivesPurchaseOrderInQuickBooks()
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
                        ItemName = "Nonexistent Item",
                        Quantity = 5,
                        Rate = 10.0
                    }
                },
                oneXerpId = "TstCreRec"
            };

            var quickBooksClient = new QuickBooksClient(qbCompanyFilePath);

            // Act
            IResponse poResponse = quickBooksClient.CreatePurchaseOrder(purchaseOrderData);

            // Assert
            Assert.True(poResponse.StatusCode == 0);

            // Get the details from the response for PO
            IPurchaseOrderRet poRet = (IPurchaseOrderRet)poResponse.Detail;
            string poTxnId = poRet.TxnID.GetValue();   // This is the id that quickbooks creates

            IResponse receiptResponse = quickBooksClient.ReceivePurchaseOrder(poTxnId, purchaseOrderData.VendorName);

            Assert.True(receiptResponse.StatusCode == 0);
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

            string vendorName = $"Test Vendor - {GenerateRandomString(8)}";

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
                vendorName,
                vendorName,
                vendorAddress,
                "123-456-7890"
            );

            var quickBooksClient = new QuickBooksClient(qbCompanyFilePath);

            // Act
            IResponse result = quickBooksClient.CreateVendor(vendorData);

            // Assert
            Assert.True(result.StatusCode == 0);
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
            var listId = quickBooksClient.GetVendorListIdByName("401K Administrator");

            // Assert
            Assert.NotNull(listId);
        }

        


        public string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

}

}
