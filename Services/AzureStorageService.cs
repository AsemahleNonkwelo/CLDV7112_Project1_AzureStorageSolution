using System.Text;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using CLDV7112_Project1_AzureStorageSolution.Models;

namespace CLDV7112_Project1_AzureStorageSolution.Services;

public class AzureStorageService
{
    private readonly IConfiguration _configuration;

    public AzureStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string ConnectionString =>
        _configuration.GetConnectionString("AzureStorage")
        ?? throw new InvalidOperationException("Azure Storage connection string is not configured.");

    private string TableName => _configuration["AzureStorage:TableName"] ?? "CustomerProfiles";
    private string BlobContainerName => _configuration["AzureStorage:BlobContainerName"] ?? "productmedia";
    private string QueueName => _configuration["AzureStorage:QueueName"] ?? "orderprocessing";
    private string FileShareName => _configuration["AzureStorage:FileShareName"] ?? "applicationlogs";

    private TableClient GetTableClient() =>
        new(ConnectionString, TableName);

    private BlobContainerClient GetBlobContainer() =>
        new BlobServiceClient(ConnectionString).GetBlobContainerClient(BlobContainerName);

    private QueueClient GetQueueClient() =>
        new QueueClient(ConnectionString, QueueName);

    private ShareClient GetShareClient() =>
        new ShareClient(ConnectionString, FileShareName);

    public async Task EnsureResourcesAsync()
    {
        await GetTableClient().CreateIfNotExistsAsync();
        await GetBlobContainer().CreateIfNotExistsAsync();
        await GetQueueClient().CreateIfNotExistsAsync();
        await GetShareClient().CreateIfNotExistsAsync();
    }

    public async Task<List<CustomerProfile>> GetCustomersAsync()
    {
        await EnsureResourcesAsync();
        var results = new List<CustomerProfile>();

        await foreach (var entity in GetTableClient().QueryAsync<CustomerProfile>())
            results.Add(entity);

        return results.OrderBy(x => x.CustomerName).ToList();
    }

    public async Task AddCustomerAsync(CustomerProfile customer)
    {
        await EnsureResourcesAsync();
        customer.PartitionKey = "Customers";
        customer.RowKey = string.IsNullOrWhiteSpace(customer.RowKey)
            ? Guid.NewGuid().ToString("N")
            : customer.RowKey;

        await GetTableClient().AddEntityAsync(customer);
    }

    public async Task<CustomerProfile?> GetCustomerAsync(string rowKey)
    {
        await EnsureResourcesAsync();
        try
        {
            var response = await GetTableClient().GetEntityAsync<CustomerProfile>("Customers", rowKey);
            return response.Value;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task DeleteCustomerAsync(string rowKey)
    {
        await EnsureResourcesAsync();
        await GetTableClient().DeleteEntityAsync("Customers", rowKey);
    }

    public async Task<List<BlobItemViewModel>> GetBlobsAsync()
    {
        await EnsureResourcesAsync();
        var results = new List<BlobItemViewModel>();

        await foreach (BlobItem blob in GetBlobContainer().GetBlobsAsync())
        {
            results.Add(new BlobItemViewModel
            {
                Name = blob.Name,
                Size = blob.Properties.ContentLength,
                LastModified = blob.Properties.LastModified,
                ContentType = blob.Properties.ContentType ?? "application/octet-stream"
            });
        }

        return results.OrderBy(x => x.Name).ToList();
    }

    public async Task UploadBlobAsync(Stream content, string fileName, string contentType)
    {
        await EnsureResourcesAsync();
        var safeName = Path.GetFileName(fileName);
        var client = GetBlobContainer().GetBlobClient(safeName);

        await client.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });
    }

    public async Task<(Stream Stream, string ContentType, string FileName)?> DownloadBlobAsync(string name)
    {
        await EnsureResourcesAsync();
        var client = GetBlobContainer().GetBlobClient(name);

        if (!await client.ExistsAsync())
            return null;

        var response = await client.DownloadStreamingAsync();
        return (response.Value.Content, response.Value.Details.ContentType ?? "application/octet-stream", Path.GetFileName(name));
    }

    public async Task DeleteBlobAsync(string name)
    {
        await EnsureResourcesAsync();
        await GetBlobContainer().DeleteBlobIfExistsAsync(name);
    }

    public async Task SendQueueMessageAsync(string message)
    {
        await EnsureResourcesAsync();
        await GetQueueClient().SendMessageAsync(message);
    }

    public async Task<List<QueueMessageViewModel>> PeekQueueAsync(int maxMessages = 32)
    {
        await EnsureResourcesAsync();
        var results = new List<QueueMessageViewModel>();

        var response = await GetQueueClient().PeekMessagesAsync(Math.Min(maxMessages, 32));

        foreach (var message in response.Value)
        {
            results.Add(new QueueMessageViewModel
            {
                MessageId = message.MessageId,
                Body = message.MessageText,
                InsertedOn = message.InsertedOn,
                ExpiresOn = message.ExpiresOn
            });
        }

        return results;
    }

    public async Task<List<FileItemViewModel>> GetFilesAsync()
    {
        await EnsureResourcesAsync();
        var directory = GetShareClient().GetRootDirectoryClient();
        var results = new List<FileItemViewModel>();

        await foreach (var item in directory.GetFilesAndDirectoriesAsync())
        {
            if (item.IsDirectory)
                continue;

            var properties = await directory.GetFileClient(item.Name).GetPropertiesAsync();

            results.Add(new FileItemViewModel
            {
                Name = item.Name,
                Size = properties.Value.ContentLength,
                LastModified = properties.Value.LastModified
            });
        }

        return results.OrderBy(x => x.Name).ToList();
    }

    public async Task UploadFileAsync(Stream content, string fileName)
    {
        await EnsureResourcesAsync();
        var directory = GetShareClient().GetRootDirectoryClient();
        var client = directory.GetFileClient(Path.GetFileName(fileName));

        await client.CreateAsync(content.Length);
        await client.UploadAsync(content);
    }

    public async Task<(Stream Stream, string FileName)?> DownloadFileAsync(string name)
    {
        await EnsureResourcesAsync();
        var client = GetShareClient().GetRootDirectoryClient().GetFileClient(name);

        if (!await client.ExistsAsync())
            return null;

        var response = await client.DownloadAsync();
        return (response.Value.Content, Path.GetFileName(name));
    }

    public async Task SeedDemoDataAsync()
    {
        await EnsureResourcesAsync();

        // Five Table Storage records.
        var existingCustomers = await GetCustomersAsync();
        var demoCustomers = new[]
        {
            ("Lerato Mokoena", "lerato@example.com", "Laptop", 3),
            ("Thabo Ndlovu", "thabo@example.com", "Smartphone", 5),
            ("Amahle Dlamini", "amahle@example.com", "Headphones", 2),
            ("Sipho Khumalo", "sipho@example.com", "Monitor", 4),
            ("Naledi Molefe", "naledi@example.com", "Keyboard", 1)
        };

        foreach (var item in demoCustomers)
        {
            if (!existingCustomers.Any(x => x.Email.Equals(item.Item2, StringComparison.OrdinalIgnoreCase)))
            {
                await AddCustomerAsync(new CustomerProfile
                {
                    CustomerName = item.Item1,
                    Email = item.Item2,
                    ProductInterest = item.Item3,
                    Orders = item.Item4
                });
            }
        }

        // Five Blob Storage demo files. SVG is valid multimedia/image content.
        var container = GetBlobContainer();
        var demoBlobs = new[]
        {
            ("product-laptop.svg", "#2563eb", "Laptop"),
            ("product-phone.svg", "#7c3aed", "Smartphone"),
            ("product-headphones.svg", "#db2777", "Headphones"),
            ("product-monitor.svg", "#059669", "Monitor"),
            ("product-keyboard.svg", "#ea580c", "Keyboard")
        };

        foreach (var (fileName, color, label) in demoBlobs)
        {
            var client = container.GetBlobClient(fileName);
            if (!await client.ExistsAsync())
            {
                var svg = $"""
                <svg xmlns="http://www.w3.org/2000/svg" width="900" height="500" viewBox="0 0 900 500">
                  <rect width="900" height="500" rx="30" fill="{color}"/>
                  <circle cx="450" cy="190" r="95" fill="white" opacity="0.95"/>
                  <rect x="385" y="125" width="130" height="130" rx="20" fill="{color}" opacity="0.85"/>
                  <text x="450" y="355" text-anchor="middle" font-family="Arial" font-size="52" fill="white">{label}</text>
                  <text x="450" y="410" text-anchor="middle" font-family="Arial" font-size="24" fill="white" opacity="0.9">ABC Retail Product Media</text>
                </svg>
                """;
                await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svg));
                await UploadBlobAsync(stream, fileName, "image/svg+xml");
            }
        }

        // Five Queue Storage messages.
        var existingMessages = await PeekQueueAsync();
        var messages = new[]
        {
            "Processing order #1001 - Laptop - Quantity 1",
            "Processing order #1002 - Smartphone - Quantity 2",
            "Processing order #1003 - Headphones - Quantity 1",
            "Inventory update - Monitor - Stock received: 10",
            "Inventory update - Keyboard - Stock received: 25"
        };

        foreach (var message in messages)
        {
            if (!existingMessages.Any(x => x.Body.Equals(message, StringComparison.OrdinalIgnoreCase)))
                await SendQueueMessageAsync(message);
        }

        // Five Azure File Storage log files.
        var existingFiles = await GetFilesAsync();
        var logNames = new[]
        {
            "order-processing.log",
            "inventory-update.log",
            "customer-activity.log",
            "blob-upload.log",
            "system-health.log"
        };

        foreach (var name in logNames)
        {
            if (!existingFiles.Any(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                var text = $"""
                ABC Retail Azure Storage Solution
                Log file: {name}
                Created: {DateTime.UtcNow:O}
                Status: Successful
                Service: Azure Files
                """;
                await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
                await UploadFileAsync(stream, name);
            }
        }
    }

    public async Task<StorageDashboardViewModel> GetDashboardAsync()
    {
        try
        {
            await EnsureResourcesAsync();
            var customers = await GetCustomersAsync();
            var blobs = await GetBlobsAsync();
            var queues = await PeekQueueAsync();
            var files = await GetFilesAsync();

            return new StorageDashboardViewModel
            {
                Connected = true,
                TableCount = customers.Count,
                BlobCount = blobs.Count,
                QueueCount = queues.Count,
                FileCount = files.Count
            };
        }
        catch (Exception ex)
        {
            return new StorageDashboardViewModel
            {
                Connected = false,
                Error = ex.Message
            };
        }
    }
}
