# Serverless client app

## Prerequisite

 - Install [node](https://nodejs.org/en/download/)

##  Register an application with your Azure Active Directory tenant

> If you're using a tenant different to the tenant associated to the subscription, log in to that tenant. You will need to log in the subscription after creating the application to continue the instructions.

```bash
az login --tenant $TENANT_ID --allow-no-subscriptions
```

```bash
# Specify the new app name
export CLIENT_APP_NAME=<app name>

# Create the application registration, requesting permission to access the Graph API and to impersonate a user when calling the drone status API 
export API_IMPERSONATION_PERMISSION=$(az ad app show --id $API_APP_ID --query "oauth2Permissions[?value == 'user_impersonation'].id" --output tsv)
export CLIENT_APP_ID=$(az ad app create --display-name $CLIENT_APP_NAME --oauth2-allow-implicit-flow true \
--native-app false --reply-urls http://localhost --identifier-uris "http://$CLIENT_APP_NAME" \
--required-resource-accesses "  [ { \"resourceAppId\": \"$API_APP_ID\", \"resourceAccess\": [ { \"id\": \"$API_IMPERSONATION_PERMISSION\", \"type\": \"Scope\" } ] }, { \"resourceAppId\": \"00000003-0000-0000-c000-000000000000\", \"resourceAccess\": [ { \"id\": \"e1fe6dd8-ba31-4d61-89e7-88639da4683d\", \"type\": \"Scope\" } ] } ]" \
--query appId --output tsv)

# Create a service principal for the registered application
az ad sp create --id $CLIENT_APP_ID
az ad sp update --id $CLIENT_APP_ID --add tags "WindowsAzureActiveDirectoryIntegratedApp"
```

> Log back into your subscription if you've used a different tenant.

```bash
az login
```

## Update the client app

```bash
export APIM_GATEWAY_URL=$(az group deployment show \
                                    --resource-group ${RESOURCEGROUP} \
                                    --name azuredeploy-apim \
                                    --query properties.outputs.apimGatewayURL.value \
                                    --output tsv) 
cat src/ClientApp/src/services/config.js | \
sed "s#<Azure AD tenant name>#$TENANT_ID#g" | \
sed "s#<application id>#$CLIENT_APP_ID#g" | \
sed "s#<api application id>#$API_APP_ID#g" | \
sed "s#<URL of the API Management API endpoint>#${APIM_GATEWAY_URL}#g" \
> config.js.tmp && mv config.js.tmp src/ClientApp/src/services/config.js
```

## Create your static website

1. install Gatsby cli
```bash
npm install -g gatsby-cli
```

2. navigate to the SPA client app folder and build static files
```bash
cd ./serverless-reference-implementation/src/ClientApp && \
gatsby build
```

## Deploy to Azure Storage static website hosting

```bash
export STORAGE_ACCOUNT_NAME=<storage account name>

# Create the storage account 
az storage account create --name $STORAGE_ACCOUNT_NAME --resource-group $RESOURCEGROUP --location $LOCATION --kind StorageV2

# Enable static web site support for the storage account
az storage blob service-properties update --account-name $STORAGE_ACCOUNT_NAME --static-website --404-document 404.html --index-document index.html

# Upload the web site
az storage blob upload-batch --source ./ClientApp/public --destination \$web --account-name $STORAGE_ACCOUNT_NAME

# Retrieve the static website endpoint
export WEB_SITE_URL=$(az storage account show --name $STORAGE_ACCOUNT_NAME --resource-group $RESOURCEGROUP --query primaryEndpoints.web --output tsv)
export WEB_SITE_HOST=$(echo $WEB_SITE_URL | sed -rn 's#.+//([^/]+)/?#\1#p')
```

See [Static website hosting in Azure Storage](https://docs.microsoft.com/azure/storage/blobs/storage-blob-static-website) for details.

## Set up the Azure CDN endpoint to point to the static web site

```bash
export CDN_NAME=<cdn name>

# Create the CDN profile and endpoint
az cdn profile create --location $LOCATION --resource-group $RESOURCEGROUP --name $CDN_NAME
export CDN_ENDPOINT_HOST=$(az cdn endpoint create --location $LOCATION --resource-group $RESOURCEGROUP --profile-name $CDN_NAME --name $CDN_NAME \
--no-http --origin $WEB_SITE_HOST --origin-host-header $WEB_SITE_HOST \
--query hostName --output tsv)

export CLIENT_URL="https://$CDN_ENDPOINT_HOST"
```

## Update the reply URL for the registered application

> If you're using a tenant different to the tenant associated to the subscription, log in to that tenant. You will need to log in the subscription after creating the application to continue the instructions.

```bash
az login --tenant $TENANT_ID --allow-no-subscriptions
```

```bash
az ad app update --id $CLIENT_APP_ID --set replyUrls="[\"$CLIENT_URL\"]"
```

> Log back into your subscription if you've used a different tenant.

```bash
az login
```

## Launch the application granting consent

The first time you use the client application you need to consent to the delegated permissions specified for the application, unless an administrator granted consent for all users in the directory. 

For single-page applications, user consent can be granted by navigating to an Azure AD URL as specified below; the web site will request a user login and consent for the application. To actually use the application, you need to complete the rest of the deployment instructions to complete the configuration.

```bash
echo "Open a browser on 'https://login.microsoftonline.com/${TENANT_ID}/oauth2/v2.0/authorize?client_id=${CLIENT_APP_ID}&response_type=code&redirect_uri=https%3A%2F%2F${CDN_ENDPOINT_HOST}&response_mode=query
&scope=${API_APP_ID}%2Fuser_impersonation&state=12345'"
```

See [Types of permissions and consent](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-permissions-and-consent) for details.