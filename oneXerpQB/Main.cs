using System;
using System.Collections.Generic;
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
            var sqsUrl = "https://sqs.us-east-1.amazonaws.com/136559125535/TestQBDEgressQueue"; // TODO make this dynamic

            // Read QuickBooks company file path from configuration
            var qbCompanyFilePath = configuration["QuickBooks:CompanyFilePath"];
            var quickBooksClient = new QuickBooksClient(qbCompanyFilePath);

            var oneXerpClient = new OneXerpClient();


            var poller = new BackgroundPoller(sqsClient, oneXerpClient, sqsUrl, quickBooksClient, 20000, 1);
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
        private readonly string _sqsUrl;
        private bool _running;
        private readonly int _pollingInterval;
        private readonly IQuickBooksClient _quickBooksClient;
        private SemaphoreSlim _semaphore;
        private readonly IOneXerpClient _oneXerpClient;
        private CancellationTokenSource _cts;
        private Task _pollingTask;
        public bool IsPollingActive => _pollingTask != null && !_pollingTask.IsCompleted;
        private readonly ILogger _logger;


        public BackgroundPoller(AmazonSQSClient sqsClient, IOneXerpClient oneXerpClient, string sqsUrl, IQuickBooksClient quickBooksClient, int pollingInterval = 20000, int maxConcurrency = 1)
        {
            _sqsClient = sqsClient;
            _sqsUrl = sqsUrl;
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


        internal async Task ProcessMessage(Message message)
        {
            try
            {
                OneXerpQBMessage parsedMessage = ParseMessage(message.Body);
                bool isSuccessful = false;
                string itemId = parsedMessage.itemId;
                string actionType = parsedMessage.actionType.ToUpperInvariant();
                PurchaseOrderData purchaseOrderData;
                VendorData vendorData;

                switch (actionType)
                {
                    
                    case "CREATE_PO":
                        // Perform actions for creating a purchase order
                        Logger.Log("Processing CREATE_PO action...");
                        purchaseOrderData = await _oneXerpClient.getPurchaseOrderData(itemId);
                        isSuccessful = _quickBooksClient.CreatePurchaseOrder(purchaseOrderData);
                        break;
                    case "RECEIVE_PO":
                        // Perform actions for updating a purchase order
                        Logger.Log("Processing UPDATE_PO action... waiting to hear back about this");
                        //purchaseOrderData = await _oneXerpClient.getPurchaseOrderData(itemId);
                        //isSuccessful = _quickBooksClient.UpdatePurchaseOrder(purchaseOrderData);
                        break;
                    case "CREATE_VENDOR":
                        // Perform actions for adding a vendor
                        Logger.Log("Processing CREATE_VENDOR action...");
                        vendorData = await _oneXerpClient.getVendorData(itemId);
                        Logger.Log($"vendorData: {vendorData}");
                        isSuccessful = _quickBooksClient.CreateVendor(vendorData);
                        break;
                    default:
                        // Handle unrecognized actionType
                        Logger.Log($"Unrecognized actionType: {actionType}");
                        break;
                }

                
                if (isSuccessful)
                {
                    // Delete the message from the queue after it's processed
                    await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
                    {
                        QueueUrl = _sqsUrl,
                        ReceiptHandle = message.ReceiptHandle
                    });
                }
                else
                {
                    Logger.Log("Purchase order creation failed. The message will not be deleted from the queue.");
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
                    QueueUrl = _sqsUrl,
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

        private bool IsValidActionType(string actionType)
        {
            string[] validActionTypes = { "CREATE_VENDOR", "CREATE_PO", "RECEIVE_PO" };
            return validActionTypes.Contains(actionType, StringComparer.OrdinalIgnoreCase);
        }


        /*
         * Args:
         *     messageBody: JSON string {
         *         itemId: string (PO/Vendor id in oneXerp database)
         *         actionType: string ("CREATE_VENDOR" | "CREATE_PO" | "RECEIVE_PO")
         *     }
         */
        internal OneXerpQBMessage ParseMessage(string messageBody)
        {
           
            var messageData = JsonConvert.DeserializeObject<OneXerpQBMessage>(messageBody);

            if (!IsValidActionType(messageData.actionType))
            {
                throw new ArgumentException($"Invalid actionType value from queue. Action type found: {messageData.actionType}");
            }

            if (string.IsNullOrEmpty(messageData.itemId))
            {
                throw new ArgumentException($"Invalid itemId value from queue. Action type found: {messageData.actionType}");
            }

            return messageData;
        }
        
    }
}

    

    

