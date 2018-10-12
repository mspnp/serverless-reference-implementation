module.exports = function (context, req, getStatusById) {
    var authUtils = require('./authorization');
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