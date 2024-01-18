using Moq;
using System.Collections.Generic;
using erpQB;
using QBFC16Lib;

namespace erpQB.Tests
{

    public class MockQuickBooksClient
    {
        public Mock<IQuickBooksClient> CreateMock()
        {
            var mockClient = new Mock<IQuickBooksClient>();

            // Setup AddNewItems to return a list of mocked responses
            mockClient
                .Setup(client => client.AddNewItems(It.IsAny<IQBSessionManager>(), It.IsAny<List<PurchaseOrderLineItem>>()))
                .Returns((IQBSessionManager sessionManager, List<PurchaseOrderLineItem> items) =>
                {
                    var responses = new List<ResponseWrapper>();
                    foreach (var item in items)
                    {
                        var mockResponse = new Mock<IResponse>();
                        mockResponse.Setup(r => r.StatusCode).Returns(0);
                        responses.Add(new ResponseWrapper(mockResponse.Object));
                    }
                    return responses;
                });


            // Setup DoesItemExist to return false
            mockClient
                .Setup(client => client.DoesItemExist(It.IsAny<IQBSessionManager>(), It.IsAny<string>()))
                .Returns(false);

            // Setup CreateVendor to return a mocked response with status code 0
            mockClient
                .Setup(client => client.CreateVendor(It.IsAny<Vendor>()))
                .Returns((Vendor vendorData) =>
                {
                    var mockResponse = new Mock<IResponse>();
                    mockResponse.Setup(r => r.StatusCode).Returns(0);
                    return mockResponse.Object;
                });

            // Setup DeleteVendor to not throw any exceptions
            mockClient
                .Setup(client => client.DeleteVendor(It.IsAny<string>()))
                .Callback(() => { });

            // Setup GetVendorListIdByName to return a string representing the vendor ID
            mockClient
                .Setup(client => client.GetVendorListIdByName(It.IsAny<string>()))
                .Returns((string vendorName) => $"MockedVendorID-{vendorName}");

            mockClient
                .Setup(client => client.CreatePurchaseOrder(It.IsAny<PurchaseOrder>()))
                .Returns((IQBSessionManager sessionManager, PurchaseOrder purchaseOrder) =>
                {
                    var mockResponse = new Mock<IResponse>();
                    mockResponse.Setup(r => r.StatusCode).Returns(0);
                    return mockResponse.Object;
                });





            return mockClient;
        }
    }
}
