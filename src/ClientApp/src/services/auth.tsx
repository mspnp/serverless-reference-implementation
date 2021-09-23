// ------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------
const isBrowser = () => typeof window !== "undefined";
const { msalInstance } = !isBrowser() ? null : require("./msal");

let accountId = "";
interface Auth {
  login(): void;
  isLoggedIn(): boolean;
  getUserName(): string;
  logout(): void;
  acquireTokenForAPI(callback): void;
}

var loginRequest = {
  scopes: ["user.read", "mail.send", "http://testdronapi/user_impersonation"],
};

var request = {
  scopes: ["http://testdronapi/user_impersonation"],
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
        console.log("acquireTokenForAPI");
        msalInstance
          .acquireTokenSilent(request)
          .then((token) => func(null, token))
          .catch((error) => func(error, null));
      }
    : (any) => {},
};
