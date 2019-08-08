// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

export const getAdalConfig = () => {
  return {
    instance: 'https://login.microsoftonline.com/',
    tenant: `<Azure AD tenant name>`,
    clientId: `<application id>`,
    postLogoutRedirectUri: window.location.origin,
    apiId: `<api application id>`
  };
}

export const getApiConfig = () =>
{
  return {
    url: `<URL of the API Management API endpoint>`
  };
}