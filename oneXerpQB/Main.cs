using System;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using QBFC16Lib;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Amazon.Runtime.Internal.Util;
using System.Windows.Forms;

namespace oneXerpQB
{
    class Program
    {
        static async Task Main(string[] args)
        {
            
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Note - Finds the instance role automatically
            var sqsClient = new AmazonSQSClient(Amazon.RegionEndpoint.USEast1);
            // TODO Here we should look up the queue URL from cloudformation output
            var incomingMessageQueueUrl = "https://sqs.us-east-1.amazonaws.com/136559125535/development-InfrastructureStack-ExtensibleFinanceModuleQBDInfraEgre-YBZpLaST854p"; // TODO make this dynamic

            // Read QuickBooks company file path from configuration
            var qbCompanyFilePath = configuration["QuickBooks:CompanyFilePath"];
            var quickBooksClient = new QuickBooksClient(qbCompanyFilePath);

            var oneXerpClient = new OneXerpClient();


            var poller = new BackgroundPoller(sqsClient, oneXerpClient, incomingMessageQueueUrl, quickBooksClient, 20000, 1);
            try
            {
                var cancellationToken = CancellationToken.None;
                await poller.Start(cancellationToken);

                while (true)
                {
                    try
                    {
                        if (File.Exists("stop.txt"))
                        {
                            Logger.Log("Stop signal received. Stopping the background poller...");
                            await poller.Stop();
                            File.Delete("stop.txt"); // remove the signal file
                            break; // stop the loop
                        }
                        await Task.Delay(1000); // delay next check by 1 second
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"An error occurred: {ex.Message}. Restarting the background poller...");
                        await poller.Start(cancellationToken);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Log($"An error occurred while starting the background poller: {ex.Message}");
            }

        }
    }

    public class BackgroundPoller
    {
        private readonly AmazonSQSClient _sqsClient;
        private readonly string _incomingMessageQueueUrl; // This is oneXerp's "egress queue" (egress from oneXerp's perspective - leaving from oneXerp)
        private bool _running;
        private readonly int _pollingInterval;
        private readonly IQuickBooksClient _quickBooksClient;
        private SemaphoreSlim _semaphore;
        private readonly IOneXerpClient _oneXerpClient;
        private CancellationTokenSource _cts;
        private Task _pollingTask;
        public bool IsPollingActive => _pollingTask != null && !_pollingTask.IsCompleted;
        private readonly ILogger _logger;


        public BackgroundPoller(AmazonSQSClient sqsClient, IOneXerpClient oneXerpClient, string incomingMessageQueueUrl, IQuickBooksClient quickBooksClient, int pollingInterval = 20000, int maxConcurrency = 1)
        {
            _sqsClient = sqsClient;
            _incomingMessageQueueUrl = incomingMessageQueueUrl;
            _quickBooksClient = quickBooksClient;
            _running = true;
            _pollingInterval = pollingInterval;
            _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            _oneXerpClient = oneXerpClient;
            _cts = new CancellationTokenSource();
            //_logger = logger;
        }


        public async Task Start(CancellationToken externalToken)
        {
            if (_pollingTask != null && !_pollingTask.IsCompleted)
            {
                // already started
                return;
            }

            _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

            _pollingTask = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        // PollSqsQueue or whatever method you're using for polling.
                        await PollSqsQueue(_cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Task was cancelled
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Log your exception here

                    }
                }
            }, _cts.Token);
        }

        public async Task Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();

                if (_pollingTask != null)
                {
                    try
                    {
                        await _pollingTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Task was cancelled, which is expected. We can ignore this exception.
                    }
                }

                _cts.Dispose();
                _cts = null;
            }
        }


        internal async Task ProcessMessage(Amazon.SQS.Model.Message message)
        {
            var outgoingMessageQueueUrl = "https://sqs.us-east-1.amazonaws.com/136559125535/development-InfrastructureStack-ExtensibleFinanceModuleQBDInfraIngr-vv40Q77IXJ4O"; // TODO make this dynamic
            IResponse response = null;          // The response received from QuickBooks
            EgressMessage egressMessage = null; // The message to send back to oneXerp

            try
            {
                Logger.Log($"Message received from queue.");
                dynamic parsedMessage = ParseMessage(message.Body);
                string oneXerpId = parsedMessage.body.oneXerpId;
                string actionType = ((string)parsedMessage.actionType).ToUpperInvariant();
                PurchaseOrder purchaseOrderData;
                Vendor vendorData;

                switch (actionType)
                {

                    case "CREATE_PO":
                        Logger.Log("Processing CREATE_PO action...");
                        purchaseOrderData = JsonConvert.DeserializeObject<PurchaseOrder>(parsedMessage.body.ToString());
                        response = _quickBooksClient.CreatePurchaseOrder(purchaseOrderData);
                        break;
                    case "CREATE_PO_AND_RECEIVE_PO_IN_FULL":
                        Logger.Log("Processing CREATE_PO_AND_RECEIVE_PO_IN_FULL action...");
                        purchaseOrderData = JsonConvert.DeserializeObject<PurchaseOrder>(parsedMessage.body.ToString());

                        // Create the PO in QuickBooks 
                        response = _quickBooksClient.CreatePurchaseOrder(purchaseOrderData);
                        if (response.StatusCode != 0) break;

                        // Get the details from the response for PO
                        IPurchaseOrderRet poRet = (IPurchaseOrderRet)response.Detail;
                        string poTxnId = poRet.TxnID.ToString();   // This is the id that quickbooks creates

                        response = _quickBooksClient.ReceivePurchaseOrder(poTxnId, purchaseOrderData.VendorName);
                        // BIG TODO
                        // TODO if there is an error here, we created the PO but didn't receive... handle it appropriately
                        if (response.StatusCode != 0) break; 

                        // Get the details from the response for Receipt
                        IItemReceiptRet itemReceiptRet = (IItemReceiptRet)response.Detail;
                        string itemReceiptTxnId = itemReceiptRet.TxnID.GetValue();   // This is the id that quickbooks creates

                        /** 
                         * TODO - See if we need to map the ListIds of the PuchaseOrderLineItems to the purchase order line item Ids from oneXerp.
                         *        In quickbooks the line items don't get their own TxnId, but they do have an ItemRef that points to the ListIds of the corresponding item.
                         *        Therefore the items have to exist in Quickbooks. If so we'll need to add those to the EgressMessage
                         *        
                         *        Might be a good idea at least to return info about items that were created in quickbooks.
                         *        
                         * Walk through the response and create a map of item from oneXerp Ids -> quickbooks Ids
                         * Dictionary<string, string> itemIdsMap = new Dictionary<string, string>();
                         */

                        // Build the egress message with details of what occurred and mapping ids
                        Logger.Log("PO Created and Received successfully. Build message to send to queue.");
                        egressMessage = new EgressMessageCreateAndReceivePOInFull(purchaseOrderData.oneXerpId, poTxnId, itemReceiptTxnId);
                        
                        break;
                    case "RECEIVE_PO":
                        Logger.Log("Processing RECEIVE_PO_IN_FULL action...");
                        Receipt receiptData = JsonConvert.DeserializeObject<Receipt>(parsedMessage.body.ToString());
                        response = _quickBooksClient.ReceivePurchaseOrder(receiptData.QuickbooksPOTxnId, receiptData.VendorName);
                        // TODO egressMessage = null;
                        break;
                    //case "RECEIVE_PO_LINE_ITEMS":
                    // TODO determine the message format for this
                    //Logger.Log("Processing RECEIVE_PO_LINE_ITEMS action... waiting to hear back about this");
                    // TODO Determine what the function should except. It's built, but not optimal really.
                    //purchaseOrderData = (PurchaseOrderData)parsedMessage;
                    //lineItems = GetLineitemsFromPurchaseOrder(purchaseOrderData);
                    //response = _quickBooksClient.ReceivePurchaseOrderLineItems(purchaseOrderData);
                    //break;
                    case "CREATE_VENDOR":
                        Logger.Log("Processing CREATE_VENDOR action...");
                        vendorData = JsonConvert.DeserializeObject<Vendor>(parsedMessage.body.ToString());
                        Logger.Log($"vendorData: {vendorData}");
                        response = _quickBooksClient.CreateVendor(vendorData);
                        break;
                    default:
                        // Handle unrecognized actionType
                        Logger.Log($"Unrecognized actionType: {actionType}");
                        break;
                }

                
                if (response != null && response.StatusCode != 0)
                {
                    try
                    {
                        // Delete the message from the queue after it's processed
                        await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
                        {
                            QueueUrl = _incomingMessageQueueUrl,
                            ReceiptHandle = message.ReceiptHandle
                        });

                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"An error occurred while deleting a message from oneXerp's egress queue that was already processed by quickbooks. Here is the message as it was originally received: {parsedMessage}");
                        throw ex;
                    }

                    try
                    {
                        // Convert the message object to a string using JSON serialization
                        string messageBody = JsonConvert.SerializeObject(egressMessage);
                        await SendMessageAsync(outgoingMessageQueueUrl, messageBody);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log((string)"An error occurred while sending a message to oneXerp's ingress queue. This is the message that was attempted: ".Concat(egressMessage.ToString()));
                        throw ex;
                    }

                }
                else
                {
                    Logger.Log("Message failed to process. The message will not be deleted from the queue.");
                }
            }
            catch (QuickBooksErrorException ex)
            {
                // Handle QuickBooksErrorException here
                Logger.Log("QuickBooks ERROR occurred while processing message: " + ex.Message);
            }
            catch (QuickBooksWarningException ex)
            {
                // Handle QuickBooksWarningException here
                Logger.Log("QuickBooks WARNING occurred while processing message: " + ex.Message);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // Handle QuickBooksWarningException here
                Logger.Log("FileNotFoundException thrown while processing message: " + ex.Message);
            }
            catch (Exception ex)
            {
                // Handle any other exceptions here
                Logger.Log("Error occurred while processing message: " + ex.Message);
            }
        }

        public async Task PollSqsQueue(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _running)
            {
                ReceiveMessageResponse response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _incomingMessageQueueUrl,
                    MaxNumberOfMessages = 1,
                    VisibilityTimeout = 60
                });

                if (response.Messages.Count > 0)
                {
                    // Wait for the semaphore before processing the message
                    await _semaphore.WaitAsync();

                    // Process the message in a separate task so that we can continue polling for new messages
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessMessage(response.Messages[0]);
                        }
                        finally
                        {
                            // Release the semaphore after processing is done
                            _semaphore.Release();
                        }
                    });
                }

                if (token.IsCancellationRequested)
                {
                    _running = false;
                }

                await Task.Delay(_pollingInterval);
            }
        }


        // TODO should probably break out some of the logic in this file.
        // The class Background Poller doesn't really make sense if it's
        // also handling sending messages
        public async Task SendMessageAsync(string queueUrl, string messageBody)
        {
            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = queueUrl, // URL of existing SQS queue
                MessageBody = messageBody
            };

            try
            {
                var sendMessageResponse = await _sqsClient.SendMessageAsync(sendMessageRequest);
                Console.WriteLine("Message sent to the queue successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in sending message to the queue: " + e.Message);
            }
        }
               


        private bool IsValidActionType(string actionType)
        {
            string[] validActionTypes = { "CREATE_VENDOR", "CREATE_PO", "RECEIVE_PO", "CREATE_PO_AND_RECEIVE_PO_IN_FULL" };
            return validActionTypes.Contains(actionType, StringComparer.OrdinalIgnoreCase);
        }
        
        /** 
         * Returns a map of ids from items in oneXerp to their corresponding
         */

        private bool createItemIdsMap(string actionType)
        {
            string[] validActionTypes = { "CREATE_VENDOR", "CREATE_PO", "RECEIVE_PO" };
            return validActionTypes.Contains(actionType, StringComparer.OrdinalIgnoreCase);
        }


        /*
         * Args:
         *     messageBody: JSON string {
         *         oneXerpId: string (PO/Vendor id in oneXerp database)
         *         actionType: string ("CREATE_VENDOR" | "CREATE_PO" | "RECEIVE_PO")
         *     }
         */
        internal dynamic ParseMessage(string messageBody)
        {
            dynamic parsedMessage = JsonConvert.DeserializeObject(messageBody);

            // To pretty print
            string prettyJsonStr = JsonConvert.SerializeObject(parsedMessage, Formatting.Indented);
            Logger.Log($"Message parsed: {prettyJsonStr}");

            string actionType = parsedMessage.actionType;
            if (!IsValidActionType(actionType))
            {
                throw new ArgumentException($"Invalid actionType value from queue. Action type found: {actionType}");
            }

            string oneXerpId = parsedMessage.body.oneXerpId;
            if (string.IsNullOrEmpty(oneXerpId))
            {
                throw new ArgumentException($"Invalid oneXerpId value from queue. Action type found: {actionType}");
            }

            return parsedMessage;
        }

    }
}

    

    

