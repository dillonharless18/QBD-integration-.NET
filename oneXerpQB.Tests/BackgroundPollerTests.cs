using System;
using Xunit;
using oneXerpQB;
using System.Collections.Generic;
using Amazon.SQS;
using Moq;
using Amazon.SQS.Model;
using Newtonsoft.Json;
using System.Configuration;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace oneXerpQB.Tests
{
    public class BackgroundPollerTests
    {
        [Fact]
        public void ParseMessage_ValidJson_ReturnsPurchaseOrderData()
        {

            // Arrange

            var mockSqsClient = new Mock<AmazonSQSClient>("access-key", "secret-key", Amazon.RegionEndpoint.USEast1);
            var mockQuickBooksConnector = new Mock<IQuickBooksConnector>();
            var backgroundPoller = new BackgroundPoller(mockSqsClient.Object, "sqs-url", mockQuickBooksConnector.Object);

            string jsonMessage = "{\"VendorName\": \"Test Vendor\", \"OrderDate\": \"2023-03-17T00:00:00\", \"Items\": [{ \"ItemName\": \"Test Item\", \"Quantity\": 5, \"Rate\": 10.0 }]}";

            // Act
            var result = backgroundPoller.ParseMessage(jsonMessage);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Vendor", result.VendorName);
            Assert.Equal(new System.DateTime(2023, 03, 17), result.OrderDate);
            Assert.Single(result.Items);
            Assert.Equal("Test Item", result.Items[0].ItemName);
            Assert.Equal(5, result.Items[0].Quantity);
            Assert.Equal(10.0, result.Items[0].Rate);
        }

        [Fact]
        public async void ProcessMessage_ValidMessage_CallsCreatePurchaseOrder()
        {
            // Arrange
            var mockSqsClient = new Mock<AmazonSQSClient>("access-key", "secret-key", Amazon.RegionEndpoint.USEast1);
            var mockQuickBooksConnector = new Mock<IQuickBooksConnector>();
            var backgroundPoller = new BackgroundPoller(mockSqsClient.Object, "sqs-url", mockQuickBooksConnector.Object);

            var message = new Message
            {
                Body = "{\"VendorName\": \"Test Vendor\", \"OrderDate\": \"2023-03-17T00:00:00\", \"Items\": [{ \"ItemName\": \"Test Item\", \"Quantity\": 5, \"Rate\": 10.0 }]}"
            };
            var purchaseOrderData = JsonConvert.DeserializeObject<PurchaseOrderData>(message.Body);

            // Act
            await backgroundPoller.ProcessMessage(message);

            // Assert
            mockQuickBooksConnector.Verify(connector => connector.CreatePurchaseOrder(It.Is<PurchaseOrderData>(data => ArePurchaseOrderDataEqual(data, purchaseOrderData))), Times.Once);
        }

        private bool ArePurchaseOrderDataEqual(PurchaseOrderData data1, PurchaseOrderData data2)
        {
            if (data1.VendorName != data2.VendorName || data1.OrderDate != data2.OrderDate || data1.Items.Count != data2.Items.Count)
            {
                return false;
            }

            for (int i = 0; i < data1.Items.Count; i++)
            {
                if (data1.Items[i].ItemName != data2.Items[i].ItemName || data1.Items[i].Quantity != data2.Items[i].Quantity || data1.Items[i].Rate != data2.Items[i].Rate)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
