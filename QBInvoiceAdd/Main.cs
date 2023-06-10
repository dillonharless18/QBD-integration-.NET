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

namespace oneXerpQB
{
    class Program
    {
        static void Main(string[] args)
        {
            
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Create an Amazon SQS client using default AWS credentials and a specific AWS region - finds instance role automatically
            var sqsClient = new AmazonSQSClient(Amazon.RegionEndpoint.USEast1);
            var sqsUrl = "https://sqs.us-east-1.amazonaws.com/your-account-id/your-queue-name"; // TODO make this dynamic

            // Read QuickBooks company file path from configuration
            var qbCompanyFilePath = configuration["QuickBooks:CompanyFilePath"];

            // Start background worker for polling SQS queue
            var quickBooksConnector = new QuickBooksConnector(qbCompanyFilePath);
            var poller = new BackgroundPoller(sqsClient, sqsUrl, quickBooksConnector, 20000);
            try
            {
                poller.Start();

                while (true)
                {
                    try
                    {
                        // Simulates a blocking operation to keep the application running.
                        Console.ReadKey();

                        // Stop the background worker if necessary.
                        poller.Stop();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred: {ex.Message}. Restarting the background poller...");
                        poller.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while starting the background poller: {ex.Message}");
            }

        }
    }

    public class BackgroundPoller
    {
        private readonly AmazonSQSClient _sqsClient;
        private readonly string _sqsUrl;
        private Thread _pollingThread;
        private bool _running;
        private readonly int _pollingInterval;
        private readonly IQuickBooksConnector _quickBooksConnector;
        private SemaphoreSlim _semaphore;
        private readonly OneXerpClient _oneXerpClient;

        public BackgroundPoller(AmazonSQSClient sqsClient, OneXerpClient oneXerpClient, string sqsUrl, IQuickBooksConnector quickBooksConnector, int pollingInterval = 20000, int maxConcurrency = 1)
        {
            _sqsClient = sqsClient;
            _sqsUrl = sqsUrl;
            _quickBooksConnector = quickBooksConnector;
            _running = true;
            _pollingInterval = pollingInterval;
            _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            _oneXerpClient = oneXerpClient;
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
                Vendor vendor;

                switch (actionType)
                {
                    
                    case "CREATE_PO":
                        // Perform actions for creating a purchase order
                        Console.WriteLine("Processing CREATE_PO action...");
                        purchaseOrderData = await _oneXerpClient.getPurchaseOrderData(itemId);
                        isSuccessful = _quickBooksConnector.CreatePurchaseOrder(purchaseOrderData);
                        break;
                    case "UPDATE_PO":
                        // Perform actions for updating a purchase order
                        Console.WriteLine("Processing UPDATE_PO action...");
                        purchaseOrderData = await _oneXerpClient.getPurchaseOrderData(itemId);
                        isSuccessful = _quickBooksConnector.UpdatePurchaseOrder(purchaseOrderData);
                        break;
                    case "CREATE_VENDOR":
                        // Perform actions for adding a vendor
                        Console.WriteLine("Processing CREATE_VENDOR action...");
                        vendor = await _oneXerpClient.getVendorData(itemId);
                        isSuccessful = _quickBooksConnector.CreateVendor(vendorData);
                        break;
                    default:
                        // Handle unrecognized actionType
                        Console.WriteLine($"Unrecognized actionType: {actionType}");
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
                    Console.WriteLine("Purchase order creation failed. The message will not be deleted from the queue.");
                }
            }
            catch (QuickBooksErrorException ex)
            {
                // Handle QuickBooksErrorException here
                Console.WriteLine("QuickBooks ERROR occurred while processing message: " + ex.Message);
            }
            catch (QuickBooksWarningException ex)
            {
                // Handle QuickBooksWarningException here
                Console.WriteLine("QuickBooks WARNING occurred while processing message: " + ex.Message);
            }
            catch (System.IO.FileNotFoundException ex)
            {
                // Handle QuickBooksWarningException here
                Console.WriteLine("FileNotFoundException thrown while processing message: " + ex.Message);
            }
            catch (Exception ex)
            {
                // Handle any other exceptions here
                Console.WriteLine("Error occurred while processing message: " + ex.Message);
            }
        }

        public void Start()
        {
            _pollingThread = new Thread(async () => await PollSqsQueue());
            _pollingThread.Start();
        }

        public void Stop()
        {
            _running = false;
            _pollingThread.Join();
        }

        public async Task PollSqsQueue()
        {
            while (_running)
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

    

    

