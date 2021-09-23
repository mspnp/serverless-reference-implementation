// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
export const getMsalConfig = () => {
  return {
    clientId: `${process.env.AZURE_CLIENT_ID}`,
    authority: `https://login.microsoftonline.com/${process.env.AZURE_TENANT_ID}`,
    postLogoutRedirectUri: window.location.origin
  }
}

export const getApiConfig = () => {
  return {
    url:  `${process.env.AZURE_API_URL}`,
    version: `/v1`
  };
}