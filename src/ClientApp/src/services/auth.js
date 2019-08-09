// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

let adalModule = null;

export const isBrowser = () => typeof window !== "undefined"

if(isBrowser())
  adalModule = require("./adal")

export const login = () => {
  if(adalModule) 
    adalModule.handleUserLogin()
}

export const isLoggedIn = () => {
  return adalModule 
  ? adalModule.isUserLoggedIn()
  : false;
}

export const getUserName = () => {
  return adalModule 
  ? adalModule.userName()
  : "";
}

export const logout = () => {
  if (adalModule)
    adalModule.handleUserLogout()
}

export const handleLoginCallback = () => {
  if (adalModule)
    adalModule.handleAdalCallback()
}

export const acquireTokenForAPI = callback => {
  return adalModule 
  ? adalModule.handleAuthBearerRequest(callback)
  : "";
}