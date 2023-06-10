using QBFC16Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB
{
    public interface IQuickBooksConnector
    {
        bool CreatePurchaseOrder(PurchaseOrderData purchaseOrderData);

    }

    public class QuickBooksConnector : IQuickBooksConnector
    {
        private string _qbCompanyFilePath;

        public QuickBooksConnector(string qbCompanyFilePath)
        {
            _qbCompanyFilePath = qbCompanyFilePath;
        }

        public bool CreatePurchaseOrder(PurchaseOrderData poData) // TODO update this to return a more robust message other than a bool
        {
            bool result = false;
            QBSessionManager sessionManager = new QBSessionManager();

            try
            {

                if (!System.IO.File.Exists(_qbCompanyFilePath))
                {
                    throw new System.IO.FileNotFoundException($"QuickBooks company file not found at path: {_qbCompanyFilePath}");
                }

                sessionManager.OpenConnection("", "oneXerpQB");
                sessionManager.BeginSession(_qbCompanyFilePath, ENOpenMode.omDontCare);

                // Create a new PurchaseOrder using QBFC
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                IPurchaseOrderAdd purchaseOrderAdd = requestMsgSet.AppendPurchaseOrderAddRq();
                purchaseOrderAdd.VendorRef.FullName.SetValue(poData.VendorName);
                purchaseOrderAdd.TxnDate.SetValue(poData.OrderDate);

                // Add items to the PurchaseOrder
                foreach (var item in poData.Items)
                {
                    IPurchaseOrderLineAdd lineAdd = purchaseOrderAdd.ORPurchaseOrderLineAddList.Append().PurchaseOrderLineAdd;
                    lineAdd.ItemRef.FullName.SetValue(item.ItemName);
                    lineAdd.Quantity.SetValue(item.Quantity);

                    // Set Rate value - TODO look into this more and see if there's a better way
                    lineAdd.Amount.SetValue(item.Rate * item.Quantity); // Not sure if this should be total or the price per
                }

                // Send the request to QuickBooks
                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                IResponse response = responseMsgSet.ResponseList.GetAt(0);

                if (response.StatusCode < 0)
                {
                    Console.WriteLine($"Error adding Purchase Order: {response.StatusMessage}");
                    // Throw a QuickBooksErrorException if the status code indicates an error
                    throw new QuickBooksErrorException(response.StatusCode, "An error occurred while creating the Purchase Order in QuickBooks.");
                }
                else if (response.StatusCode > 0)
                {
                    // Throw a QuickBooksWarningException if the status code indicates a warning
                    throw new QuickBooksWarningException(response.StatusCode, "A warning occurred while creating the Purchase Order in QuickBooks.");
                }
                else
                {
                    Console.WriteLine("Purchase Order added successfully.");
                    Console.WriteLine(responseMsgSet.ResponseList.GetAt(0).ToString());
                    result = true;
                }
            }
            finally
            {
                // End the session and close the connection
                sessionManager.EndSession();
                sessionManager.CloseConnection();
            }

            return result;
        }
    }
}
