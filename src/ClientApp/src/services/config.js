// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

export const getAdalConfig = () => {
  return {
    instance: `https://login.microsoftonline.com/`,
    tenant: `${process.env.AZURE_TENANT_ID}`,
    clientId: `${process.env.AZURE_CLIENT_ID}`,
    postLogoutRedirectUri: window.location.origin,
    apiId: `${process.env.AZURE_API_CLIENT_ID}`
  };
}

export const getApiConfig = () =>
{
  return {
    url: `${process.env.AZURE_API_URL}`,
    version: `/v1`
  };
}