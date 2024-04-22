# Serverless client app

## Prerequisites

- [Azure CLI 2.51 or later](https://learn.microsoft.com/cli/azure/install-azure-cli)
- [Azure DevOps account](https://azure.microsoft.com/services/devops)

## Register an application with your Microsoft Entra ID tenant

> If you're using a tenant different to the tenant associated to the subscription, log in to that tenant. You will need to log in the subscription after creating the application to continue the instructions.

```bash
az login --tenant $TENANT_ID --allow-no-subscriptions
```

```bash
# Specify the new app name
export CLIENT_APP_NAME=<app name>

# Create the application registration, requesting permission to access the Graph API and to impersonate a user when calling the drone status API
export API_IMPERSONATION_PERMISSION=$(az ad app show --id $API_APP_ID --query "api.oauth2PermissionScopes[0].id" --output tsv)

export SP_RESPONSE=$(az ad app create --display-name $CLIENT_APP_NAME \
--is-fallback-public-client false --identifier-uris "http://$TENANT_DOMAIN/$CLIENT_APP_NAME" \
--required-resource-accesses "  [ { \"resourceAppId\": \"$API_APP_ID\", \"resourceAccess\": [ { \"id\": \"$API_IMPERSONATION_PERMISSION\", \"type\": \"Scope\" } ] }, { \"resourceAppId\": \"00000003-0000-0000-c000-000000000000\", \"resourceAccess\": [ { \"id\": \"e1fe6dd8-ba31-4d61-89e7-88639da4683d\", \"type\": \"Scope\" } ] } ] ")

export CLIENT_APP_ID=$(echo $SP_RESPONSE | jq ".appId" -r)
export CLIENT_APP_OBJECT_ID=$(echo $SP_RESPONSE | jq ".id" -r)

# Create a service principal for the registered application
az ad sp create --id $CLIENT_APP_ID
az ad sp update --id $CLIENT_APP_ID --set tags='["WindowsAzureActiveDirectoryIntegratedApp"]'
```

> Log back into your subscription if you've used a different tenant.

```bash
az login
az account set --subscription <your-subscription-id>
```

## Allow client app contact Done Status Function App

1. **Navigate to the Done Status Function App**.
2. Go to the **Authentication** section.
3. Edit the **Microsoft Identity Provider** settings.
4. Under **Client application requirements**, select **"Allow requests from specific client applications"**.
5. Add the **$CLIENT_APP_ID** to the list of allowed client applications.
6. Under **Tenant requirement**, select **"Allow requests from specific tenants"**.
7. Add the **$TENANT_ID** to the list of allowed tenants.
8. Save your changes.

## Create Azure Storage static website hosting

```bash
export STORAGE_ACCOUNT_NAME=<storage account name>

# Create the storage account
az storage account create --name $STORAGE_ACCOUNT_NAME --resource-group $RESOURCEGROUP --location $LOCATION --kind StorageV2

# Enable static web site support for the storage account
az storage blob service-properties update --account-name $STORAGE_ACCOUNT_NAME --static-website --404-document 404.html --index-document index.html

# Retrieve the static website endpoint
export WEB_SITE_URL=$(az storage account show --name $STORAGE_ACCOUNT_NAME --resource-group $RESOURCEGROUP --query primaryEndpoints.web --output tsv)
export WEB_SITE_HOST=$(echo $WEB_SITE_URL | sed -rn 's#.+//([^/]+)/?#\1#p')
```

See [Static website hosting in Azure Storage](https://learn.microsoft.com/azure/storage/blobs/storage-blob-static-website) for details.

## Set up the Azure CDN endpoint to point to the static web site

```bash
export CDN_NAME=<cdn name>

# Create the CDN profile and endpoint
az cdn profile create --resource-group $RESOURCEGROUP --name $CDN_NAME --sku Standard_Microsoft
export CDN_ENDPOINT_HOST=$(az cdn endpoint create --location $LOCATION --resource-group $RESOURCEGROUP --profile-name $CDN_NAME --name $CDN_NAME \
--no-http --origin $WEB_SITE_HOST --origin-host-header $WEB_SITE_HOST \
--query hostName --output tsv)

# Configure custom caching rules
az cdn endpoint update \
   -g $RESOURCEGROUP \
   --profile-name $CDN_NAME \
   -n $CDN_NAME \
   --set deliveryPolicy.description="" \
   --set deliveryPolicy.rules='[{"name": "CacheExpiration", "actions": [{"name": "CacheExpiration","parameters": {"cacheType":"All","cacheBehavior": "Override","cacheDuration": "366.00:00:00"}}],"conditions": [{"name": "UrlFileExtension","parameters": {"operator":"EndsWith","matchValues": ["js","css","map"],"transforms": ["Lowercase"] }}],"order": 1}]'


export CLIENT_URL="https://$CDN_ENDPOINT_HOST"
```

## Clone and add remote

```
export GITHUB_USER=<github-username>
# the following repository must be created in GitHub beforehand under your repositories
export NEW_REMOTE_URL=https://github.com/${GITHUB_USER}/serverless-reference-implementation.git

git clone https://github.com/mspnp/serverless-reference-implementation.git && \
cd serverless-reference-implementation && \
git remote add newremote $NEW_REMOTE_URL && \
git push newremote master
```

> Note: alternatively you could fork this repo and then clone.

## Setup GitHub Action Workflow

 The GitHub Action integration uses [OpenID Connect (OIDC) with an Azure service principal using a Federated Identity Credential](https://learn.microsoft.com/azure/developer/github/connect-from-azure)

   1. Install [GitHub CLI](https://github.com/cli/cli/blob/trunk/docs/install_linux.md#official-sources).  
     Then you will need to login via the GitHib CLI

       ```bash
          gh auth login
       ```

   1. Create a Microsoft Entra application and service principal then assign a role on your subscription to your application so that your workflow has access to your subscription.  

       ```bash
         GH_ACTION_FEDERATED_IDENTITY=$(az ad app create --display-name ghActionFederatedIdentity)
         GH_ACTION_FEDERATED_IDENTITY_APP_ID=$(echo $GH_ACTION_FEDERATED_IDENTITY | jq -r '.appId')
         GH_ACTION_FEDERATED_IDENTITY_OBJECT_ID=$(echo $GH_ACTION_FEDERATED_IDENTITY | jq -r '.id')

         GH_ACTION_FEDERATED_IDENTITY_SP=$(az ad sp create --id $GH_ACTION_FEDERATED_IDENTITY_APP_ID)
         GH_ACTION_FEDERATED_IDENTITY_SP_OBJECT_ID=$(echo $GH_ACTION_FEDERATED_IDENTITY_SP | jq -r '.id')
       ```

       Set environment information

       ```bash
         AZURE_SUBSCRIPTION_ID=$(az account show --query 'id' -o tsv)
         AZURE_RESOURCEGROUP_RESOURCE_ID=$(az group show --name ${RESOURCEGROUP} --query id -o tsv)
       ```

       Create a new role assignment

       ```bash
         # Assign built-in Contributor RBAC role for creating resource groups and performing deployments at the resource group level
         az role assignment create --role contributor --subscription $AZURE_SUBSCRIPTION_ID --assignee-object-id  $GH_ACTION_FEDERATED_IDENTITY_SP_OBJECT_ID --assignee-principal-type ServicePrincipal --scope $AZURE_RESOURCEGROUP_RESOURCE_ID
       ```

   1. Add federated credentials

       First, the ./github-workflow/credential.json file needs to be customized with the repository owner.

       ```bash
          sed -i "s/<repo_owner>/${GITHUB_USER}/g" ../.github/workflows/credential.json
       ```

       Then, the federated credential needs to be created.

       ```bash
         az ad app federated-credential create --id $GH_ACTION_FEDERATED_IDENTITY_OBJECT_ID --parameters ../.github/workflows/credential.json
       ```

   1. Create secrets for AZURE_CLIENT_ID, AZURE_TENANT_ID, and AZURE_SUBSCRIPTION_ID.  
       Use these values from your Microsoft Entra application for your GitHub secrets:

       ```bash
         export TENANT_ID_GHACTION=$(az account show --query tenantId --output tsv)
         gh secret set AZURE_CLIENT_ID -b"${GH_ACTION_FEDERATED_IDENTITY_APP_ID}" --repo $GITHUB_USER/serverless-reference-implementation
         gh secret set AZURE_TENANT_ID -b"${TENANT_ID_GHACTION}" --repo $GITHUB_USER/serverless-reference-implementation
         gh secret set AZURE_SUBSCRIPTION_ID -b"${AZURE_SUBSCRIPTION_ID}" --repo $GITHUB_USER/serverless-reference-implementation
       ```

## Create variables and kickoff first CI/CD run

```
export APIM_GATEWAY_URL=$(az deployment group show \
                                    --resource-group ${RESOURCEGROUP} \
                                    --name azuredeploy-apim \
                                    --query properties.outputs.apimGatewayURL.value \
                                    --output tsv)
export ACESS_TOKEN_SCOPE=${IDENTIFIER_URI}/Status.Read

gh workflow run deploy-clientapp.yaml --ref main -f azureTenantId=$TENANT_ID -f clientAppId=$CLIENT_APP_ID -f apiAppId=$API_APP_ID -f accessTokenScope=$ACESS_TOKEN_SCOPE -f apiURL=$APIM_GATEWAY_URL -f azureStorageAccountName=$STORAGE_ACCOUNT_NAME -f azureCdnName=$CDN_NAME -f resourceGroupName=$RESOURCEGROUP -f githubBranch=main
```

## Monitor the current pipeline execution status

```bash
# monitor until stages are completed
gh run list -L 1
# Get Id and execute 
gh run view <yourId>
# This command will include at the end a command with an id to see more details, it will look like 
# To see the full job log, try: gh run view --log --job=8644697022
# and you could get good information running that: gh run view --log --job=8644697022
```

Wait until the GitHub Action is complete

## Configure Dynamic Site Acceleration

```bash
az cdn endpoint update \
   -g $RESOURCEGROUP \
   --profile-name $CDN_NAME \
   -n $CDN_NAME \
   --set probePath="/semver.txt"
```

## Update the reply URL for the registered application

> If you're using a tenant different to the tenant associated to the subscription, log in to that tenant. You will need to log in the subscription after creating the application to continue the instructions.

```bash
az login --tenant $TENANT_ID --allow-no-subscriptions
```

```bash
az rest --method PATCH --uri 'https://graph.microsoft.com/v1.0/applications/'$CLIENT_APP_OBJECT_ID --headers 'Content-Type=application/json' --body '{"spa":{"redirectUris":["'$CLIENT_URL'"]},web:{implicitGrantSettings:{enableAccessTokenIssuance:true}}}'
```

> Log back into your subscription if you've used a different tenant.

```bash
az login
az account set --subscription <your-subscription-id>
```

## About consent

The first time you use the client application you need to consent to the delegated permissions specified for the application, unless an administrator granted consent for all users in the directory.

See [Types of permissions and consent](https://learn.microsoft.com/entra/identity-platform/permissions-consent-overview) for details.
