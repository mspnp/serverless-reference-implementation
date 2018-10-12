
# Serverless client app

##  Register with your Azure Active Directory tenant

1. Sign in to the [Azure portal](https://portal.azure.com).
2. From the left hand navigation pane, choose **Azure Active Directory**.
3. Optionally, click **Switch directory** and choose the Active Directory tenant where you wish to register your application, or [create a new tenant](https://docs.microsoft.com/azure/active-directory/fundamentals/active-directory-access-create-new-tenant).
4. Click **Properties** and copy the directory ID. You will need this value later.

Register the application

1. Click **App registrations** and choose **New application registration**.
2. Enter a friendly name for the application, for example 'Fabrikam', and select 'Web Application and/or Web API' as the Application Type. For the sign-on URL, you can enter `https://localhost/`. Click **Create** to create the application.
3. Copy the Application ID. You will need this later.
4. Click **Settings** > **Required Permissions**, and click the **Grant Permissions** button in the top bar. Click **Yes** to confirm.

Update the manifest

1. From the application blade, click **Manifest** to open the inline manifest editor.
2. Search for the `oauth2AllowImplicitFlow` property. Change the value to `true`. This setting enables implicit grant flow, which is disabled by default.
3. Define a "GetStatus" role for the app by adding the following entry in the "appRoles" array in the manifest (replacing the placeholder GUID with a new GUID)

    ```json
    {
    "allowedMemberTypes": [
        "User"
    ],
    "description":"Access to device status",
    "displayName":"Get Device Status",
    "id": "[generate a new GUID]",
    "isEnabled":true,
    "value":"GetStatus"
    }
    ```

4. Click **Save**.

Get the issuer URL

1. Go back to the **App registrations** blade. Click **Endpoints**.
2. Copy the Federation Metadata Document URL. This URL refers to an XML document.
3. Open a new browser window and navigate to the URL. In the XML document, find the **EntityDescriptor** element.
4. Copy the value of the **entityID** attribute. This attribute is the Issuer URL.

Assign users

1. Go back to the blade for your Azure AD tenant. Click **Enterprise Applications** and then click on the application name.
2. Click **Users and groups**.
3. Click **Users**, select a user, and click **Select**.

    > Note: If you define more than one App role in the manifest, you can select the user's role. In this case, there is only one role, so the option is grayed out.

4. Click **Assign**.

At this point you should have the following values, which you will need later:

- Azure AD tenant name
- Azure AD directory ID
- Application ID
- Issuer URL

## Update the client app 

1. Open the file src/ClientApp/app.js and locate the setup of the window.config.
2. Update the **tenant** value with the Azure AD tenant name
3. Update the **clientId** value with the Application ID
4. Update the **apiUrl** value with the URL for the APIM gateway URL 

## Deploy to Azure Storage static website hosting

1. Create a v2 Storage account
2. In the Azure Portal, navigate to the storage account and click Static Website.
3. Click **Enabled**.
4. For Index document name, enter "index.html"
5. For Error document path, enter "404.html"
6. Click **Save**.
7. Copy the primary endpoint URL.
8. Navigate to the blob container named $web.
9. Upload the files in the `ClientApp` directory to the container.

See [Static website hosting in Azure Storage](https://docs.microsoft.com/azure/storage/blobs/storage-blob-static-website)

## Update the reply URL

1. In the Azure Portal, navigate to your Azure AD tenant.
2. Click on **App registrations**.
3. View all applications, and select the Fabrikam appliction.
4. Clieck **Settings**/
5. Under **Reply URL**, add the primary endpoint URL for the website, from the previous step.
6. Click **Save**.

## Configure Azure AD authentication in the Function App

1. In the Azure Portal, navigate to your Function App.
2. Select **Platform features**
3. Click **Authentication / Authorization**
4. Toggle App Service Authentication to **On**.
5. Click **Azure Active Directory**.
6. In the **Azure Active Directory Settings** blade, select **Advanced**.
7. Under **Client ID**, paste in the Application ID.
8. Under **Issuer Url**, paste in the issuer URL.
9. Click **OK**.

## Configure the API Management policies

1. In the Azure Portal, navigate to your API Management instance.
2. Click **APIs** and select the GetStatus API.
3. Click **Design**.
4. Click the **&lt;/&gt;** icon next to **Policies**.
5. Paste in the following policy definitions:

    ```xml
    <inbound>
        <base />
        <cors allow-credentials="true">
            <allowed-origins>
                <origin>[Website URL]</origin>
            </allowed-origins>
            <allowed-methods>
                <method>GET</method>
            </allowed-methods>
            <allowed-headers>
                <header>*</header>
            </allowed-headers>
        </cors>
        <validate-jwt header-name="Authorization" failed-validation-httpcode="401" failed-validation-error-message="Unauthorized. Access token is missing or invalid.">
            <openid-config url="https://login.microsoftonline.com/[Azure AD directory ID]/.well-known/openid-configuration" />
            <required-claims>
                <claim name="aud">
                    <value>[Application ID]</value>
                </claim>
            </required-claims>
        </validate-jwt>
    </inbound>
    ```
