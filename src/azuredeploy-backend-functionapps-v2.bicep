@description('The name for the function app. It must only contain characters and numbers, and be 6 chars long max.')
@maxLength(6)
param appName string

@description('The name for the cosmos db account name.')
param cosmosDatabaseAccountName string

@description('The name for the cosmos db database name.')
param cosmosDatabaseName string

@description('The name for the cosmos db collection name.')
param cosmosDatabaseCollection string

@allowed([
  'Standard_LRS'
  'Standard_ZRS'
  'Standard_GRS'
  'Standard_RAGRS'
])
param storageAccountType string = 'Standard_LRS'

@description('The resources location.')
param location string = resourceGroup().location

var droneStatusStorageAccountName = toLower('${appName}dsv2${uniqueString(resourceGroup().id)}')
var droneStatusStorageAccountId = '${resourceGroup().id}/providers/Microsoft.Storage/storageAccounts/${droneStatusStorageAccountName}'
var hostingPlanName = '${appName}-asp'
var droneStatusFunctionAppName = '${appName}-dsv2-funcapp'
var droneStatusAppInsightsName = '${appName}${uniqueString(resourceGroup().id)}-dsv2-ai'

resource droneStatusStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: droneStatusStorageAccountName
  location: location
  sku: {
    name: storageAccountType
  }
  tags: {
    displayName: 'Drone Status Function App'
  }
  kind: 'StorageV2'
  properties: {
    accessTier: 'Hot'
    supportsHttpsTrafficOnly: true
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    encryption: {
      services: {
        blob: {
          enabled: true
        }
        file: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
  dependsOn: []
}

resource droneStatusAppInsights 'microsoft.insights/components@2020-02-02' = {
  kind: 'other'
  name: droneStatusAppInsightsName
  location: location
  tags: {}
  properties: {
    Application_Type: 'web'
  }
  dependsOn: []
}

resource hostingPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
    size: 'Y1'
  }
  properties: { }
}

resource cosmosDatabaseAccount 'Microsoft.DocumentDB/databaseAccounts@2024-02-15-preview' existing = {
  name: cosmosDatabaseAccountName
}

resource droneStatusFunctionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: droneStatusFunctionAppName
  location: location
  tags: {
    displayName: 'Drone Status Function App'
  }
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      nodeVersion: 'NODE|18-lts'
      netFrameworkVersion: 'v8.0'
      numberOfWorkers: 1
      alwaysOn: false
      http20Enabled: false
      functionAppScaleLimit: 200
      minimumElasticInstanceCount: 0
      use32BitWorkerProcess: false
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${droneStatusStorageAccountName};AccountKey=${listKeys(droneStatusStorageAccountId,'2015-05-01-preview').key1};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${droneStatusStorageAccountName};AccountKey=${listKeys(droneStatusStorageAccountId,'2015-05-01-preview').key1};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(droneStatusFunctionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: droneStatusAppInsights.properties.InstrumentationKey
        }
        {
          name: 'CosmosDBEndpoint'
          value: cosmosDatabaseAccount.properties.documentEndpoint
        }
        {
          name: 'COSMOSDB_DATABASE_NAME'
          value: cosmosDatabaseName
        }
        {
          name: 'COSMOSDB_DATABASE_COL'
          value: cosmosDatabaseCollection
        }
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '~18'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'node'
        }
      ]
    }
  }
  dependsOn: [
    droneStatusStorageAccount
  ]
}

// Assign Role to allow Read data from cosmos DB
resource cosmosDBDataReaderRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid(resourceGroup().id, cosmosDatabaseAccount.id, 'cosmosDBDataReaderRoleV2')
  parent: cosmosDatabaseAccount
  properties: {
    principalId: droneStatusFunctionApp.identity.principalId
    roleDefinitionId: '/${subscription().id}/resourceGroups/${resourceGroup().name}/providers/Microsoft.DocumentDB/databaseAccounts/${cosmosDatabaseAccount.name}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000001'
    scope: cosmosDatabaseAccount.id
  }
}
