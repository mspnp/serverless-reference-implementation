# Drone Delivery API Managment

## step 1

```bash
# export the following environment variables
export LOCATION=<location>
export RESOURCEGROUP=<resource-group>
```

## step 2

```bash
# create resource group
az group create \
   -n $RESOURCEGROUP \
   -l $LOCATION
```

## step 3

follow the instruction to deploy the [backend function apps](./readme-backend-functionapps.md)

## step 4

optionally it is possible to have backend versions side by side of drone status
by [deploying a v2](./readme-backend-functionapps-v2.md) or please feel free to skip this step for now.

## step 5

```bash
# export the following environment variables for the drone status function app. FUNCTIONAPP_KEY may already be set by the previous step.

export FUNCTIONAPP_NAME_V1=<drone-status-functionapp-name-v1>
export FUNCTIONAPP_KEY_V1=<drone-status-function-key-v1> # please note you should have exported this in step 3

export FUNCTIONAPP_URL_V1="https://$(az functionapp show -g ${RESOURCEGROUP} -n ${FUNCTIONAPP_NAME_V1} --query defaultHostName -o tsv)/api"

# optionally export the following environment variables for drone status v2 function app if you have completed step 4

export FUNCTIONAPP_NAME_V2=<drone-status-functionapp-name-v2>
export FUNCTIONAPP_KEY_V2=<drone-status-function-key-v2> # please note you should have exported this in step 4

export FUNCTIONAPP_URL_V2="https://$(az functionapp show -g ${RESOURCEGROUP} -n ${FUNCTIONAPP_NAME_V2} --query defaultHostName -o tsv)/api"
```

> Note: Ideally, this function key shound't be shared, and only used by Azure API Managment

## step 6

```bash
# create apim
az group deployment create \
   -g ${RESOURCEGROUP} \
   --template-file azuredeploy-apim.json \
   --parameters functionAppNameV1=${FUNCTIONAPP_NAME_V1} \
           functionAppCodeV1=${FUNCTIONAPP_KEY_V1} \
           functionAppUrlV1=${FUNCTIONAPP_URL_V1} \
           functionAppNameV2=${FUNCTIONAPP_NAME_V2} \
           functionAppCodeV2=${FUNCTIONAPP_KEY_V2} \
           functionAppUrlV2=${FUNCTIONAPP_URL_V2}
```

## step 7

```bash
# get apim gateway url
export APIM_GATEWAY_URL=$(az group deployment show \
                                    -g ${RESOURCEGROUP} \
                                    -n azuredeploy-apim \
                                    --query properties.outputs.apimGatewayURL.value \
                                    -o tsv)
```

## step 7

```bash
# test dronestatus v1 and v2 via APIM
curl GET "${APIM_GATEWAY_URL}/api/v1/dronestatus/{deviceid}" && \
curl GET "${APIM_GATEWAY_URL}/api/v2/dronestatus/{deviceid}"
```
