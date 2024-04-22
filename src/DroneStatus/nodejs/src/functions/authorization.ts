'use strict';

const clientPrincipalHeaderKey = 'X-MS-CLIENT-PRINCIPAL';

function handleIfAuthorizedForRoles(req, roles, handler, logger) {
    return handleIfAuthorizedByClaims(req, claims => {
        const principalRoles = claims.filter(obj => obj.typ === "roles").map(roles => roles.val);
        const missingRoles = roles.filter(r => principalRoles.indexOf(r) < 0);
        if (missingRoles.length > 0) {
            logger.warn('The principal does not have the required roles %s', missingRoles.join(', '));
            return false;
        }

        return true;
    }, handler, logger);
};

function handleIfAuthorizedByClaims(req, authorizeClaims, handler, logger) {
    return getResultIfUnauthorized(req, authorizeClaims, logger) || handler();
};

function getResultIfUnauthorized(req, authorizeClaims, logger) {
    const principal = req.headers[clientPrincipalHeaderKey.toLocaleLowerCase()];
    if (!principal) {
        logger.error('The request does not contain the required header %s', clientPrincipalHeaderKey);
        return { status: 401, body: 'Unauthorized',
        headers: {
          'Content-Type': 'text/plain'
      }}
    }

    let claims = undefined;
    try {
        const token = Buffer.from(principal.toString(), 'base64').toString('ascii');
        claims = JSON.parse(token)['claims'];
    } catch (error) {
        logger.error('The value of header %s does not contain the expected information', clientPrincipalHeaderKey);
        return { status: 401, body: 'Unauthorized',
        headers: {
          'Content-Type': 'text/plain'
      } }
    }

    return authorizeClaims(claims, logger) ? null : { status: 401, body: 'Unauthorized'  ,
    headers: {
      'Content-Type': 'text/plain'
    }};
};

function createPrincipalWithRoles(roles) {
    const token = { claims: roles.map(role => ({ typ: 'roles', val: role })) };
    return Buffer.from(JSON.stringify(token)).toString('base64');
};

module.exports = {
    handleIfAuthorizedForRoles: handleIfAuthorizedForRoles,
    handleIfAuthorizedByClaims: handleIfAuthorizedByClaims,
    getResultIfUnauthorized: getResultIfUnauthorized,
    createPrincipalWithRoles: createPrincipalWithRoles
};