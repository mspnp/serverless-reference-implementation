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

@description('It will be used to assign roles to the user, allowing them to access resources.')
param userObjectId string

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
var eventHubConsumerGroupName = 'dronetelemetry'
var sendEventSourceKeyName = 'send'
var listenEventSourceKeyName = 'listen'
var eventSourceKeyName = 'allinone'

var eventHubsDataReceiverRole = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'a638d3c7-ab3a-418d-83e6-5f17a39d4fde'
) // Event Hubs Data Receiver

var eventHubsDataOwnerRole = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  'f526a384-b230-433a-b45c-95f59c4a2dec'
) // Event Hubs Data Owner

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
    // Enforcing role-based access control as the only authentication method
    disableLocalAuth: true
    locations:[
      {
        locationName: location
        failoverPriority: 0
      }
    ]
  }
}

// Assign Role to allow Read data from cosmos DB
resource cosmosDBDataReaderRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name:guid(resourceGroup().id, cosmosDatabaseAccount.id, 'cosmosDBDataReaderRole')
  parent: cosmosDatabaseAccount
  properties:{
    principalId:  droneStatusFunctionApp.identity.principalId
    roleDefinitionId: '/${subscription().id}/resourceGroups/${resourceGroup().name}/providers/Microsoft.DocumentDB/databaseAccounts/${cosmosDatabaseAccount.name}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000001'
    scope: cosmosDatabaseAccount.id
  }
}

// Assign Role to allow Write data from cosmos DB
resource cosmosDBDataContributorRoleAssignment 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid(resourceGroup().id, cosmosDatabaseAccount.id, 'cosmosDBDataContributorRole')
  parent: cosmosDatabaseAccount
  properties:{
    principalId:  droneTelemetryFunctionApp.identity.principalId
    roleDefinitionId: '/${subscription().id}/resourceGroups/${resourceGroup().name}/providers/Microsoft.DocumentDB/databaseAccounts/${cosmosDatabaseAccount.name}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    scope: cosmosDatabaseAccount.id
  }
}

// Assign Role to allow Read data from cosmos DB
resource cosmosDBDataReaderRoleAssignmentUser 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name:guid(resourceGroup().id, cosmosDatabaseAccount.id, 'cosmosDBDataReaderRoleUser')
  parent: cosmosDatabaseAccount
  properties:{
    principalId:  userObjectId
    roleDefinitionId: '/${subscription().id}/resourceGroups/${resourceGroup().name}/providers/Microsoft.DocumentDB/databaseAccounts/${cosmosDatabaseAccount.name}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000001'
    scope: cosmosDatabaseAccount.id
  }
}

// Assign Role to allow Write data from cosmos DB
resource cosmosDBDataContributorRoleAssignmentUser 'Microsoft.DocumentDB/databaseAccounts/sqlRoleAssignments@2023-04-15' = {
  name: guid(resourceGroup().id, cosmosDatabaseAccount.id, 'cosmosDBDataContributorRoleUser')
  parent: cosmosDatabaseAccount
  properties:{
    principalId:  userObjectId
    roleDefinitionId: '/${subscription().id}/resourceGroups/${resourceGroup().name}/providers/Microsoft.DocumentDB/databaseAccounts/${cosmosDatabaseAccount.name}/sqlRoleDefinitions/00000000-0000-0000-0000-000000000002'
    scope: cosmosDatabaseAccount.id
  }
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
          name: 'COSMOSDB_CONNECTION_STRING__accountEndpoint'
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

// Assign Role receive messages from Event Hub
resource eventHubsDataReceiverRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, eventHubNamespace.id, 'eventHubsDataReceiverRole')
  scope: eventHubNamespace
  properties: {
    roleDefinitionId: eventHubsDataReceiverRole
    principalId: droneTelemetryFunctionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource eventHubsDataReceiverOwnerAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, eventHubNamespace.id, 'eventHubsDataOwnerRole')
  scope: eventHubNamespace
  properties: {
    roleDefinitionId: eventHubsDataOwnerRole
    principalId: droneTelemetryFunctionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource droneTelemetryFunctionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: droneTelemetryFunctionAppName
  location: location
  tags: {
    displayName: 'Drone Telemetry Function App'
  }
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
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
          name: 'COSMOSDB_CONNECTION_STRING__accountEndpoint'
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
          name: 'EventHubConnection__fullyQualifiedNamespace'
          value: '${eventHubNamespace.name}.servicebus.windows.net'
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
  ]
}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2021-12-01-preview' = {
  name: 'law-${appName}'
  location: location
  properties: any({
    retentionInDays: 30
    features: {
      searchVersion: 1
    }
    sku: {
      name: 'PerGB2018'
    }
  })
}

resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: '${eventHubNamespace.name}-diagnostic'
  scope: eventHubNamespace
  properties: {
    logs: [
      {
        category: 'OperationalLogs'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    workspaceId: logAnalytics.id
  }
}

output cosmosDatabaseAccount string = cosmosDatabaseAccountName
output droneStatusFunctionAppName string = droneStatusFunctionAppName
output droneTelemetryFunctionAppName string = droneTelemetryFunctionAppName
