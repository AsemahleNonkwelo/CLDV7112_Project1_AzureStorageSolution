namespace CLDV7112_Project1_AzureStorageSolution.Models;

public class QueueMessageViewModel
{
    public string MessageId { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTimeOffset? InsertedOn { get; set; }
    public DateTimeOffset? ExpiresOn { get; set; }
}

public class BlobItemViewModel
{
    public string Name { get; set; } = "";
    public long? Size { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public string ContentType { get; set; } = "";
}

public class FileItemViewModel
{
    public string Name { get; set; } = "";
    public long? Size { get; set; }
    public DateTimeOffset? LastModified { get; set; }
}

public class StorageDashboardViewModel
{
    public int TableCount { get; set; }
    public int BlobCount { get; set; }
    public int QueueCount { get; set; }
    public int FileCount { get; set; }
    public bool Connected { get; set; }
    public string? Error { get; set; }
}
