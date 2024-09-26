// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
const isBrowser = () => typeof window !== "undefined";
import { AuthenticationResult } from "@azure/msal-browser";
import { accessTokenScope } from "./config";
const { msalInstance } = !isBrowser()
  ? {
    msalInstance: {
      getAllAccounts: () => {
        return [];
      },
      setActiveAccount: (x: any) => { },
    },
  }
  : require("./msal");

interface Auth {
  login(): Promise<void>;
  isLoggedIn(): Promise<boolean>;
  getUserName(): Promise<string>;
  logout(): Promise<void>;
  acquireTokenForAPI(callback: (err: any, token: string | null) => void): Promise<void>;
}

var loginRequest = {
  scopes: ["user.read", "mail.send", accessTokenScope],
};

var request = {
  scopes: [accessTokenScope],
};

export const auth: Auth = {
  login: async () => {
    if (msalInstance) {
      try {
        await msalInstance.initialize(); // Ensure MSAL is initialized
        const request = await msalInstance.loginPopup(loginRequest);
        msalInstance.setActiveAccount(request.account);
        location.href = window.location.origin;
      } catch (error) {
        console.error(error);
      }
    }
  },
  isLoggedIn: async () => {
    if (msalInstance) {
      try {
        await msalInstance.initialize(); // Ensure MSAL is initialized
        const currentAccounts = msalInstance.getAllAccounts();
        return currentAccounts.length > 0;
      } catch (error) {
        console.error(error);
      }
    }
    return false;
  },
  getUserName: async () => {
    if (msalInstance) {
      try {
        await msalInstance.initialize(); // Ensure MSAL is initialized
        const currentAccounts = await msalInstance.getAllAccounts();
        const name: string =
          currentAccounts.length > 0
            ? currentAccounts[0] && currentAccounts[0].username
            : "";
        return name;
      } catch (error) {
        console.error(error);
      }
    }
    return "";
  },
  logout: async () => {
    if (msalInstance) {
      try {
        await msalInstance.initialize(); // Ensure MSAL is initialized
        const request = await msalInstance.logoutRedirect({
          onRedirectNavigate: (url) => {
            return true;
          },
        })
      } catch (error) {
        console.error(error);
      }
    }
  },
  acquireTokenForAPI: async (func) => {
    if (msalInstance) {
      try {
        await msalInstance.initialize(); // Ensure MSAL is initialized
        const token = await msalInstance.acquireTokenSilent(request);
        func(null, token.accessToken);
      } catch (error) {
        func(error, null);
      }
    }
  }
};
