# Drone Status and Drone Telemetry backend function apps deploy

## step 1

```
# export the following environment variables
export RESOURCEGROUP=<resource-group>
export APPNAME=<functionapp-name>
```

## step 2

```bash
# create function app
az group deployment create \
   -g ${RESOURCEGROUP} \
   --template-file azuredeploy-backend-functionapps.json \
   --parameters appName=${APPNAME}
```

## step 3

```bash
# export the following exports
export COSMOSDB_DATABASE_ACCOUNT=$(az group deployment show \
                                    -g ${RESOURCEGROUP} \
                                    -n azuredeploy-backend-functionapps \
                                    --query properties.outputs.cosmosDatabaseAccount.value \
                                    -o tsv) && \
export COSMOSDB_DATABASE_NAME=$(az group deployment show \
                                    -g ${RESOURCEGROUP} \
                                    -n azuredeploy-backend-functionapps \
                                    --query properties.outputs.cosmosDatabaseName.value \
                                    -o tsv) && \
export COSMOSDB_DATABASE_COL=$(az group deployment show \
                                    -g ${RESOURCEGROUP} \
                                    -n azuredeploy-backend-functionapps \
                                    --query properties.outputs.cosmosDatabaseCollection.value \
                                    -o tsv)

# create the Cosmos DB database
az cosmosdb database create \
   -g $RESOURCEGROUP \
   -n $COSMOSDB_DATABASE_ACCOUNT \
   -d $COSMOSDB_DATABASE_NAME

# create the collection
az cosmosdb collection create \
   -g $RESOURCEGROUP \
   -n $COSMOSDB_DATABASE_ACCOUNT \
   -d $COSMOSDB_DATABASE_NAME \
   -c $COSMOSDB_DATABASE_COL \
   --partition-key-path /id --throughput 100000
```

## step 4

```bash
# publish the drone status function
dotnet publish DroneStatus/dotnet/DroneStatusFunctionApp/ \
       --configuration Release \
       --output ./../../../dronestatus-publish && \
cd dronestatus-publish;zip -r DroneStatusFunction.zip *;cd ..

# alternatively in an environment where Microsoft Visual Studio is installed just exec
dotnet publish /p:PublishProfile=Azure /p:Configuration=Release
```

## step 5

```bash
# deploy the drone status function to a new function app
az functionapp deployment source config-zip \
   --src dronestatus-publish/DroneStatusFunction.zip \
   -g $RESOURCEGROUP \
   -n ${APPNAME}-ds-funcapp
```

## step 6

```bash
# create new function Key from Azure Portal to lock down drone status function.
# navigate Function Apps -> <APPNAME>-ds-funcapp —> Manage —> Function Keys
export FUNCTIONAPP_KEY_V1=<function-key>
```

> Note: it is possible to use the default function key but as a best practice,
> please create a new one. This can be used later on when setting up your
> Azure API Management resource as a way to lock down drone status function with a
> rovokable specific key.
> By the time writing this, azure function key management using ARM is problematic.
> For more information please refer to https://github.com/Azure/azure-functions-host/wiki/Changes-to-Key-Management-in-Functions-V2#arm-impact

## step 7

```bash
# publish the drone telemetry function
dotnet publish DroneTelemetry/DroneTelemetryFunctionApp/ \
       --configuration Release \
       --output ./../../dronetelemetry-publish && \
cd dronetelemetry-publish;zip -r DroneTelemetryFunction.zip *;cd ..

# alternatively in an environment where Microsoft Visual Studio is installed just exec
dotnet publish /p:PublishProfile=Azure /p:Configuration=Release
```

## step 8

```bash
# deploy the drone telemetry function to a new function app
az functionapp deployment source config-zip \
   --src dronetelemetry-publish/DroneTelemetryFunction.zip \
   -g $RESOURCEGROUP \
   -n ${APPNAME}-dt-funcapp
```
