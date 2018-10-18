# Drone Delivery Serverless

follow [steps here](./src/readme.md) to deploy this RI.

# Drone Status Function App CICD with Azure DevOps

add CICD to Drone Status using Azure Pipelines with YAML and Azure Functions Slots.

## Prerequistes
1. [create Azure DevOps account](https://azure.microsoft.com/en-us/services/devops)
2. [add Azure subscription as service connection](https://docs.microsoft.com/en-us/azure/devops/pipelines/library/connect-to-azure?view=vsts#create-an-azure-resource-manager-service-connection-with-an-existing-service-principal)
3. [optionally, assign service connection application to role, so it is allowed to create new azure resources](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-create-service-principal-portal#assign-application-to-role)
4. create a Github or Azure Repos repository

## Configure CICD using Azure Pipelines

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

Replace azure pipeline place holders

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

Trigger the CICD pipeline by pushing to staging

```
git checkout -b staging && \
git push <remote-name> staging
```

> Note: also feature branches are going through the CI pipeline.

Follow CICD from Azure Pipelines

```
open https://dev.azure.com/<organization-name>/<project-name>/_build
```
