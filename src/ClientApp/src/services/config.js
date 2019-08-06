// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

export const getAdalConfig = () => {
  return {
    instance: 'https://login.microsoftonline.com/',
    tenant: ``,
    clientId: ``,
    postLogoutRedirectUri: window.location.origin,
    apiId: ``
  };
}

export const getApiConfig = () =>
{
  return {
    url: ``
  };
}