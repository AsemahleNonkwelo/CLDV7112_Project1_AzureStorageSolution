# CLDV7112 Project 1 — Azure Storage Solution

## ABC Retail

This is an ASP.NET Core MVC web application created for **CLDV7112 Cloud Development B — Project 1**.

The application demonstrates:

1. Azure Table Storage — customer profiles and product-related information
2. Azure Blob Storage — product images/multimedia
3. Azure Queue Storage — order processing and inventory messages
4. Azure File Storage — application log files
5. Azure App Service — deployment target

## Requirements

- .NET 8 SDK
- Visual Studio 2022 with ASP.NET and web development workload
- An Azure Storage Account
- An Azure App Service

## Configure Azure Storage

### Local Visual Studio

Open `appsettings.json` and set:

```json
"ConnectionStrings": {
  "AzureStorage": "YOUR_AZURE_STORAGE_CONNECTION_STRING"
}
```

Do not commit real connection strings to GitHub.

### Azure App Service

After publishing, add an App Service application setting:

- Name: `ConnectionStrings__AzureStorage`
- Value: your Azure Storage connection string

The double underscore maps to the nested ASP.NET Core configuration key.

## Run

1. Open `CLDV7112_Project1_AzureStorageSolution.csproj` in Visual Studio.
2. Restore NuGet packages.
3. Build the solution.
4. Run the project.
5. Open the Dashboard.
6. Confirm that the status says **Azure Storage Connected**.
7. Click **Seed Demo Data**.

The seed operation creates at least five examples for each required storage service.

## Evidence for the rubric

After seeding, capture screenshots of:

- Dashboard showing the four storage service counts.
- Azure Table page showing at least 5 customer records.
- Azure Portal Storage Browser showing at least 5 Table entities.
- Blob page showing at least 5 product images.
- Azure Portal Storage Browser showing at least 5 blobs.
- Queue page showing at least 5 messages.
- Azure Portal Storage Browser showing at least 5 queue messages.
- Files page showing at least 5 log files.
- Azure Portal Storage Browser showing at least 5 files.
- Azure App Service overview/deployment.
- Deployed web application in a browser.
- GitHub repository.



## Project 2 extension

The solution also contains `CLDV7112_Project2_AzureFunctions`, an Azure Functions .NET 8 isolated-worker project that integrates four Functions with the same Azure Storage resources used by this MVC application. See the Function project README for test payloads and deployment evidence.
