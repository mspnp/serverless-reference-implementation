// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

const isBrowser = () => typeof window !== "undefined";
const adal = !isBrowser() ? null : require("./adal");

interface Auth {
  login(): void;
  isLoggedIn(): boolean;
  getUserName(): string;
  logout(): void;
  handleLoginCallback(): void;
  acquireTokenForAPI(callback): void;
}

export const auth: Auth = { 
  login: adal ? adal.handleUserLogin : () => {},
  isLoggedIn: adal ? adal.isUserLoggedIn : () => false,
  getUserName: adal ? adal.userName: () => "",
  logout: adal ? adal.handleUserLogout : () => {},
  handleLoginCallback: adal ? adal.handleAdalCallback : () => {},
  acquireTokenForAPI: adal ? adal.handleAuthBearerRequest : (any) => {}
};