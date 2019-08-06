// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

import { getAdalConfig } from './config';
import AuthenticationContext from 'adal-angular'

const logErrorMessage = (msg) => {
  // TODO send to AppInsights
  console.log(msg);
}

let authContext = new AuthenticationContext(getAdalConfig()); 

const getUser = () => authContext.getCachedUser();

export const isUserLoggedIn = () => getUser() ? true : false;
export const userName = () => getUser().userName;
export const handleUserLogin = () => authContext.login();
export const handleUserLogout = () => authContext.logOut();
export const handleAdalCallback = () => {
  var isCallback = authContext.isCallback(window.location.hash);
  authContext.handleWindowCallback();
  
  if (isCallback && !authContext.getLoginError()) {
      window.location = authContext._getItem(authContext.CONSTANTS.STORAGE.LOGIN_REQUEST);
  }
}
export const handleAuthBearerRequest = callback => {
  authContext.acquireToken(authContext.config.apiId, function (error, token) {
    
    if (error || !token) {
      logErrorMessage('error occurred: ' + error);
    }

    callback(error, token);
  });
}