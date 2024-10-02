@description('The email address of the owner of the service')
@minLength(1)
param publisherEmail string = 'drones@contoso.com'

@description('The name of the owner of the service')
@minLength(1)
param publisherName string = 'contoso'

@description('The pricing tier of this API Management service')
param sku string = 'Developer'

@description('The instance size of this API Management service.')
param skuCount int = 1

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Function app url')
param functionAppUrlV1 string

@description('Function app name')
param functionAppNameV1 string

@description('Function app code')
@secure()
param functionAppCodeV1 string

@description('Function app url V2')
param functionAppUrlV2 string = ''

@description('Function app name V2')
param functionAppNameV2 string = ''

@description('Function app code V2')
@secure()
param functionAppCodeV2 string = ''

@description('indicates whether subscription is required')
param requireSubscription bool = false

var apiManagementServiceName = '${resourceGroup().name}-apim-${uniqueString(resourceGroup().id)}'
var functionAppResourceIdV1 = resourceId('Microsoft.Web/sites', functionAppNameV1)
var functionAppResourceIdV2 = (empty(functionAppNameV2) ? '' : resourceId('Microsoft.Web/sites', functionAppNameV2))
var xmlJsonEscapedPolicyV1 = '<policies>\r\n    <inbound>\r\n        <base />\r\n        <rewrite-uri template="GetStatusFunction?deviceId={deviceid}" />\r\n        <set-backend-service id="apim-generated-policy" backend-id="dronestatusdotnet" />\r\n   </inbound>\r\n    <backend>\r\n        <forward-request />\r\n</backend>\r\n    <outbound>\r\n        <base />\r\n    </outbound>\r\n    <on-error>\r\n        <base />\r\n    </on-error>\r\n</policies>'
var xmlJsonEscapedPolicyV2 = '<policies>\r\n    <inbound>\r\n        <base />\r\n        <rewrite-uri template="GetStatusFunction?deviceId={deviceid}" />\r\n        <set-backend-service id="apim-generated-policy-v2" backend-id="dronestatusnodejs" />\r\n   </inbound>\r\n    <backend>\r\n        <forward-request />\r\n</backend>\r\n    <outbound>\r\n        <base />\r\n    </outbound>\r\n    <on-error>\r\n        <base />\r\n    </on-error>\r\n</policies>'
var versionSetName = 'dronestatusversionset'
var deployV2 = (empty(functionAppUrlV2) ? 'No' : 'Yes')

resource apiManagementService 'Microsoft.ApiManagement/service@2023-09-01-preview' = {
  name: apiManagementServiceName
  location: location
  tags: {}
  sku: {
    name: sku
    capacity: skuCount
  }
  properties: {
    publisherEmail: publisherEmail
    publisherName: publisherName
    apiVersionConstraint: {
      minApiVersion: '2019-12-01'
    }
  }
}

resource apiManagementServiceNameNamedValueFunctionAppCodeV1 'Microsoft.ApiManagement/service/namedValues@2023-09-01-preview' = {
  parent: apiManagementService
  name: 'getstatusfunctionapp-code'
  properties: {
    displayName: 'getstatusfunctionapp-code'
    tags: [
      'key'
      'function'
      'code'
    ]
    secret: true
    value: functionAppCodeV1
  }
}

resource apiManagementServiceNameNamedValueFunctionAppCodeV2 'Microsoft.ApiManagement/service/namedValues@2023-09-01-preview' = if (deployV2 == 'Yes') {
  parent: apiManagementService
  name: 'getstatusv2functionapp-code'
  properties: {
    tags: [
      'key'
      'function'
      'code'
    ]
    secret: true
    displayName: 'getstatusv2functionapp-code'
    value: functionAppCodeV2
  }
}

resource apiManagementServiceNameDronestatusdotnet 'Microsoft.ApiManagement/service/backends@2023-09-01-preview' = {
  parent: apiManagementService
  name: 'dronestatusdotnet'
  properties: {
    resourceId: 'https://management.azure.com${functionAppResourceIdV1}'
    credentials: {
      query: {
        code: [
          '{{getstatusfunctionapp-code}}'
        ]
      }
    }
    url: functionAppUrlV1
    protocol: 'http'
  }
  dependsOn:[
    apiManagementServiceNameNamedValueFunctionAppCodeV1
  ]
}

resource apiManagementServiceNameDronestatusnodejs 'Microsoft.ApiManagement/service/backends@2023-09-01-preview' = if (deployV2 == 'Yes') {
  parent: apiManagementService
  name: 'dronestatusnodejs'
  properties: {
    resourceId: 'https://management.azure.com${functionAppResourceIdV2}'
    credentials: {
      query: {
        code: [
          '{{getstatusv2functionapp-code}}'
        ]
      }
    }
    url: functionAppUrlV2
    protocol: 'http'
  }
  dependsOn: [
    apiManagementServiceNameNamedValueFunctionAppCodeV2
  ]
}

resource apiVersionSet 'Microsoft.ApiManagement/service/apiVersionSets@2023-09-01-preview' = {
  parent: apiManagementService
  name: versionSetName
  properties: {
    displayName: 'Drone Delivery API'
    versioningScheme: 'Segment'
  }
}

resource apiManagementServiceNameDronedeliveryapiv1 'Microsoft.ApiManagement/service/apis@2023-09-01-preview' = {
  parent: apiManagementService
  name: 'dronedeliveryapiv1'
  properties: {
    displayName: 'Drone Delivery API'
    description: 'Drone Delivery API'
    path: 'api'
    apiVersion: 'v1'
    apiVersionSetId: apiVersionSet.id
    protocols: [
      'https'
    ]
  }
  dependsOn: [
    apiManagementServiceNameDronestatusdotnet
  ]
}

resource apiManagementServiceNameDronedeliveryapiv1DronestatusGET 'Microsoft.ApiManagement/service/apis/operations@2023-09-01-preview' = {
  parent: apiManagementServiceNameDronedeliveryapiv1
  name: 'dronestatusGET'
  properties: {
    displayName: 'Retrieve drone status'
    description: 'Retrieve drone status'
    method: 'GET'
    urlTemplate: '/dronestatus/{deviceid}'
    templateParameters: [
      {
        name: 'deviceid'
        description: 'device id'
        type: 'string'
        required: true
      }
    ]
  }
}

resource apiManagementServiceNameDronedeliveryapiv1DronestatusGET_policy 'Microsoft.ApiManagement/service/apis/operations/policies@2023-09-01-preview' = {
  parent: apiManagementServiceNameDronedeliveryapiv1DronestatusGET
  name: 'policy'
  properties: {
    format: 'xml'
    value: xmlJsonEscapedPolicyV1
  }
}

resource apiManagementServiceNameDronedeliveryapiv2 'Microsoft.ApiManagement/service/apis@2023-09-01-preview' = if (deployV2 == 'Yes') {
  parent: apiManagementService
  name: 'dronedeliveryapiv2'
  properties: {
    displayName: 'Drone Delivery API'
    description: 'Drone Delivery API'
    path: 'api'
    apiVersion: 'v2'
    apiVersionSetId: apiVersionSet.id
    protocols: [
      'https'
    ]
  }
  dependsOn: [
    apiManagementServiceNameDronestatusnodejs
  ]
}

resource apiManagementServiceNameDronedeliveryapiv2_dronestatusGET 'Microsoft.ApiManagement/service/apis/operations@2023-09-01-preview' = if (deployV2 == 'Yes') {
  parent: apiManagementServiceNameDronedeliveryapiv2
  name: 'dronestatusGET'
  properties: {
    displayName: 'Retrieve drone status'
    description: 'Retrieve drone status'
    method: 'GET'
    urlTemplate: '/dronestatus/{deviceid}'
    templateParameters: [
      {
        name: 'deviceid'
        description: 'device id'
        type: 'string'
        required: true
      }
    ]
  }
}

resource apiManagementServiceNameDronedeliveryapiv2DronestatusGETPolicy 'Microsoft.ApiManagement/service/apis/operations/policies@2023-09-01-preview' = if (deployV2 == 'Yes') {
  parent: apiManagementServiceNameDronedeliveryapiv2_dronestatusGET
  name: 'policy'
  properties: {
    format: 'xml'
    value: xmlJsonEscapedPolicyV2
  }
}

resource apiManagementServiceNameDronedeliveryprodapi 'Microsoft.ApiManagement/service/products@2023-09-01-preview' = {
  parent: apiManagementService
  name: 'dronedeliveryprodapi'
  properties: {
    displayName: 'drone delivery product api'
    description: 'drone delivery product api'
    terms: 'terms for example product'
    subscriptionRequired: requireSubscription
    state: 'published'
  }
}

resource apiManagementServiceNameDronedeliveryprodapiDronedeliveryapiv1 'Microsoft.ApiManagement/service/products/apis@2023-09-01-preview' = {
  parent: apiManagementServiceNameDronedeliveryprodapi
  name: 'dronedeliveryapiv1'
  dependsOn: [
    apiManagementServiceNameDronedeliveryapiv1
  ]
}

resource apiManagementServiceNameDronedeliveryprodapiDronedeliveryapiv2 'Microsoft.ApiManagement/service/products/apis@2023-09-01-preview' = if (deployV2 == 'Yes') {
  parent: apiManagementServiceNameDronedeliveryprodapi
  name: 'dronedeliveryapiv2'
  dependsOn: [
    apiManagementServiceNameDronedeliveryapiv2
  ]
}

resource apiManagementServiceName_dronestatususerscustomgroup 'Microsoft.ApiManagement/service/groups@2023-09-01-preview' = {
  parent: apiManagementService
  name: 'dronestatususerscustomgroup'
  properties: {
    displayName: 'drone delivery status user custom group'
    description: 'drone delivery status user custom group'
  }
}

output apimGatewayURL string = apiManagementService.properties.gatewayUrl
output apimGatewayServiceName string = apiManagementServiceName
