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
        IResponse CreatePurchaseOrder(PurchaseOrder purchaseOrderData);
        IResponse CreateVendor(Vendor vendorData);

        IResponse ReceivePurchaseOrder(string purchaseOrderId);

        IResponse ReceivePurchaseOrderLineItems(string purchaseOrderId, List<PurchaseOrderLineItem> lineItems);

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


        ///////////////////////////
        //  BEGIN Purchase Order //
        ///////////////////////////
        
        /**
         * Creates a purchase order.
         */ 
        public IResponse CreatePurchaseOrder(PurchaseOrder poData) // TODO update this to return a more robust message other than a bool
        {

            QBSessionManager sessionManager = new QBSessionManager();
            IResponse response;

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

                // Check and Add New Items if they do not exist
                var newItemResponses = AddNewItems(sessionManager, poData.Items);
                foreach (var newItemResponse in newItemResponses)
                {
                    if (newItemResponse.StatusCode != 0)
                    {
                        Logger.Log($"Error adding Item: {newItemResponse.StatusMessage}");
                    }
                }

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
                response = responseMsgSet.ResponseList.GetAt(0);

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

            return response;
        }


        /** Fully receive a PO. 
         * 
         * In QuickBooks, in order to mark a PO "Fully Received", you create an item receipt and link it 
         * to the PO. All of the items in that PO will be automatically added to the receipt and fully received. 
         The PO then derives its status from the amount of each line item received.*/
        public IResponse ReceivePurchaseOrder(string poId)
        {
            bool result = false;
            QBSessionManager sessionManager = new QBSessionManager();
            IResponse response;

            try
            {

                // Start a session
                sessionManager.OpenConnection("", _qbApplicationName);
                sessionManager.BeginSession("", ENOpenMode.omDontCare);

                // Create a new ItemReceipt using QBFC
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                IItemReceiptAdd itemReceiptAdd = requestMsgSet.AppendItemReceiptAddRq();

                // TODO Figure out if we need to pull in the AccountName and ReceiptDate from oneXerp.
                //itemReceiptAdd.APAccountRef.FullName.SetValue(itemReceiptData.AccountName);
                //itemReceiptAdd.TxnDate.SetValue(itemReceiptData.ReceiptDate);

                // Link the ItemReceipt to the PurchaseOrder
                itemReceiptAdd.LinkToTxnIDList.Add(poId);

                // Send the request to QuickBooks
                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                // Handle the response
                response = responseMsgSet.ResponseList.GetAt(0);
                if (response.StatusCode < 0)
                {
                    Logger.Log($"Error receiving PO: {response.StatusMessage}");
                    // Throw a QuickBooksErrorException if the status code indicates an error
                    throw new QuickBooksErrorException(response.StatusCode, "An error occurred while receiving PO in QuickBooks.");
                }
                else if (response.StatusCode > 0)
                {
                    // Throw a QuickBooksWarningException if the status code indicates a warning
                    throw new QuickBooksWarningException(response.StatusCode, "A warning occurred while receiving PO in QuickBooks.");
                }
                else if (response.Detail == null)
                {
                    // Throw a QuickBooksWarningException if the status code indicates a warning
                    throw new QuickBooksErrorException(response.StatusCode, "A warning occurred while receiving PO in QuickBooks. Quickbooks did not throw an error, but the detail of the response was empty.");
                }
            }
            finally
            {
                // Close the session manager
                if (sessionManager != null)
                {
                    sessionManager.EndSession();
                    sessionManager.CloseConnection();
                }
            }

            return response;
        }

        /**
         * Partially recieve a purchase order by creating a receipt 
         * against a PO, updating the ReceivedQuantity on the line items.
         * 
         * It first retrieves the existing ReceievedQuantity on the list items and adds the appropriate amount
         * from the payload.
         */ 
        public IResponse ReceivePurchaseOrderLineItems(string purchaseOrderId, List<PurchaseOrderLineItem> lineItems)
        {
            QBSessionManager sessionManager = new QBSessionManager();
            IResponse response;

            try
            {
                // Start a session
                sessionManager.OpenConnection("", _qbApplicationName);
                sessionManager.BeginSession("", ENOpenMode.omDontCare);

                // Get the existing received quantities for the line items
                string[] lineItemIds = lineItems.Select(li => li.LineId).ToArray();
                Dictionary<string, double> existingReceivedQuantities = GetReceivedQuantitiesForLineItems(purchaseOrderId, lineItemIds);

                // Create a new ItemReceiptAdd request
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                IItemReceiptAdd itemReceiptAdd = requestMsgSet.AppendItemReceiptAddRq();

                // Link the ItemReceipt to the PurchaseOrder
                itemReceiptAdd.LinkToTxnIDList.Add(purchaseOrderId);

                // Update the line items
                foreach (var lineItem in lineItems)
                {
                    IItemLineAdd itemLineAdd = itemReceiptAdd.ORItemLineAddList.Append().ItemLineAdd;
                    itemLineAdd.LinkToTxn.TxnID.SetValue(purchaseOrderId);
                    itemLineAdd.LinkToTxn.TxnLineID.SetValue(lineItem.LineId);

                    // Determine the new received quantity based on existing and incoming quantities
                    double existingReceivedQuantity = existingReceivedQuantities.ContainsKey(lineItem.LineId) ? existingReceivedQuantities[lineItem.LineId] : 0;
                    double newReceivedQuantity = existingReceivedQuantity + lineItem.ReceivedQuantity;
                    itemLineAdd.Quantity.SetValue(newReceivedQuantity);
                }

                // Send the request to QuickBooks
                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                // Handle the response
                response = responseMsgSet.ResponseList.GetAt(0);
                if (response.StatusCode < 0)
                {
                    Logger.Log($"Error receiving PO LineItems: {response.StatusMessage}");
                    // Throw a QuickBooksErrorException if the status code indicates an error
                    throw new QuickBooksErrorException(response.StatusCode, "An error occurred while receiving PO LineItems in QuickBooks.");
                }
                else if (response.StatusCode > 0)
                {
                    // Throw a QuickBooksWarningException if the status code indicates a warning
                    throw new QuickBooksWarningException(response.StatusCode, "A warning occurred while receiving PO LineItems in QuickBooks.");
                }
                else if (response.Detail == null)
                {
                    // Throw a QuickBooksWarningException if the status code indicates a warning
                    throw new QuickBooksErrorException(response.StatusCode, "A warning occurred while receiving PO LineItems in QuickBooks. Quickbooks did not throw an error, but the detail of the response was empty.");
                }
            }
            finally
            {
                // Close the session manager
                if (sessionManager != null)
                {
                    sessionManager.EndSession();
                    sessionManager.CloseConnection();
                }
            }

            return response;
        }


        /**
         * Takes a PO Id and a list of line item Ids on that PO and returns a dictionary
         * where the keys are the line item Ids and the values are the ReceievedQuantity of each item.
         * Used when receiving items. Get the existing values and then add the received amount appropriately (or
         * maybe subtract/custom reporting in the future).
         */
        private Dictionary<string, double> GetReceivedQuantitiesForLineItems(string purchaseOrderId, string[] lineItemIds)
        {
            Dictionary<string, double> receivedQuantities = new Dictionary<string, double>();
            QBSessionManager sessionManager = new QBSessionManager();

            try
            {
                // Start a session
                sessionManager.OpenConnection("", _qbApplicationName);
                sessionManager.BeginSession("", ENOpenMode.omDontCare);

                // Create a new PurchaseOrderQuery request
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                IPurchaseOrderQuery purchaseOrderQuery = requestMsgSet.AppendPurchaseOrderQueryRq();
                purchaseOrderQuery.ORTxnQuery.TxnIDList.Add(purchaseOrderId);

                // Send the request to QuickBooks
                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                // Handle the response
                IResponse response = responseMsgSet.ResponseList.GetAt(0);
                if (response.StatusCode == 0)
                {
                    IPurchaseOrderRetList purchaseOrderRetList = response.Detail as IPurchaseOrderRetList;
                    if (purchaseOrderRetList != null && purchaseOrderRetList.Count > 0)
                    {
                        IPurchaseOrderRet purchaseOrderRet = purchaseOrderRetList.GetAt(0);
                        if (purchaseOrderRet != null)
                        {
                            IORPurchaseOrderLineRetList purchaseOrderLineRetList = purchaseOrderRet.ORPurchaseOrderLineRetList;
                            if (purchaseOrderLineRetList != null)
                            {
                                for (int i = 0; i < purchaseOrderLineRetList.Count; i++)
                                {
                                    IORPurchaseOrderLineRet purchaseOrderLineRet = purchaseOrderLineRetList.GetAt(i);
                                    string lineItemId = purchaseOrderLineRet.PurchaseOrderLineRet.TxnLineID.GetValue();
                                    if (lineItemIds.Contains(lineItemId))
                                    {
                                        double receivedQuantity = purchaseOrderLineRet.PurchaseOrderLineRet.ReceivedQuantity.GetValue();
                                        receivedQuantities[lineItemId] = receivedQuantity;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Error handling
                    string errorCode = response.StatusCode.ToString();
                    string errorMessage = response.StatusMessage;
                    // Handle the error
                }
            }
            catch (Exception ex)
            {
                // Exception handling
                // Handle the exception
                Logger.Log($"There was an Exception in GetReceivedQuantitiesForLineItems: {ex.Message}");
            }
            finally
            {
                // Close the session manager
                if (sessionManager != null)
                {
                    sessionManager.EndSession();
                    sessionManager.CloseConnection();
                }
            }

            return receivedQuantities;
        }

        public List<IResponse> AddNewItems(IQBSessionManager sessionManager, List<PurchaseOrderLineItem> items)
        {
            List<IResponse> responses = new List<IResponse>();

            foreach (var item in items)
            {
                if (DoesItemExist(sessionManager, item.ItemName))
                {
                    Logger.Log("Item with name " + item.ItemName + " already exists. Skipping the creation of this item");
                    continue;
                }

                Logger.Log("Item with name " + item.ItemName + " does not exist. Creating it now.");

                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                IItemInventoryAdd itemInventoryAddRq = requestMsgSet.AppendItemInventoryAddRq();

                // Set the name of the item
                itemInventoryAddRq.Name.SetValue(item.ItemName);

                // Set the sales description and sales price of the item
                itemInventoryAddRq.SalesDesc.SetValue(item.ItemName);
                itemInventoryAddRq.SalesPrice.SetValue(item.Rate);

                // Optionally, we could also set the asset account for the inventory item.
                // We would need to get the ListID of the account, and use it to set the value
                // For example:
                // itemInventoryAddRq.AssetAccountRef.ListID.SetValue("8000003D-1556629351");

                // Set the purchase cost of the item
                itemInventoryAddRq.PurchaseCost.SetValue(item.Rate);

                // Make sure the item is active
                itemInventoryAddRq.IsActive.SetValue(true);

                // Perform the request and capture the response
                var response = sessionManager.DoRequests(requestMsgSet).ResponseList.GetAt(0);
                responses.Add(response);
            }

            return responses;
        }


        public bool DoesItemExist(IQBSessionManager sessionManager, string itemName)
        {
            IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 13, 0);
            IItemQuery itemQueryRq = requestMsgSet.AppendItemQueryRq();
            itemQueryRq.ORListQuery.FullNameList.Add(itemName);

            IMsgSetResponse responseSet = sessionManager.DoRequests(requestMsgSet);
            IResponse response = responseSet.ResponseList.GetAt(0);

            // If the returned count is greater than zero, the item exists
            return response.retCount > 0;
        }





        /////////////////////////
        //  END Purchase Order //
        /////////////////////////


        ////////////////////
        //  BEGIN Vendor  //
        ////////////////////

        public IResponse CreateVendor(Vendor vendorData)
        {
            IResponse response;
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
                response = responseMsgSet.ResponseList.GetAt(0);

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
                else if (response.Detail == null)
                {
                    // Throw a QuickBooksWarningException if the status code indicates a warning
                    throw new QuickBooksErrorException(response.StatusCode, "A warning occurred while creating the Vendor in QuickBooks. Quickbooks did not throw an error, but the detail of the response was empty.");
                }
                else
                {
                    Logger.Log("Vendor added successfully.");
                    Logger.Log(responseMsgSet.ResponseList.GetAt(0).ToString());
                }

            }
            finally
            {
                // End the session and close the connection
                sessionManager.EndSession();
                sessionManager.CloseConnection();
            }

            return response;
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

        //////////////////
        //  END Vendor  //
        //////////////////

    }
}
