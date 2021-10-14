import * as msal from "@azure/msal-browser";
import { getMsalConfig } from './config';


const msalConfig = {
    auth: getMsalConfig()
};

export const msalInstance = new msal.PublicClientApplication(msalConfig);
