using System.Net;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CLDV7112_Project2_AzureFunctions;

public class StorageFunctions
{
    private readonly ILogger<StorageFunctions> _logger;
    private readonly string _connectionString;
    private const string TableName = "CustomerProfiles";
    private const string BlobContainerName = "productmedia";
    private const string QueueName = "orderprocessing";
    private const string FileShareName = "applicationlogs";

    public StorageFunctions(ILogger<StorageFunctions> logger)
    {
        _logger = logger;
        _connectionString = Environment.GetEnvironmentVariable("AzureStorage")
            ?? throw new InvalidOperationException("AzureStorage application setting is not configured.");
    }

    // Function 1: store customer information in Azure Table Storage.
    [Function("StoreCustomerInTable")]
    public async Task<HttpResponseData> StoreCustomerInTable(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        try
        {
            var request = await ReadJsonAsync<CustomerRequest>(req);
            if (request is null || string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.Email))
                return await JsonResponse(req, HttpStatusCode.BadRequest, new { success = false, message = "CustomerName and Email are required." });

            var table = new TableClient(_connectionString, TableName);
            await table.CreateIfNotExistsAsync();

            var entity = new TableEntity("Customers", Guid.NewGuid().ToString("N"))
            {
                ["CustomerName"] = request.CustomerName.Trim(),
                ["Email"] = request.Email.Trim(),
                ["ProductInterest"] = request.ProductInterest?.Trim() ?? "",
                ["Orders"] = request.Orders,
                ["CreatedDate"] = DateTime.UtcNow.ToString("O")
            };

            await table.AddEntityAsync(entity);
            _logger.LogInformation("Customer {CustomerName} stored in Azure Table Storage.", request.CustomerName);

            return await JsonResponse(req, HttpStatusCode.OK, new
            {
                success = true,
                function = "StoreCustomerInTable",
                message = "Customer successfully stored in Azure Table Storage.",
                table = TableName,
                partitionKey = entity.PartitionKey,
                rowKey = entity.RowKey,
                customer = request
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Table Storage function failed.");
            return await JsonResponse(req, HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
        }
    }

    // Function 2: write text/image content to Azure Blob Storage.
    [Function("WriteToBlobStorage")]
    public async Task<HttpResponseData> WriteToBlobStorage(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        try
        {
            var request = await ReadJsonAsync<BlobRequest>(req);
            if (request is null || string.IsNullOrWhiteSpace(request.FileName) || request.Content is null)
                return await JsonResponse(req, HttpStatusCode.BadRequest, new { success = false, message = "FileName and Content are required." });

            var container = new BlobServiceClient(_connectionString).GetBlobContainerClient(BlobContainerName);
            await container.CreateIfNotExistsAsync();

            var safeName = Path.GetFileName(request.FileName);
            var blob = container.GetBlobClient(safeName);
            await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(request.Content));
            await blob.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = string.IsNullOrWhiteSpace(request.ContentType) ? "text/plain" : request.ContentType
                }
            });

            _logger.LogInformation("Blob {BlobName} written to Azure Blob Storage.", safeName);

            return await JsonResponse(req, HttpStatusCode.OK, new
            {
                success = true,
                function = "WriteToBlobStorage",
                message = "Content successfully written to Azure Blob Storage.",
                container = BlobContainerName,
                blob = safeName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blob Storage function failed.");
            return await JsonResponse(req, HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
        }
    }

    // Function 3: write a transaction message and read/peek messages from Azure Queue Storage.
    [Function("ProcessQueueTransaction")]
    public async Task<HttpResponseData> ProcessQueueTransaction(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        try
        {
            var request = await ReadJsonAsync<QueueRequest>(req);
            if (request is null || string.IsNullOrWhiteSpace(request.Message))
                return await JsonResponse(req, HttpStatusCode.BadRequest, new { success = false, message = "Message is required." });

            var queue = new QueueClient(_connectionString, QueueName);
            await queue.CreateIfNotExistsAsync();
            await queue.SendMessageAsync(request.Message.Trim());

            var peek = await queue.PeekMessagesAsync(32);
            var messages = peek.Value.Select(x => new
            {
                x.MessageId,
                message = x.MessageText,
                x.InsertedOn
            }).ToList();

            _logger.LogInformation("Queue transaction written and {Count} messages read from the queue.", messages.Count);

            return await JsonResponse(req, HttpStatusCode.OK, new
            {
                success = true,
                function = "ProcessQueueTransaction",
                message = "Queue transaction written successfully and existing messages read using PeekMessages.",
                queue = QueueName,
                writtenMessage = request.Message.Trim(),
                messagesRead = messages.Count,
                messages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Queue Storage function failed.");
            return await JsonResponse(req, HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
        }
    }

    // Function 4: send a log file to Azure Files.
    [Function("SendFileToAzureFiles")]
    public async Task<HttpResponseData> SendFileToAzureFiles(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        try
        {
            var request = await ReadJsonAsync<FileRequest>(req);
            if (request is null || string.IsNullOrWhiteSpace(request.FileName) || request.Content is null)
                return await JsonResponse(req, HttpStatusCode.BadRequest, new { success = false, message = "FileName and Content are required." });

            var share = new ShareClient(_connectionString, FileShareName);
            await share.CreateIfNotExistsAsync();
            var directory = share.GetRootDirectoryClient();
            var safeName = Path.GetFileName(request.FileName);
            var file = directory.GetFileClient(safeName);
            var bytes = Encoding.UTF8.GetBytes(request.Content);

            await file.CreateAsync(bytes.Length);
            await using var stream = new MemoryStream(bytes);
            await file.UploadAsync(stream);

            _logger.LogInformation("File {FileName} written to Azure Files.", safeName);

            return await JsonResponse(req, HttpStatusCode.OK, new
            {
                success = true,
                function = "SendFileToAzureFiles",
                message = "File successfully written to Azure Files.",
                share = FileShareName,
                file = safeName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Files function failed.");
            return await JsonResponse(req, HttpStatusCode.InternalServerError, new { success = false, message = ex.Message });
        }
    }

    private static async Task<T?> ReadJsonAsync<T>(HttpRequestData req)
    {
        return await JsonSerializer.DeserializeAsync<T>(req.Body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    private static async Task<HttpResponseData> JsonResponse(HttpRequestData req, HttpStatusCode statusCode, object value)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(value);
        return response;
    }

    public sealed class CustomerRequest
    {
        public string? CustomerName { get; set; }
        public string? Email { get; set; }
        public string? ProductInterest { get; set; }
        public int Orders { get; set; }
    }

    public sealed class BlobRequest
    {
        public string? FileName { get; set; }
        public string? Content { get; set; }
        public string? ContentType { get; set; }
    }

    public sealed class QueueRequest
    {
        public string? Message { get; set; }
    }

    public sealed class FileRequest
    {
        public string? FileName { get; set; }
        public string? Content { get; set; }
    }
}
