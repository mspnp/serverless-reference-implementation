# Deploy the Drone Delivery Serverless App

## Prerequisites

- [.NET Core 2.1](https://www.microsoft.com/net/download)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli?view=azure-cli-latest)

## Deploy Azure resources

Export the following environment variables:

``` bash
export LOCATION=<location>
export RESOURCEGROUP=<resource-group>
export APPNAME=<functionapp-name> # Cannot be more than 8 characters
export APP_INSIGHTS_LOCATION=<application insights location>
export COSMOSDB_DATABASE_NAME=${APPNAME}-db
export COSMOSDB_DATABASE_COL=${APPNAME}-col
```

Create a resource group.

```bash
az group create -n $RESOURCEGROUP -l $LOCATION
```

Deploy Azure resources.

```bash
cd src

az group deployment create \
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
export FUNCTIONAPP_KEY=<function key from the previous step>

export FUNCTIONAPP_URL="https://$(az functionapp show -g ${RESOURCEGROUP} -n ${DRONE_STATUS_FUNCTION_APP_NAME} --query defaultHostName -o tsv)/api"

az group deployment create \
   -g ${RESOURCEGROUP} \
   --template-file azuredeploy-apim.json \
   --parameters functionAppNameV1=${DRONE_STATUS_FUNCTION_APP_NAME} \
           functionAppCodeV1=${FUNCTIONAPP_KEY} \
           functionAppUrlV1=${FUNCTIONAPP_URL} 
```

## Build and run the device simulator

```bash
export EVENT_HUB_CONNECTION_STRING=<event hub connection string>
export SIMULATOR_PROJECT_PATH=DroneSimulator/Serverless.Simulator/Serverless.Simulator.csproj

dotnet build $SIMULATOR_PROJECT_PATH
dotnet run --project $SIMULATOR_PROJECT_PATH
```

## Deploy the single-page web app

Follow the instructions [here](./ClientApp/readme.md) to deploy the SPA.

Next, update the policies in the API Management gateway:

1. In the Azure Portal, navigate to your API Management instance.
2. Click **APIs** and select the GetStatus API.
3. Click v1
4. Click **Design**.
5. Click the **&lt;/&gt;** icon next to **Policies**.
6. Paste in the following policy definitions:

    ```xml
    <inbound>
        <base />
        <cors allow-credentials="true">
            <allowed-origins>
                <origin>[Client website URL]</origin>
            </allowed-origins>
            <allowed-methods>
                <method>GET</method>
            </allowed-methods>
            <allowed-headers>
                <header>*</header>
            </allowed-headers>
        </cors>
        <validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="Unauthorized. Access token is missing or invalid.">
            <openid-config url="https://login.microsoftonline.com/[Azure AD directory ID]/.well-known/openid-configuration" />
            <required-claims>
                <claim name="aud">
                    <value>[API Application ID]</value>
                </claim>
            </required-claims>
        </validate-jwt>
    </inbound>
    ```
7. Click **Save**.
8. Repeat for v2

## (Optional) Deploy v2 of GetStatus API

Optionally, it is possible to have backend versions side by side of drone status by [deploying a v2](./readme-backend-functionapps-v2.md).
