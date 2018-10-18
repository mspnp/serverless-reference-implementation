# Drone Delivery Serverless

follow [steps here](./src/readme.md) to deploy this RI.

# Drone Status Function App CICD with Azure DevOps

add CICD to Drone Status using Azure Pipelines with YAML and Azure Functions Slots.

## Prerequistes
1. [create Azure DevOps account](https://azure.microsoft.com/en-us/services/devops)
2. [add Azure subscription as service connection](https://docs.microsoft.com/en-us/azure/devops/pipelines/library/connect-to-azure?view=vsts#create-an-azure-resource-manager-service-connection-with-an-existing-service-principal)
3. [assign application for service connection to role, so it is allow to create new azure resources](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-group-create-service-principal-portal#assign-application-to-role)
4. create a Github or Azure Repos repository

## step 1
```
#export the following environment variables

export SERVICECONNECTION=<serviceconnectionname>
export LOCATION=<location>
export RESOURCEGROUP=<resourcegroup>
export APPNAME=<appName> # less or equal than 6 chars
export SLOTNAME=<slotName>
```

## step 2
```
sed -i "s#ServiceConnectionName: '<serviceconnection>'#ServiceConnectionName: '$SERVICECONNECTION'#g" azure-pipelines.yml && \
sed -i "s#Location: '<location>'#Location: '$LOCATION'#g"  azure-pipelines.yml && \
sed -i "s#ResourceGroup: '<resourcegroup>'#ResourceGroup: '$RESOURCEGROUP'#g" azure-pipelines.yml && \
sed -i "s#AppName: '<appName>'#AppName: '$APPNAME'#g" azure-pipelines.yml && \
sed -i "s#SlotName: '<slotName>'#SlotName: '$SLOTNAME'#g" azure-pipelines.yml
```
## step 3

```bash
# clone and add remote
git clone <repo> && \
git remote add <remotename> <remoteurl> # this remote url corresponds to the prerequisite step 4th
```

## step 4

```bash
# push changes to azure repos or github
git push <remotename> master
```

## step 5

```bash
follow instructions below to configure your first Azure Pipeline
https://docs.microsoft.com/en-us/azure/devops/pipelines/get-started-yaml?view=vsts#get-your-first-build
```

## step 6

```
# deploy a new version of your azure function app by pushing changes into staging
git checkout -b staging && \
git push <remotename> staging
```
> Note: also feature branches are going through the CI pipeline.

## step 7
```
# follow CICD from Azure Pipelines
open https://dev.azure.com/<yourorganization>/<project>/_build
```
