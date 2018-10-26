# Drone Delivery Serverless

This project contains a reference implementation for two serverless application architectures.

**Serverless web application**

![](https://docs.microsoft.com/azure/architecture/reference-architectures/serverless/_images/serverless-web-app.png)

The application serves static content from Azure Blob Storage, and implements an API using Azure Functions. The API reads data from Cosmos DB and returns the results to the web app.

**Serverless event processing**

![](https://docs.microsoft.com/azure/architecture/reference-architectures/serverless/_images/serverless-event-processing.png)

The application ingests a stream of data, processes the data, and writes the results to a back-end database (Cosmos DB).

For more information about these architectures, including guidance about best practices, see the following articles in the Azure Architecture Center: 

- [Serverless web application](https://docs.microsoft.com/azure/architecture/reference-architectures/serverless/web-app)

- [Serverless event processing using Azure Functions](https://docs.microsoft.com/azure/architecture/reference-architectures/serverless/event-processing)


## Deployment

Follow [steps here](./src/readme.md) to deploy this reference implementation.

## Drone Status Function App CI/CD with Azure DevOps

Add CI/CD to Drone Status using Azure Pipelines with YAML and Azure Functions Slots.

## Prerequistes
1. [Create Azure DevOps account](https://azure.microsoft.com/en-us/services/devops)
2. [Add Azure subscription as service connection](https://docs.microsoft.com/en-us/azure/devops/pipelines/library/connect-to-azure?view=vsts#create-an-azure-resource-manager-service-connection-with-an-existing-service-principal)
3. [Optionally, assign service connection application to role, so it is allowed to create new azure resources](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-create-service-principal-portal#assign-application-to-role)
4. Create a Github or Azure Repos repository.

## Configure CI/CD using Azure Pipelines

Clone and add remote

```bash
git clone https://github.com/mspnp/serverless-reference-implementation.git && \
cd serverless-reference-implementation && \
git remote add <remote-name> <remote-url> # this remote url corresponds to the prerequisite step 4th
```

Export the following environment variables

```
export SERVICECONNECTION=<service-connection-name> # use the name configured in the 2nd prerequisite step
export LOCATION=<location>
export RESOURCEGROUP=<resource-group>
export APPNAME=<app-name> # less or equal than 6 chars
export SLOTNAME=<slot-name>
```

Replace Azure Pipeline place holders

```
sed -i "s#ServiceConnectionName: '<serviceconnection>'#ServiceConnectionName: '$SERVICECONNECTION'#g" azure-pipelines.yml && \
sed -i "s#Location: '<location>'#Location: '$LOCATION'#g"  azure-pipelines.yml && \
sed -i "s#ResourceGroup: '<resourcegroup>'#ResourceGroup: '$RESOURCEGROUP'#g" azure-pipelines.yml && \
sed -i "s#AppName: '<appName>'#AppName: '$APPNAME'#g" azure-pipelines.yml && \
sed -i "s#SlotName: '<slotName>'#SlotName: '$SLOTNAME'#g" azure-pipelines.yml
```

Push changes to azure repos or github

```bash
git push <remote-name> master
```

Follow instructions below to configure your first Azure Pipeline

[Get your first build with Azure Pipelines](https://docs.microsoft.com/en-us/azure/devops/pipelines/get-started-yaml?view=vsts#get-your-first-build)

> Note: this first build will attemp to execute the azurepipeline.yml against master

Trigger the CI/CD pipeline by pushing to staging

```
git checkout -b staging && \
git push <remote-name> staging
```

> Note: also feature branches are going through the CI pipeline.

Follow CI/CD from Azure Pipelines

```
open https://dev.azure.com/<organization-name>/<project-name>/_build
```
