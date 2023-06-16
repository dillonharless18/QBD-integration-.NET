using QBFC16Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace oneXerpQB
{
    public interface IQuickBooksClient
    {
        bool CreatePurchaseOrder(PurchaseOrderData purchaseOrderData);
        bool CreateVendor(VendorData vendorData);

        //bool CreateItemReceipt(string poId);

    }

    public class QuickBooksClient : IQuickBooksClient
    {
        private string _qbCompanyFilePath;
        private string _qbApplicationName;

        public QuickBooksClient(string qbCompanyFilePath)
        {
            _qbCompanyFilePath = qbCompanyFilePath;
            _qbApplicationName = "oneXerpQB";
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

                sessionManager.OpenConnection("", _qbApplicationName);
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
                    Logger.Log($"Error adding Purchase Order: {response.StatusMessage}");
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
                    Logger.Log("Purchase Order added successfully.");
                    Logger.Log(responseMsgSet.ResponseList.GetAt(0).ToString());
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

        public bool CreateVendor(VendorData vendorData)
        {
            bool result = false;
            QBSessionManager sessionManager = new QBSessionManager();

            try
            {
                if (!System.IO.File.Exists(_qbCompanyFilePath))
                {
                    throw new System.IO.FileNotFoundException($"QuickBooks company file not found at path: {_qbCompanyFilePath}");
                }

                // Start the session
                sessionManager.OpenConnection("", "oneXerpQB");
                sessionManager.BeginSession(_qbCompanyFilePath, ENOpenMode.omDontCare);

                // Create the message set request
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);

                // Add requests to the message set request - add a VendorAddRequest to the set
                IVendorAdd addVendorRequest = requestMsgSet.AppendVendorAddRq();
                addVendorRequest.Name.SetValue(vendorData.Name);
                addVendorRequest.CompanyName.SetValue(vendorData.CompanyName);
                addVendorRequest.Phone.SetValue(vendorData.Phone);
                addVendorRequest.VendorAddress.Addr1.SetValue(vendorData.VendorAddress._addr1);
                addVendorRequest.VendorAddress.Addr2.SetValue(vendorData.VendorAddress._addr2);
                //addVendorRequest.VendorAddress.Addr3.SetValue(vendorData.VendorAddress._addr3);
                //addVendorRequest.VendorAddress.Addr4.SetValue(vendorData.VendorAddress._addr4);
                //addVendorRequest.VendorAddress.Addr5.SetValue(vendorData.VendorAddress._addr5);
                addVendorRequest.VendorAddress.City.SetValue(vendorData.VendorAddress._city);
                addVendorRequest.VendorAddress.State.SetValue(vendorData.VendorAddress._state);
                addVendorRequest.VendorAddress.Country.SetValue(vendorData.VendorAddress._country);
                addVendorRequest.VendorAddress.Note.SetValue(vendorData.VendorAddress._note);
                addVendorRequest.VendorAddress.PostalCode.SetValue(vendorData.VendorAddress._postalCode);

                // Send the request to QuickBooks
                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);
                IResponse response = responseMsgSet.ResponseList.GetAt(0);

                if (response.StatusCode < 0)
                {
                    Logger.Log($"Error adding Vendor: {response.StatusMessage}");
                    // Throw a QuickBooksErrorException if the status code indicates an error
                    throw new QuickBooksErrorException(response.StatusCode, "An error occurred while creating the Vendor in QuickBooks.");
                }
                else if (response.StatusCode > 0)
                {
                    // Throw a QuickBooksWarningException if the status code indicates a warning
                    throw new QuickBooksWarningException(response.StatusCode, "A warning occurred while creating the Vendor in QuickBooks.");
                }
                else
                {
                    Logger.Log("Vendor added successfully.");
                    Logger.Log(responseMsgSet.ResponseList.GetAt(0).ToString());
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

        // NOTE: You can only delete List Type items when quickbooks is opened in single user mode
        public void DeleteVendor(string vendorId) 
        {
            // TODO - determine if we should accept name and do this by name
            bool sessionBegun = false;
            bool connectionOpen = false;
            QBSessionManager sessionManager = null;

            try
            {
                // Create the session Manager object
                sessionManager = new QBSessionManager();

                // Create the message set request object to hold our request
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                // Build the Vendor Delete Request
                BuildVendorDeleteRq(requestMsgSet, vendorId);

                // Connect to QuickBooks and begin a session
                sessionManager.OpenConnection("", _qbApplicationName);
                connectionOpen = true;
                sessionManager.BeginSession("", ENOpenMode.omDontCare);
                sessionBegun = true;

                // Send the request and get the response from QuickBooks
                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                // End the session and close the connection to QuickBooks
                sessionManager.EndSession();
                sessionBegun = false;
                sessionManager.CloseConnection();
                connectionOpen = false;

                // Process the response
                WalkVendorDeleteRs(responseMsgSet);
            }
            catch (Exception e)
            {
                Logger.Log(e.Message);
                if (sessionBegun)
                {
                    sessionManager.EndSession();
                }
                if (connectionOpen)
                {
                    sessionManager.CloseConnection();
                }
            }
        }

        void BuildVendorDeleteRq(IMsgSetRequest requestMsgSet, string vendorId)
        {
            IListDel vendorDeleteRq = requestMsgSet.AppendListDelRq();
            // Set field value for ListDelType to Vendor
            vendorDeleteRq.ListDelType.SetValue(ENListDelType.ldtVendor);
            // Set field value for ListID to the Vendor ID to delete
            vendorDeleteRq.ListID.SetValue(vendorId);
        }

        void WalkVendorDeleteRs(IMsgSetResponse responseMsgSet)
        {
            if (responseMsgSet == null) return;
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null) return;

            // If we sent only one request, there is only one response, we'll walk the list for this sample
            for (int i = 0; i < responseList.Count; i++)
            {
                IResponse response = responseList.GetAt(i);
                // Check the status code of the response, 0=ok, >0 is warning
                if (response.StatusCode >= 0)
                {
                    // The request-specific response is in the details, make sure we have some
                    if (response.Detail != null)
                    {
                        // Make sure the response is the type we're expecting
                        ENResponseType responseType = (ENResponseType)response.Type.GetValue();
                        if (responseType == ENResponseType.rtListDelRs)
                        {
                            // Upcast to more specific type here, this is safe because we checked with response.Type check above
                            IQBENListDelTypeType vendorDeleteType = (IQBENListDelTypeType)response.Detail;
                            WalkVendorDeleteType(vendorDeleteType);
                        }
                    }
                }
            }
        }

        void WalkVendorDeleteType(IQBENListDelTypeType vendorDeleteType)
        {
            if (vendorDeleteType == null) return;
            // Go through all the elements of IQBENListDelTypeType
            // In this case, there may not be much to do as the vendor has been deleted
        }

        public string GetVendorListIdByName(string vendorName) // Things like Vendors are of the type List in Quickbooks.
        {
            // Create the session manager
            QBSessionManager sessionManager = new QBSessionManager();

            // Start a session
            sessionManager.OpenConnection("", _qbApplicationName);
            sessionManager.BeginSession("", ENOpenMode.omDontCare);

            // Create the message set request
            IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 13, 0);

            // Create the vendor query
            IVendorQuery vendorQuery = requestMsgSet.AppendVendorQueryRq();
            vendorQuery.ORVendorListQuery.FullNameList.Add(vendorName);

            // Send the request and get the response
            IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

            // End the session
            sessionManager.EndSession();
            sessionManager.CloseConnection();

            // Handle the response
            IResponse response = responseMsgSet.ResponseList.GetAt(0);
            IVendorRetList vendorRetList = (IVendorRetList)response.Detail;

            if (vendorRetList == null || vendorRetList.Count == 0)
            {
                // No vendor found with the given name
                return null;
            }
            else
            {
                // Return the ListID of the first vendor found
                return vendorRetList.GetAt(0).ListID.GetValue();
            }
        }


        // TODO - After hearing from Eric about whether we can just recieve the entire PO or close it, finish implementing this
        //public bool CreateItemReceipt(ItemReceiptData itemReceiptData, string poId)
        //{
        //    bool result = false;
        //    QBSessionManager sessionManager = new QBSessionManager();

        //    try
        //    {
        //        if (!System.IO.File.Exists(_qbCompanyFilePath))
        //        {
        //            throw new System.IO.FileNotFoundException($"QuickBooks company file not found at path: {_qbCompanyFilePath}");
        //        }

        //        sessionManager.OpenConnection("", _qbApplicationName);
        //        sessionManager.BeginSession(_qbCompanyFilePath, ENOpenMode.omDontCare);

        //        // Create a new ItemReceipt using QBFC
        //        IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
        //        IItemReceiptAdd itemReceiptAdd = requestMsgSet.AppendItemReceiptAddRq();
        //        itemReceiptAdd.APAccountRef.FullName.SetValue(itemReceiptData.AccountName);
        //        itemReceiptAdd.TxnDate.SetValue(itemReceiptData.ReceiptDate);

        //        // Link the ItemReceipt to the PurchaseOrder
        //        itemReceiptAdd.LinkToTxnID.SetValue(poId);

        //        // Add items to the ItemReceipt
        //        foreach (var item in itemReceiptData.Items)
        //        {
        //            IItemReceiptLineAdd lineAdd = itemReceiptAdd.ORItemLineAddList.Append().ItemLineAdd;
        //            lineAdd.ItemRef.FullName.SetValue(item.ItemName);
        //            lineAdd.Quantity.SetValue(item.Quantity);
        //            lineAdd.Amount.SetValue(item.Amount);
        //        }

        //        // Send the request to QuickBooks
        //        IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);
        //        IResponse response = responseMsgSet.ResponseList.GetAt(0);

        //        if (response.StatusCode < 0)
        //        {
        //            Logger.Log($"Error adding Item Receipt: {response.StatusMessage}");
        //            throw new QuickBooksErrorException(response.StatusCode, "An error occurred while creating the Item Receipt in QuickBooks.");
        //        }
        //        else if (response.StatusCode > 0)
        //        {
        //            throw new QuickBooksWarningException(response.StatusCode, "A warning occurred while creating the Item Receipt in QuickBooks.");
        //        }
        //        else
        //        {
        //            Logger.Log("Item Receipt added successfully.");
        //            result = true;
        //        }
        //    }
        //    finally
        //    {
        //        sessionManager.EndSession();
        //        sessionManager.CloseConnection();
        //    }

        //    return result;
        //}
    }
}
