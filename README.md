# CLDV7112 Project 2 — Azure Functions Integration

This project extends the ABC Retail Azure Storage solution from Project 1 by adding four Azure Functions that call the same Azure Storage account used by the MVC application.

## Functions

1. **StoreCustomerInTable** — stores customer profile information in the `CustomerProfiles` Azure Table.
2. **WriteToBlobStorage** — writes supplied content to the `productmedia` Blob container.
3. **ProcessQueueTransaction** — writes a transaction message to `orderprocessing` and reads existing queue messages using `PeekMessagesAsync`.
4. **SendFileToAzureFiles** — writes a log file to the `applicationlogs` Azure File share.

## Local configuration

Set the following values in `local.settings.json` using the same Azure Storage connection string used by Project 1:

- `AzureWebJobsStorage`
- `AzureStorage`

Never commit real credentials to GitHub.

## Local test examples

The local Functions host normally exposes endpoints under:

`http://localhost:7071/api/<FunctionName>`

Use POST requests with JSON bodies.

### StoreCustomerInTable
```json
{
  "customerName": "Project 2 Test Customer",
  "email": "project2@example.com",
  "productInterest": "Laptop",
  "orders": 2
}
```

### WriteToBlobStorage
```json
{
  "fileName": "project2-test.txt",
  "content": "ABC Retail Project 2 Blob test",
  "contentType": "text/plain"
}
```

### ProcessQueueTransaction
```json
{
  "message": "Processing Project 2 test order"
}
```

### SendFileToAzureFiles
```json
{
  "fileName": "project2-test.log",
  "content": "ABC Retail Project 2 Azure Files test log"
}
```

## Deployment evidence

For the Project 2 submission, capture:

- Function App overview and deployed functions.
- Code for each function.
- Successful Table response and the resulting Table entity.
- Successful Blob response and the resulting Blob.
- Successful Queue response and the resulting Queue message.
- Successful Azure Files response and the resulting file in the share.
- Deployed MVC application URL and Function App URL.

## Customer experience discussion

### Azure Event Hubs
Azure Event Hubs is a managed event-ingestion service designed for high-throughput streams of events. In ABC Retail, it could collect large volumes of customer activity, such as product views, searches and order events. Applications can publish events to an Event Hub, while downstream consumers process those events independently. This could support near-real-time analytics and personalised experiences without tightly coupling the retail web application to every analytics consumer.

### Azure Service Bus (Event Bus / enterprise messaging)
Azure Service Bus provides reliable enterprise messaging using queues and topics. In ABC Retail, it could separate order processing, notifications, payment workflows and inventory operations into independent services. Producers send messages to queues or publish to topics, while consumers process them independently. Features such as dead-lettering, duplicate detection and scheduled delivery can improve reliability and operational control.

These services are discussed for their potential customer-experience value; they are not required to replace the four implemented Project 2 Functions.
