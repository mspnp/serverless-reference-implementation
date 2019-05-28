# Deploy the Drone Delivery Serverless App

## Prerequisites

- [.NET Core 2.1](https://www.microsoft.com/net/download)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli?view=azure-cli-latest)


Clone or download this repo locally.

```bash
git clone https://github.com/mspnp/serverless-reference-implementation.git
cd serverless-reference-implementation/src
```

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

Create a resource group.

```bash
az group create -n $RESOURCEGROUP -l $LOCATION
```

Deploy Azure resources.

```bash
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
az eventhubs namespace list \
     -g $RESOURCEGROUP \
     --query [].name

# list the send keys
az eventhubs eventhub authorization-rule keys list \
     -g $RESOURCEGROUP \
     --eventhub-name $APPNAME-eh  \
     --namespace-name <use-the-name-from-previous-step> \
     -n send

export EVENT_HUB_CONNECTION_STRING="<event-hub-connection-string>" # Use the 'send' authorization rule
export SIMULATOR_PROJECT_PATH=DroneSimulator/Serverless.Simulator/Serverless.Simulator.csproj

dotnet build $SIMULATOR_PROJECT_PATH
dotnet run --project $SIMULATOR_PROJECT_PATH
```

The simulator sends data to Event Hubs, which triggers the drone telemetry function app. You can verify the function app is working by viewing the logs in the Azure portal. Navigate to the `dronetelemetry` function app resource, select RawTelemetryFunction, and expand the **Logs** tab.

## Enable authentication in the function app

This step creates a new app registration for the API in Azure AD, and enables OIDC authentication in the function app.

1. In the Azure Portal, navigate to the drone status function.
2. Select **Platform features**
3. Click **Authentication / Authorization**
4. Toggle App Service Authentication to **On**.
5. Click **Azure Active Directory**.
6. In the **Azure Active Directory Settings** blade, select **Express**, leave the default **Create New AD App**.
7. Enter a name for application, such as "drone-api".
8. Click **OK**.
9. Click **Save**.

### Define a "GetStatus" role for the app

1. In the Azure Portal, navigate to your Azure AD tenant.
2. Click on **App registrations**.
3. View all applications, and select the drone API application.
4. From the application blade, click **Manifest** to open the inline manifest editor.
5. Define a "GetStatus" role for the app by adding the following entry in the "appRoles" array in the manifest (replacing the placeholder GUID with a new GUID)

    ```json
    {
    "allowedMemberTypes": [ "User" ],
    "description":"Access to device status",
    "displayName":"Get Device Status",
    "id": "[generate a new GUID]",
    "isEnabled":true,
    "value":"GetStatus"
    }
    ```
6. Click **Save**.

### Assign application to user or role

1. In the Azure Portal, navigate to your Azure AD tenant.
2. Click on **Enterprise Applications** and then click on the Drone Status application name.
3. Click **Users and groups**.
4. Click **Add user**.
5. Click **Users and groups**.
6. Select a user, and click **Select**.

    > Note: If you define more than one App role in the manifest, you can select the user's role. In this case, there is only one role, so the option is grayed out.

7. Click **Assign**.

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

## (Optional) Deploy v2 of GetStatus API

Optionally, it is possible to have backend versions side by side of drone status by [deploying a v2](./readme-backend-functionapps-v2.md).
