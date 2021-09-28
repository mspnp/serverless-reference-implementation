import { AzureFunction, Context, HttpRequest } from "@azure/functions"

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

const httpTrigger: AzureFunction = async function (context: Context, req: HttpRequest, getStatusById: IDroneStatus): Promise<void> {
    const authUtils = require('./authorization');
    context.log('Processing getStatus request.');

    if (!req.query['deviceId']) {
        context.log('Missing deviceId');
        context.res = {
            status: 404,
            body: { error: 'No device id specified in the query string.' }
        };

        context.done();
    }

    const getDeviceStatusRoleName = ['GetStatus'];
    context.res = (authUtils.handleIfAuthorizedForRoles(req, getDeviceStatusRoleName, () => {
        if (!getStatusById) {
            context.log('Device NOT found');
            return {
                status: 400,
                body: { error: 'No data found for device id: ' + req.query.id }
            };
        }
        else {
            const jsonResponse = JSON.stringify(getStatusById);
            context.log("Device found, response: " + jsonResponse);
            return {
                status: 200, /* Defaults to 200 */
                body: { body: jsonResponse },
                headers: {
                    'Content-Type': 'application/json'
                }
            };
        }
    }, context.log));

    context.done();
};

export default httpTrigger;