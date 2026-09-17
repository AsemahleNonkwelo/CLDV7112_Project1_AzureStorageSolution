using Azure;
using Azure.Data.Tables;

namespace CLDV7112_Project1_AzureStorageSolution.Models;

public class CustomerProfile : ITableEntity
{
    public string PartitionKey { get; set; } = "Customers";
    public string RowKey { get; set; } = Guid.NewGuid().ToString("N");
    public string CustomerName { get; set; } = "";
    public string Email { get; set; } = "";
    public string ProductInterest { get; set; } = "";
    public int Orders { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
