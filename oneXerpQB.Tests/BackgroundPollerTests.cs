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
using System.Numerics;

namespace oneXerpQB.Tests
{
    public class BackgroundPollerTests
    {
        [Fact]
        public void ParseMessage_ValidJson_ReturnsPurchaseOrderData()
        {

            // Arrange

            var mockSqsClient = new Mock<AmazonSQSClient>("access-key", "secret-key", Amazon.RegionEndpoint.USEast1);
            var mockOneXErpClient = new Mock<OneXerpClient>();
            var mockQuickBooksClient = new Mock<IQuickBooksClient>();
            var backgroundPoller = new BackgroundPoller(mockSqsClient.Object, mockOneXErpClient.Object, "sqs-url", mockQuickBooksClient.Object);

            string jsonMessage = "{ \"itemId\": \"xxxx-yyyy-zzzz-1111-2222-3333\", \"actionType\": \"CREATE_VENDOR\"}";

            // Act
            var result = backgroundPoller.ParseMessage(jsonMessage);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("xxxx-yyyy-zzzz-1111-2222-3333", result.itemId);
            Assert.Equal("CREATE_VENDOR", result.actionType);
        }

        [Fact]
        public async void ProcessMessage_ValidMessage_CallsCreatePurchaseOrder()
        {
            // Arrange
            var mockSqsClient = new Mock<AmazonSQSClient>("access-key", "secret-key", Amazon.RegionEndpoint.USEast1);
            var mockOneXErpClient = new Mock<OneXerpClient>();
            var mockQuickBooksClient = new Mock<IQuickBooksClient>();
            var backgroundPoller = new BackgroundPoller(mockSqsClient.Object, mockOneXErpClient.Object, "sqs-url", mockQuickBooksClient.Object);

            var message = new Message
            {
                Body = "{ \"itemId\": \"xxxx-yyyy-zzzz-1111-2222-3333\", \"actionType\": \"CREATE_VENDOR\"}"
        };
            var purchaseOrderData = JsonConvert.DeserializeObject<PurchaseOrderData>(message.Body);

            // Act
            await backgroundPoller.ProcessMessage(message);

            // Assert
            mockQuickBooksClient.Verify(connector => connector.CreatePurchaseOrder(It.Is<PurchaseOrderData>(data => ArePurchaseOrderDataEqual(data, purchaseOrderData))), Times.Once);
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
