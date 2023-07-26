using Xunit;
using Moq;
using Amazon.SQS.Model;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using System.Collections.Generic;
using System;
using System.Text.Json;

namespace oneXerpQB.Tests
{
    public class BackgroundPollerTests
    {
        private readonly Mock<AmazonSQSClient> _mockSqsClient;
        private readonly Mock<IOneXerpClient> _mockOneXerpClient;
        private readonly Mock<IQuickBooksClient> _mockQuickBooksClient;

        private readonly BackgroundPoller _backgroundPoller;

        public BackgroundPollerTests()
        {
            // Arrange
            _mockSqsClient = new Mock<AmazonSQSClient>();
            
            // Setup the ReceiveMessageAsync function to return a response with no messages.
            _mockSqsClient
                .Setup(client => client.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ReceiveMessageResponse { Messages = new List<Message>() });



            _mockOneXerpClient = new Mock<IOneXerpClient>();
            _mockQuickBooksClient = new Mock<IQuickBooksClient>();

            string sqsUrl = "https://sqs.us-east-1.amazonaws.com/your-account-id/your-queue-name";
            int pollingInterval = 20000;
            int maxConcurrency = 1;

            _backgroundPoller = new BackgroundPoller(
                _mockSqsClient.Object,
                _mockOneXerpClient.Object,
                sqsUrl,
                _mockQuickBooksClient.Object,
                pollingInterval,
                maxConcurrency
            );
        }


        [Fact]
        public async Task Start_WhenCalled_StartsPollingTask()
        {
            // Arrange
            CancellationTokenSource cts = new CancellationTokenSource();

            // Act
            await _backgroundPoller.Start(cts.Token);

            // Assert
            Assert.True(_backgroundPoller.IsPollingActive);

            // Cleanup: ensure the task is stopped after the test
            cts.Cancel();
            await _backgroundPoller.Stop();
        }



        [Fact]
        public async Task Stop_WhenCalled_StopsPollingTask()
        {
            // Arrange
            CancellationTokenSource cts = new CancellationTokenSource();

            // Act
            await _backgroundPoller.Start(cts.Token);

            // Cleanup: ensure the task is stopped after the test
            cts.Cancel();
            await _backgroundPoller.Stop();

            // Assert
            Assert.False(_backgroundPoller.IsPollingActive);

            
        }

        // TODO: Determine if we need this. Commented out for now since I removed calls to the get data functions since we are sending the entire message to the queue.
        //[Fact]
        //public async Task ProcessMessage_WhenCalledWithValidMessage_CallsCorrectClientMethods()
        //{
        //    // Arrange
        //    VendorData vendorData = new VendorData(
        //    "SampleName",
        //    "SampleCompanyName",
        //    new Address(
        //        "123 Main St",
        //        "Apt 4B",
        //        "",
        //        "",
        //        "",
        //        "SampleCity",
        //        "SampleState",
        //        "12345",
        //        "SampleCountry",
        //        "SampleNote"),
        //        "123-456-7890"
        //    );

        //    string json = JsonSerializer.Serialize(vendorData);

        //    Message message = new Message()
        //    {
        //        Body = json
        //    };

        //    PurchaseOrderData expectedData = new PurchaseOrderData();
        //    _mockOneXerpClient
        //        .Setup(c => c.getPurchaseOrderData(It.IsAny<string>()))
        //        .ReturnsAsync(expectedData);
        //    _mockQuickBooksClient
        //        .Setup(c => c.CreatePurchaseOrder(It.IsAny<PurchaseOrderData>()))
        //        .Returns(true);

        //    // Act
        //    await _backgroundPoller.ProcessMessage(message);

        //    // Assert
        //    _mockOneXerpClient.Verify(c => c.getPurchaseOrderData("testItemId"), Times.Once());
        //    _mockQuickBooksClient.Verify(c => c.CreatePurchaseOrder(expectedData), Times.Once());
        //    _mockSqsClient.Verify(c => c.DeleteMessageAsync(It.IsAny<DeleteMessageRequest>(), It.IsAny<CancellationToken>()), Times.Once());
        //}

        // Add more tests here for other branches of the ProcessMessage method, 
        // such as different action types and exceptional cases
    }
}
