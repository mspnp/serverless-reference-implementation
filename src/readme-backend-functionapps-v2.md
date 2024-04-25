# Drone Status v2 backend function app deploy

We are going to develop a new Azure Function version using nodejs, and then change the environment to use the new api v2.

## step 1

Set environment variables

```
# Export the following environment variables. The same that you have created during v1 deployment.
# if you are in the same context than v1, you don't need to do again
export RESOURCEGROUP=<resource-group>
export APPNAME=<functionapp-name>
export COSMOSDB_DATABASE_NAME=${APPNAME}-db
export COSMOSDB_DATABASE_COL=${APPNAME}-col
```

## step 2

Read the database account

```bash
# export the database account
export COSMOSDB_DATABASE_ACCOUNT=$(az deployment group show \
                                    -g ${RESOURCEGROUP} \
                                    -n azuredeploy-backend-functionapps \
                                    --query properties.outputs.cosmosDatabaseAccount.value \
                                    -o tsv)
```

## step 3

Create the new Function App Server

```bash
# create function app
az deployment group create \
   -g ${RESOURCEGROUP} \
   --template-file azuredeploy-backend-functionapps-v2.bicep \
   --parameters appName=${APPNAME} \
                cosmosDatabaseAccountName=${COSMOSDB_DATABASE_ACCOUNT} \
                cosmosDatabaseName=${COSMOSDB_DATABASE_NAME} \
                cosmosDatabaseCollection=${COSMOSDB_DATABASE_COL}
```

## step 4

Compile the new API version

```bash
cd DroneStatus/nodejs
func azure functionapp publish ${APPNAME}-dsv2-funcapp --typescript
cd  ./../../

```

## step 5

Enable security to the new Azure Function App Server. The values are the same as they were in the v1 deploy.

```bash
az webapp auth config-version upgrade --resource-group $RESOURCEGROUP --name ${APPNAME}-dsv2-funcapp

az webapp auth microsoft update --resource-group $RESOURCEGROUP --name ${APPNAME}-dsv2-funcapp  --client-id $API_APP_ID  --allowed-audiences $IDENTIFIER_URI --issuer $ISSUER_URL
az webapp auth update --resource-group $RESOURCEGROUP --name ${APPNAME}-dsv2-funcapp --enabled  --action Return401

```

### Allow client app contact Done Status v2 Function App

1. **Navigate to the Done Status v2 Function App**.
2. Go to the **Authentication** section.
3. Edit the **Microsoft Identity Provider** settings.
4. Under **Client application requirements**, select **"Allow requests from specific client applications"**.
5. Add the **$CLIENT_APP_ID** to the list of allowed client applications.
6. Under **Tenant requirement**, select **"Allow requests from specific tenants"**.
7. Add the **$TENANT_ID** to the list of allowed tenants.
8. Save your changes.

## step 6

Deploy API management v2
Get the function key for the new DroneStatus function.

1. Open the Azure portal
1. Navigate to the resource group and open the new DroneStatus function app
1. Click Fuctions and then select GetStatusFunction.
1. Under Function Keys, copy the default value

> Note: it is possible to use the default function key but as a best practice,
> please create a new one. This can be used later on when setting up your
> Azure API Management resource as a way to lock down drone status function with a
> rovokable specific key.
> By the time writing this, azure function key management using ARM is problematic.
> For more information please refer to https://github.com/Azure/azure-functions-host/wiki/Changes-to-Key-Management-in-Functions-V2#arm-impact

```bash
export FUNCTIONAPP_URL_V2="https://$(az functionapp show -g ${RESOURCEGROUP} -n ${APPNAME}-dsv2-funcapp --query defaultHostName -o tsv)/api"

export FUNCTIONAPP_KEY_V2=<function-key-from-the-previous-step>

az deployment group create \
   -g ${RESOURCEGROUP} \
   --template-file azuredeploy-apim.json \
   --parameters functionAppNameV1=${DRONE_STATUS_FUNCTION_APP_NAME} \
           functionAppCodeV1=${FUNCTIONAPP_KEY} \
           functionAppUrlV1=${FUNCTIONAPP_URL}  \
           functionAppCodeV2=${FUNCTIONAPP_KEY_V2} \
		     functionAppUrlV2=${FUNCTIONAPP_URL_V2}
```

## step 7

Create API Management policies

```bash
export API_MANAGEMENT_SERVICE=$(az deployment group show \
                                    --resource-group ${RESOURCEGROUP} \
                                    --name azuredeploy-apim \
                                    --query properties.outputs.apimGatewayServiceName.value \
                                    --output tsv)
export API_POLICY_ID_V2="$(az resource show --resource-group $RESOURCEGROUP --resource-type Microsoft.ApiManagement/service --name $API_MANAGEMENT_SERVICE --query id --output tsv)/apis/dronedeliveryapiv2/policies/policy"
az resource create --id $API_POLICY_ID_V2 \
    --properties "{
        \"value\": \"<policies><inbound><base /><cors allow-credentials=\\\"true\\\"><allowed-origins><origin>$CLIENT_URL</origin></allowed-origins><allowed-methods><method>GET</method></allowed-methods><allowed-headers><header>*</header></allowed-headers></cors><validate-jwt header-name=\\\"Authorization\\\" failed-validation-httpcode=\\\"401\\\" failed-validation-error-message=\\\"Unauthorized. Access token is missing or invalid.\\\"><openid-config url=\\\"${ISSUER_URL}.well-known/openid-configuration\\\" /><required-claims><claim name=\\\"aud\\\"><value>$IDENTIFIER_URI</value></claim></required-claims></validate-jwt></inbound><backend><base /></backend><outbound><base /></outbound><on-error><base /></on-error></policies>\"
    }"
```

## step 8

The client app need to be changed to use the new api version
We need to change config.js to use `v2`

```bash
export const getApiConfig = () =>
{
  return {
    url: `${process.env.AZURE_API_URL}`,
    version: `/v2`
  };
}
```

## step 9

The ClientApp deploy need to be trigger again
After checking the new version is deployed `$CLIENT_URL/semver.txt`, the new nodejs Azure Function is executed. 
You can check the url called from the Network developer tools (.../api/**v2**/dronestatus/drone-3)

> Note: for this example, both versions of the function will represent the same resource so they will share the Microsoft Entra ID application id. Tokens retrieved to access the API will work for both versions.
