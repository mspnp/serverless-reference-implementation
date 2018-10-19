# Drone Status v2 backend function app deploy

## step 1

```
# export the following environment variables
export RESOURCEGROUP=<resource-group>
export APPNAME=<functionapp-name>
```

## step 2

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
```

## step 3

```bash
# create function app
az group deployment create \
   -g ${RESOURCEGROUP} \
   --template-file azuredeploy-backend-functionapps-v2.json \
   --parameters appName=${APPNAME} \
                cosmosDatabaseAccount=${COSMOSDB_DATABASE_ACCOUNT} \
                cosmosDatabaseName=${COSMOSDB_DATABASE_NAME} \
                cosmosDatabaseCollection=${COSMOSDB_DATABASE_COL}
```

## step 4

```bash
# publish the drone status function v2
cd DroneStatus/nodejs && \
dotnet publish -c Release -o bin || \
cp bin/Release/netstandard2.0/extensions.json bin/. && \
rm -rf obj && \
npm install && \
zip -r DroneStatusFunction-nodejs.zip * && \
mkdir -p ./../../dronestatus-nodejs-publish/ && \
mv DroneStatusFunction-nodejs.zip ./../../dronestatus-nodejs-publish/. && \
cd  ./../../
```

## step 5

```bash
# deploy the drone status function to the new function app
az functionapp deployment source config-zip \
   --src dronestatus-nodejs-publish/DroneStatusFunction-nodejs.zip \
   -g $RESOURCEGROUP \
   -n ${APPNAME}-dsv2-funcapp
```

## step 6

```bash
# create new function Key from Azure Portal to lock down drone status function
export FUNCTIONAPP_KEY_V2=<function-key>
```

> Note: it is possible to use the default function key but as a best practice,
> please create a new one. This can be used later on when setting up your
> Azure API Management resource as a way to lock down drone status function with a
> rovokable specific key.
> By the time writing this, azure function key management using ARM is problematic.
> For more information please refer to https://github.com/Azure/azure-functions-host/wiki/Changes-to-Key-Management-in-Functions-V2#arm-impact

## step 7

1. In the Azure Portal, navigate to the drone status v2 function.
2. Select **Platform features**
3. Click **Authentication / Authorization**
4. Toggle App Service Authentication to **On**.
5. Click **Azure Active Directory**.
6. In the **Azure Active Directory Settings** blade, select **Express**, click the default **Select Existing AD App**.
7. Click **Azure AD App**, select the application created for v1 in the list, and click **OK**.
7. Click **OK**.
8. Click **Save**.

> Note: for this example, both versions of the function will represent the same resource so they will share the AAD application id. Tokens retrieved to access the API will work for both versions.
