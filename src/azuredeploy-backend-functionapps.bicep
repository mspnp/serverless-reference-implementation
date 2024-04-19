@description('The name for the function app. It must only contain characters and numbers, and be 6 chars long max.')
@maxLength(6)
param appName string

@allowed([
  'Standard_LRS'
  'Standard_ZRS'
  'Standard_GRS'
  'Standard_RAGRS'
])
param storageAccountType string = 'Standard_LRS'


@description('Location to deploy Application Insights')
param appInsightsLocation string = resourceGroup().location

@description('Cosmos DB database name')
param cosmosDatabaseName string

@description('Cosmos DB collection name')
param cosmosDatabaseCollection string

@description('The resources location.')
param location string = resourceGroup().location

var droneStatusStorageAccountName = toLower('${appName}ds${uniqueString(resourceGroup().id)}')
var droneTelemetryStorageAccountName = toLower('${appName}dt${uniqueString(resourceGroup().id)}')
var droneTelemetryDeadLetterStorageQueueAccountName = toLower('${appName}dtq${uniqueString(resourceGroup().id)}')
var appServicePlanName = '${appName}-asp'
var droneStatusFunctionAppName = '${appName}${uniqueString(resourceGroup().id)}-dronestatus'
var droneTelemetryFunctionAppName = '${appName}${uniqueString(resourceGroup().id)}-dronetelemetry'
var droneStatusStorageAccountId = '${resourceGroup().id}/providers/Microsoft.Storage/storageAccounts/${droneStatusStorageAccountName}'
var droneTelemetryStorageAccountId = '${resourceGroup().id}/providers/Microsoft.Storage/storageAccounts/${droneTelemetryStorageAccountName}'
var droneTelemetryDeadLetterStorageQueueAccountId = '${resourceGroup().id}/providers/Microsoft.Storage/storageAccounts/${droneTelemetryDeadLetterStorageQueueAccountName}'
var droneStatusAppInsightsName = '${appName}${uniqueString(resourceGroup().id)}-ds-ai'
var droneTelemetryAppInsightsName = '${appName}${uniqueString(resourceGroup().id)}-dt-ai'
var cosmosDatabaseAccountName = toLower('${appName}${uniqueString(resourceGroup().id)}')
var eventHubNameSpaceName = '${appName}${uniqueString(resourceGroup().id)}-ns'
var eventHubName = '${appName}-eh'
var eventHubId = 'Microsoft.EventHub/namespaces/${eventHubNameSpaceName}/EventHubs/${eventHubName}'
var eventHubConsumerGroupName = 'dronetelemetry'
var sendEventSourceKeyName = 'send'
var listenEventSourceKeyName = 'listen'
var eventSourceKeyName = 'allinone'

resource droneStatusStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: droneStatusStorageAccountName
  location: location
  sku: {
    name: storageAccountType
  }
  tags: {
    displayName: 'Drone Status Function App '
  }
  kind: 'Storage'
  properties: {
    supportsHttpsTrafficOnly: true
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

resource droneTelemetryStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: droneTelemetryStorageAccountName
  location: location
  sku: {
    name: storageAccountType
  }
  tags: {
    displayName: 'Drone Telemetry Function App Storage'
  }
  kind: 'Storage'
  properties: {
    supportsHttpsTrafficOnly: true
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

resource droneTelemetryDeadLetterStorageQueueAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: droneTelemetryDeadLetterStorageQueueAccountName
  location: location
  sku: {
    name: storageAccountType
  }
  tags: {
    displayName: 'Drone Telemetry Function App Storage'
  }
  kind: 'Storage'
  properties: {
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        blob: {
          enabled: true
        }
        queue: {
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
  }
  dependsOn: []
}

resource droneStatusAppInsights 'Microsoft.Insights/components@2020-02-02' = {
  kind: 'other'
  name: droneStatusAppInsightsName
  location: appInsightsLocation
  tags: {}
  properties: {
    Application_Type: 'web'
  }
  dependsOn: []
}

resource droneTelemetryAppInsights 'Microsoft.Insights/components@2020-02-02' = {
  kind: 'other'
  name: droneTelemetryAppInsightsName
  location: appInsightsLocation
  tags: {}
  properties: {
    Application_Type: 'web'
  }
  dependsOn: []
}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
    size: 'Y1'
  }
  properties: { }
}

resource cosmosDatabaseAccount 'Microsoft.DocumentDB/databaseAccounts@2024-02-15-preview' = {
  name: cosmosDatabaseAccountName
  location: location
  tags: {
    displayName: 'cosmosDB'
  }
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations:[
      {
        locationName: location
        failoverPriority: 0
      }
    ]
  }
}

resource droneStatusFunctionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: droneStatusFunctionAppName
  location: location
  tags: {
    displayName: 'Drone Status Function App'
  }
  kind: 'functionapp'
  properties: {
    enabled: true
    serverFarmId: appServicePlan.id
    httpsOnly: true
    redundancyMode: 'None'
    publicNetworkAccess: 'Enabled'
    keyVaultReferenceIdentity: 'SystemAssigned'
    siteConfig: {
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
          name: 'COSMOSDB_CONNECTION_STRING'
          value: 'AccountEndpoint=${cosmosDatabaseAccount.properties.documentEndpoint};AccountKey=${listKeys(cosmosDatabaseAccount.id,'2015-04-08').primaryMasterKey};'
        }
        {
          name: 'CosmosDBEndpoint'
          value: cosmosDatabaseAccount.properties.documentEndpoint
        }
        {
          name: 'CosmosDBKey'
          value: listKeys(cosmosDatabaseAccount.id, '2015-04-08').primaryMasterKey
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
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED'
          value: '1'
        }
      ]
    }
  }
  dependsOn: [
    droneStatusStorageAccount
  ]
}

resource droneStatusFunctionAppName_slot 'Microsoft.Web/sites/slots@2022-09-01' = {
  parent: droneStatusFunctionApp
  kind: 'functionapp'
  name: 'slotName'
  location: location
  properties: {
    enabled: true
    reserved: false
    clientAffinityEnabled: true
    clientCertEnabled: false
    hostNamesDisabled: false
    dailyMemoryTimeQuota: 0
    cloningInfo: null
  }
  dependsOn: [
    appServicePlan
  ]
}

resource eventHubNamespace 'Microsoft.EventHub/namespaces@2022-10-01-preview' = {
  name: eventHubNameSpaceName
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    isAutoInflateEnabled: true
    maximumThroughputUnits: 20
  }
  
  resource eventHub 'eventHubs' = {
    name: eventHubName
    properties: {
      messageRetentionInDays: 1
      partitionCount: 4
    }

    resource eventHubAuthorization 'AuthorizationRules@2022-10-01-preview' = {
      name: eventSourceKeyName
      properties: {
        rights: [
          'Listen'
          'Send'
          'Manage'
        ]
      }
    }

    resource sendEventHubAuthorization 'AuthorizationRules@2022-10-01-preview' = {
      name: sendEventSourceKeyName
      properties: {
        rights: [
          'Send'
        ]
      }
    }

    resource listenEventHubAuthorization 'AuthorizationRules@2022-10-01-preview' = {
      name: listenEventSourceKeyName
      properties: {
        rights: [
          'Listen'
        ]
      }
    }

    resource eventHubConsumerGroup 'ConsumerGroups@2022-10-01-preview' = {
      name: eventHubConsumerGroupName
    }
  }
}

resource droneTelemetryFunctionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: droneTelemetryFunctionAppName
  location: location
  tags: {
    displayName: 'Drone Telemetry Function App'
  }
  kind: 'functionapp'
  properties: {
    enabled: true
    serverFarmId: appServicePlan.id
    httpsOnly: true
    redundancyMode: 'None'
    publicNetworkAccess: 'Enabled'
    keyVaultReferenceIdentity: 'SystemAssigned'
    siteConfig: {
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
          value: 'DefaultEndpointsProtocol=https;AccountName=${droneTelemetryStorageAccountName};AccountKey=${listKeys(droneTelemetryStorageAccountId,'2015-05-01-preview').key1};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${droneTelemetryStorageAccountName};AccountKey=${listKeys(droneTelemetryStorageAccountId,'2015-05-01-preview').key1};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: toLower(droneTelemetryFunctionAppName)
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: droneTelemetryAppInsights.properties.InstrumentationKey
        }
        {
          name: 'COSMOSDB_CONNECTION_STRING'
          value: 'AccountEndpoint=${cosmosDatabaseAccount.properties.documentEndpoint};AccountKey=${listKeys(cosmosDatabaseAccount.id,'2015-04-08').primaryMasterKey};'
        }
        {
          name: 'CosmosDBEndpoint'
          value: cosmosDatabaseAccount.properties.documentEndpoint
        }
        {
          name: 'CosmosDBKey'
          value: listKeys(cosmosDatabaseAccount.id, '2015-04-08').primaryMasterKey
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
          name: 'EventHubConnection'
          value: listKeys('${eventHubId}/authorizationRules/listen/', '2017-04-01').primaryConnectionString
        }
        {
          name: 'EventHubConsumerGroup'
          value: eventHubConsumerGroupName
        }
        {
          name: 'EventHubName'
          value: eventHubName
        }
        {
          name: 'DeadLetterStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${droneTelemetryDeadLetterStorageQueueAccountName};AccountKey=${listKeys(droneTelemetryDeadLetterStorageQueueAccountId,'2015-05-01-preview').key1};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'WEBSITE_USE_PLACEHOLDER_DOTNETISOLATED'
          value: '1'
        }
      ]
    }
  }
  dependsOn: [
    droneTelemetryStorageAccount
    eventHubNamespace
  ]
}

output cosmosDatabaseAccount string = cosmosDatabaseAccountName
output droneStatusFunctionAppName string = droneStatusFunctionAppName
output droneTelemetryFunctionAppName string = droneTelemetryFunctionAppName
