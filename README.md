# Drone Delivery Serverless

This project contains a reference implementation for two serverless application architectures.

**Serverless web application**

![](https://learn.microsoft.com/azure/architecture/reference-architectures/serverless/_images/serverless-web-app.png)

The application serves static content from Azure Blob Storage, and implements an API using Azure Functions. The API reads data from Cosmos DB and returns the results to the web app.

**Serverless event processing**

![](https://learn.microsoft.com/azure/architecture/reference-architectures/serverless/_images/serverless-event-processing.png)

The application ingests a stream of data, processes the data, and writes the results to a back-end database (Cosmos DB).

For more information about these architectures, including guidance about best practices, see the following articles in the Azure Architecture Center: 

- [Serverless web application](https://learn.microsoft.com/azure/architecture/reference-architectures/serverless/web-app)

- [Serverless event processing using Azure Functions](https://learn.microsoft.com/azure/architecture/reference-architectures/serverless/event-processing)


## Deployment

Follow [steps here](./src/readme.md) to deploy this reference implementation.
