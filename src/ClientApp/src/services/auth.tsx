// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
const isBrowser = () => typeof window !== "undefined";
import { accessTokenScope } from "./config";
const { msalInstance } = !isBrowser()
  ? {
      msalInstance: {
        getAllAccounts: () => {
          return [];
        },
        setActiveAccount: (x: any) => {},
      },
    }
  : require("./msal");

interface Auth {
  login(): void;
  isLoggedIn(): boolean;
  getUserName(): string;
  logout(): void;
  acquireTokenForAPI(callback): void;
}

var loginRequest = {
  scopes: ["user.read", "mail.send", accessTokenScope],
};

var request = {
  scopes: [accessTokenScope],
};

export const auth: Auth = {
  login: msalInstance
    ? () =>
        msalInstance
          .loginPopup(loginRequest)
          .then((request) => {
            msalInstance.setActiveAccount(request.account);
            location.href = window.location.origin;
          })
          .catch((error) => console.error(error))
    : () => {},
  isLoggedIn: () => {
    const currentAccounts = msalInstance.getAllAccounts();
    return currentAccounts.length > 0;
  },
  getUserName: () => {
    const currentAccounts = msalInstance.getAllAccounts();
    const name =
      currentAccounts.length > 0
        ? currentAccounts[0] && currentAccounts[0].username
        : "";
    return name;
  },
  logout: msalInstance
    ? () =>
        msalInstance.logoutRedirect({
          onRedirectNavigate: (url) => {
            return true;
          },
        })
    : () => {},
  acquireTokenForAPI: msalInstance
    ? (func) => {
        msalInstance
          .acquireTokenSilent(request)
          .then((token) => func(null, token))
          .catch((error) => func(error, null));
      }
    : (any) => {},
};
