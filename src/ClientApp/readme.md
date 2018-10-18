
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
5. Click the **Add** button in the top bar. 
6. Click **Select an API**, search the principal name of the DroneStatus API, and click **Select**.
7. Click **Select permissions**, click the entry **Access [DroneStatus API]** in the **Delegated Permissions** section, and click **Select**.
8. Click the **Done** button.

Update the manifest

1. From the application blade, click **Manifest** to open the inline manifest editor.
2. Search for the `oauth2AllowImplicitFlow` property. Change the value to `true`. This setting enables implicit grant flow, which is disabled by default.
3. Click **Save**.

Get the issuer URL

1. Go back to the **App registrations** blade. Click **Endpoints**.
2. Copy the Federation Metadata Document URL. This URL refers to an XML document.
3. Open a new browser window and navigate to the URL. In the XML document, find the **EntityDescriptor** element.
4. Copy the value of the **entityID** attribute. This attribute is the Issuer URL.

At this point you should have the following values, which you will need later:

- Azure AD tenant name
- Azure AD directory ID
- Application ID
- API Application ID
- Issuer URL

## Update the client app 

1. Open the file src/ClientApp/app.js and locate the setup of the window.config.
2. Update the **tenant** value with the Azure AD tenant name
3. Update the **clientId** value with the Application ID
4. Update the **apiId** value with the API Application ID
4. Update the **apiUrl** value with the URL for the APIM gateway URL for v1

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
