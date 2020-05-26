# Deploy the Drone Delivery Serverless App

## Prerequisites

- [.NET Core 2.1](https://www.microsoft.com/net/download)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli?view=azure-cli-latest), version 2.0.69 or higher
- [SED](https://www.gnu.org/software/sed/)

Clone or download this repo locally.

```bash
git clone https://github.com/mspnp/serverless-reference-implementation.git
cd serverless-reference-implementation/src
```

These instructions target Linux-based systems. For Windows machine, [install Windows Subsystem for Linux](https://docs.microsoft.com/windows/wsl/install-win10) and choose a Linux distribution. Make sure to install the .Net Core libraries specific to your environment. For Linux or Windows Subsystem for Linux, choose [this installation for your distribution](https://dotnet.microsoft.com/download/linux-package-manager/rhel/sdk-2.2.108) . 


## Deploy Azure resources

Export the following environment variables:

``` bash
export LOCATION=<location>
export RESOURCEGROUP=<resource-group>
export APPNAME=<functionapp-name> # Cannot be more than 6 characters
export APP_INSIGHTS_LOCATION=<application-insights-location>
export COSMOSDB_DATABASE_NAME=${APPNAME}-db
export COSMOSDB_DATABASE_COL=${APPNAME}-col
```

> Note: This reference implementation uses Application Insights, an Azure resource that might not be available in [all regions](https://azure.microsoft.com/en-us/global-infrastructure/services/?products=monitor). Ensure to select a region for `APP_INSIGHTS_LOCATION` that supports this resource, preferably the same as or nearest region to `LOCATION` for network performance and cost benefits.

Login to Azure CLI and select your subscription.

```bash
az login
az account set --subscription <your-subscription-id>
```

Create a resource group.

```bash
az group create -n $RESOURCEGROUP -l $LOCATION
```

Deploy Azure resources.

```bash
az deployment group create \
   -g ${RESOURCEGROUP} \
   --template-file azuredeploy-backend-functionapps.json \
   --parameters appName=${APPNAME} \
   appInsightsLocation=${APP_INSIGHTS_LOCATION} \
   cosmosDatabaseName=${COSMOSDB_DATABASE_NAME} \
   cosmosDatabaseCollection=${COSMOSDB_DATABASE_COL}
```

Create Cosmos DB database and collection.

```bash
# Get the Cosmos DB account name from the deployment output
export COSMOSDB_DATABASE_ACCOUNT=$(az group deployment show \
                                    -g ${RESOURCEGROUP} \
                                    -n azuredeploy-backend-functionapps \
                                    --query properties.outputs.cosmosDatabaseAccount.value \
                                    -o tsv) 

# Create the Cosmos DB database
az cosmosdb database create \
   -g $RESOURCEGROUP \
   -n $COSMOSDB_DATABASE_ACCOUNT \
   -d $COSMOSDB_DATABASE_NAME

# Create the collection
az cosmosdb collection create \
   -g $RESOURCEGROUP \
   -n $COSMOSDB_DATABASE_ACCOUNT \
   -d $COSMOSDB_DATABASE_NAME \
   -c $COSMOSDB_DATABASE_COL \
   --partition-key-path /id --throughput 10000
```

## Publish Azure Function Apps

Deploy the drone status function

```bash
## Get the functiona app name from the deployment output
export DRONE_STATUS_FUNCTION_APP_NAME=$(az group deployment show \
                                    -g ${RESOURCEGROUP} \
                                    -n azuredeploy-backend-functionapps \
                                    --query properties.outputs.droneStatusFunctionAppName.value \
                                    -o tsv) 

# Publish the function to a local directory
dotnet publish DroneStatus/dotnet/DroneStatusFunctionApp/ \
       --configuration Release \
       --output ./../../../dronestatus-publish
(cd dronestatus-publish && zip -r DroneStatusFunction.zip *)

# Alternatively, if you have Microsoft Visual Studio installed:
# dotnet publish /p:PublishProfile=Azure /p:Configuration=Release

# Deploy the function to the function app
az functionapp deployment source config-zip \
   --src dronestatus-publish/DroneStatusFunction.zip \
   -g $RESOURCEGROUP \
   -n ${DRONE_STATUS_FUNCTION_APP_NAME}
```

Deploy the drone telemetry function

```bash
## Get the functiona app name from the deployment output
export DRONE_TELEMETRY_FUNCTION_APP_NAME=$(az group deployment show \
                                    -g ${RESOURCEGROUP} \
                                    -n azuredeploy-backend-functionapps \
                                    --query properties.outputs.droneTelemetryFunctionAppName.value \
                                    -o tsv) 

# Publish the function to a local directory
dotnet publish DroneTelemetry/DroneTelemetryFunctionApp/ \
       --configuration Release \
       --output ./../../dronetelemetry-publish
(cd dronetelemetry-publish && zip -r DroneTelemetryFunction.zip *)

# Alternatively, if you have Microsoft Visual Studio installed:
##dotnet publish /p:PublishProfile=Azure /p:Configuration=Release

# Deploy the function to the function app
az functionapp deployment source config-zip \
   --src dronetelemetry-publish/DroneTelemetryFunction.zip \
   -g $RESOURCEGROUP \
   -n ${DRONE_TELEMETRY_FUNCTION_APP_NAME}
```

## Deploy the API Management gateway

Get the function key for the DroneStatus function. 

1. Open the Azure portal
2. Navigate to the resource group and open the blade for the DroneStatus function
3. Click **Manage**.
4. Under **Function Keys**, click **Copy**

Deploy API Management

```bash
export FUNCTIONAPP_KEY=<function-key-from-the-previous-step>

export FUNCTIONAPP_URL="https://$(az functionapp show -g ${RESOURCEGROUP} -n ${DRONE_STATUS_FUNCTION_APP_NAME} --query defaultHostName -o tsv)/api"

az group deployment create \
   -g ${RESOURCEGROUP} \
   --template-file azuredeploy-apim.json \
   --parameters functionAppNameV1=${DRONE_STATUS_FUNCTION_APP_NAME} \
           functionAppCodeV1=${FUNCTIONAPP_KEY} \
           functionAppUrlV1=${FUNCTIONAPP_URL}
```

> Allow 20-30 minutes for this step to complete.

## Build and run the device simulator

```bash
# list Event Hub namespace name(s)
export EH_NAMESPACE=$(az eventhubs namespace list \
     -g $RESOURCEGROUP \
     --query [].name --output tsv)

# list the send keys
export EVENT_HUB_CONNECTION_STRING=$(az eventhubs eventhub authorization-rule keys list \
     -g $RESOURCEGROUP \
     --eventhub-name $APPNAME-eh  \
     --namespace-name $EH_NAMESPACE \
     -n send \
     --query primaryConnectionString --output tsv)

export SIMULATOR_PROJECT_PATH=DroneSimulator/Serverless.Simulator/Serverless.Simulator.csproj

dotnet build $SIMULATOR_PROJECT_PATH
dotnet run --project $SIMULATOR_PROJECT_PATH
```

The simulator sends data to Event Hubs, which triggers the drone telemetry function app. You can verify the function app is working by viewing the logs in the Azure portal. Navigate to the `dronetelemetry` function app resource, select RawTelemetryFunction, expand the **Monitor** tab, and click on any of the logs.

## Enable authentication in the function app

This step creates a new app registration for the API in Azure AD, and enables OIDC authentication in the function app.

### Register the application in Azure AD
If you're planning on using the tenant associated to your Azure subscription you can retrieve it.

```bash
export TENANT_ID=$(az account show --query tenantId --output tsv)
```

If you're planning on using a different tenant instead, log in to that tenant. You will need to log in the subscription after creating the application to continue the instructions.

```bash
export TENANT_ID=<your tenant id>
az login --tenant $TENANT_ID --allow-no-subscriptions
```

```bash
# Specify the new app name
export API_APP_NAME=<app name>

# Collect information about your tenant
export FEDERATION_METADATA_URL="https://login.microsoftonline.com/$TENANT_ID/FederationMetadata/2007-06/FederationMetadata.xml"
export ISSUER_URL=$(curl $FEDERATION_METADATA_URL --silent | sed -n 's/.*entityID="\([^"]*\).*/\1/p')

# Create the application registration, defining a new application role and requesting access to read a user using the Graph API
export API_APP_ID=$(az ad app create --display-name $API_APP_NAME --oauth2-allow-implicit-flow true \
--native-app false --reply-urls http://localhost --identifier-uris "http://$API_APP_NAME" \
--app-roles '  [ {  "allowedMemberTypes": [ "User" ], "description":"Access to device status", "displayName":"Get Device Status", "isEnabled":true, "value":"GetStatus" }]' \
--required-resource-accesses '  [ {  "resourceAppId": "00000003-0000-0000-c000-000000000000", "resourceAccess": [ { "id": "e1fe6dd8-ba31-4d61-89e7-88639da4683d", "type": "Scope" } ] }]' \
--query appId --output tsv)

# Create a service principal for the registered application
az ad sp create --id $API_APP_ID
az ad sp update --id $API_APP_ID --add tags "WindowsAzureActiveDirectoryIntegratedApp"
```

Log back into your subscription if you've used a different tenant.

```bash
az login
```

### Configure Azure AD authentication in the Function App

```bash
az webapp auth update --resource-group $RESOURCEGROUP --name $DRONE_STATUS_FUNCTION_APP_NAME --enabled true \
--action LoginWithAzureActiveDirectory \
--aad-token-issuer-url $ISSUER_URL \
--aad-client-id $API_APP_ID
```

### Assign application to user or role
This is required for the admin user who will need to be authenticated to use the Azure Function.

1. In the Azure Portal, navigate to your Azure AD tenant.
2. Click on **Enterprise Applications** and then search and select the Drone Status application name.
3. Click **Users and groups**.
4. Click **Add user**.
5. Click **Users and groups**.
6. Select a user, and click **Select**.

    > Note: If you define more than one App role in the manifest, you can select the user's role. In this case, there is only one role, so the option is grayed out.

7. Click **Assign**.

## Deploy the single-page web app

Follow the instructions [here](./ClientApp/readme.md) to deploy the SPA. Make sure to follow these instructions in the same session and keep the session open to make variables available to the next step.

Next, update the policies in the API Management gateway. This update adds the CDN endpoint URL as an allowed origin for the CORS configuration, and enables authentication for tokens issued for the registered application for the API.

```bash
export API_MANAGEMENT_SERVICE=$(az group deployment show \
                                    --resource-group ${RESOURCEGROUP} \
                                    --name azuredeploy-apim \
                                    --query properties.outputs.apimGatewayServiceName.value \
                                    --output tsv) 
export API_POLICY_ID="$(az resource show --resource-group $RESOURCEGROUP --resource-type Microsoft.ApiManagement/service --name $API_MANAGEMENT_SERVICE --query id --output tsv)/apis/dronedeliveryapiv1/policies/policy"
az resource create --id $API_POLICY_ID \
    --properties "{
        \"value\": \"<policies><inbound><base /><cors allow-credentials=\\\"true\\\"><allowed-origins><origin>$CLIENT_URL</origin></allowed-origins><allowed-methods><method>GET</method></allowed-methods><allowed-headers><header>*</header></allowed-headers></cors><validate-jwt header-name=\\\"Authorization\\\" failed-validation-httpcode=\\\"401\\\" failed-validation-error-message=\\\"Unauthorized. Access token is missing or invalid.\\\"><openid-config url=\\\"https://login.microsoftonline.com/$TENANT_ID/.well-known/openid-configuration\\\" /><required-claims><claim name=\\\"aud\\\"><value>$API_APP_ID</value></claim></required-claims></validate-jwt></inbound><backend><base /></backend><outbound><base /></outbound><on-error><base /></on-error></policies>\"
    }"
```

## (Optional) Use an Azure Front Door as alternative to CDN

Alternatively, it is possible to have static web content stored by the same storage account used by the CDN, but using an Azure Front Door instead.
This can be done by configuring a backend pool with a custom host name and having the backend host header set to the URL of the static storage account website.

Prerequisite

1) The storage account has to be a general purpose v2 storage account
2) For storage account name and resource group name, use the same variables used for installing the serverless client app

```bash
export AFD_NAME=<Azure Front Door Name>


# Retrieve the static website endpoint
export WEB_SITE_URL=$(az storage account show --name $STORAGE_ACCOUNT_NAME --resource-group $RESOURCEGROUP --query primaryEndpoints.web --output tsv)
export WEB_SITE_HOST=$(echo $WEB_SITE_URL | sed -rn 's#.+//([^/]+)/?#\1#p')

# Install the Azure Front Door extension
az extension add --name front-doorHttp

# Create and Configure the Azure Front Door
az network front-door create --backend-address $WEB_SITE_HOST --name $AFD_NAME --resource-group $RESOURCEGROUP --accepted-protocols Http Https
```

## (Optional) Deploy v2 of GetStatus API

Optionally, it is possible to have backend versions side by side of drone status by [deploying a v2](./readme-backend-functionapps-v2.md).
