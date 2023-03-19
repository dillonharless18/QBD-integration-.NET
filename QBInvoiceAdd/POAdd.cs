using System;
using System.Collections.Generic;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using QBFC16Lib;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace oneXerpQB
{
    class Program
    {
        static void Main(string[] args)
        {
            // Set up AWS credentials and SQS client - TODO ensure we're using the instance profile instead of creds
            var sqsClient = new AmazonSQSClient("your-access-key", "your-secret-key", Amazon.RegionEndpoint.USEast1);
            var sqsUrl = "https://sqs.us-east-1.amazonaws.com/your-account-id/your-queue-name"; // TODO make this dynamic

            // Start background worker for polling SQS queue
            var quickBooksConnector = new QuickBooksConnector();
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

        public BackgroundPoller(AmazonSQSClient sqsClient, string sqsUrl, IQuickBooksConnector quickBooksConnector, int pollingInterval = 20000, int maxConcurrency = 1)
        {
            _sqsClient = sqsClient;
            _sqsUrl = sqsUrl;
            _quickBooksConnector = quickBooksConnector;
            _running = true;
            _pollingInterval = pollingInterval;
            _semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        }

        internal async Task ProcessMessage(Message message)
        {
            PurchaseOrderData purchaseOrderData = ParseMessage(message.Body);
            bool isSuccessful = _quickBooksConnector.CreatePurchaseOrder(purchaseOrderData);

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
                    MaxNumberOfMessages = 1
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
                            ProcessMessage(response.Messages[0]);
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

        internal PurchaseOrderData ParseMessage(string messageBody)
        {
            // You can use a JSON or XML serializer to parse the messageBody, depending on the message format
            // For simplicity, we'll assume the messageBody contains JSON
            var poData = JsonConvert.DeserializeObject<PurchaseOrderData>(messageBody);
            return poData;
        }
    }

    public class PurchaseOrderData
    {
        public string VendorName { get; set; }
        public DateTime OrderDate { get; set; }
        public List<PurchaseOrderItem> Items { get; set; }
    }

    public class PurchaseOrderItem
    {
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public double Rate { get; set; }
    }

}

    

    

