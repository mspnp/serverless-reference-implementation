import { app, HttpRequest, HttpResponseInit, input, InvocationContext } from '@azure/functions';
const CosmosClient = require('@azure/cosmos').CosmosClient

interface IDroneStatus {
    id: number;
    Battery: number;
    FlightMode: number,
    Latitude: number,
    Longitude: number,
    Altitude: number,
    GyrometerOK: boolean,
    AccelerometerOK: boolean,
    MagnetometerOK: boolean
}

async function getDroneStatusFromCosmosDB(deviceId: string): Promise<IDroneStatus> {
    const client = new CosmosClient({ endpoint: process.env.CosmosDBEndpoint, key: process.env.CosmosDBKey });
    const dbResponse = await client.databases.createIfNotExists({
        id: process.env.COSMOSDB_DATABASE_NAME
    });
    const database = dbResponse.database;
    const coResponse = await database.containers.createIfNotExists({
        id: process.env.COSMOSDB_DATABASE_COL
    });
    const container = coResponse.container;
    const { resource } = await container.item(deviceId, deviceId).read();

    return <IDroneStatus>resource;
}

export async function GetStatusFunction(request: HttpRequest, context: InvocationContext): Promise<HttpResponseInit> {
        const authUtils = require('./authorization');
        context.log('Processing getStatus request.');

        const deviceId = request.query.get('deviceId');
        if (!deviceId) {
            context.log('Missing deviceId');
            return { status: 404, body: 'No device id specified in the query string.',  headers: {
                'Content-Type': 'text/plain'
            }};
        }

        const getDeviceStatusRoleName = ['GetStatus'];
        const droneStatus = await getDroneStatusFromCosmosDB(deviceId);
        const result = (authUtils.handleIfAuthorizedForRoles(request, getDeviceStatusRoleName, () => {          
          if (!droneStatus) {
              return {
                  status: 404,
                  body: 'ToDo item not found',
                  headers: {
                    'Content-Type': 'text/plain'
                }
              };
          } else {
              const jsonResponse = JSON.stringify(droneStatus);
              return {
                status: 200, /* Defaults to 200 */
                body: jsonResponse,
                headers: {
                    'Content-Type': 'application/json'
                }
              }
            };
          }, context));   
          return result;    
};

app.http('GetStatusFunction', {
    route: "getstatusfunction",
    methods: ['GET'],
    authLevel: 'anonymous',
    handler: GetStatusFunction
});
