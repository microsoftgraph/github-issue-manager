# Register an application in Microsoft Entra admin center

1. Open a browser and navigate to the [Microsoft Entra admin center](https://entra.microsoft.com) and login using a Global administrator account.

1. Select **Microsoft Entra ID** in the left-hand navigation, expand **Identity**, expand **Applications**, then select **App registrations**.

1. Select **New registration**. Enter a name for your application, for example, `GitHub Issue Manager Connector`.

1. Set **Supported account types** to **Accounts in this organizational directory only**.

1. Leave **Redirect URI** empty.

1. Select **Register**. On the application's **Overview** page, copy the value of the **Application (client) ID** and **Directory (tenant) ID** and save them, you will need these values in the next step.

1. Select **API permissions** under **Manage**.

1. Remove the default **User.Read** permission under **Configured permissions** by selecting the ellipses (**...**) in its row and selecting **Remove permission**.

1. Select **Add a permission**, then **Microsoft Graph**.

1. Select **Application permissions**.

1. Select **ExternalConnection.ReadWrite.OwnedBy** and **ExternalItem.ReadWrite.OwnedBy**, then select **Add permissions**.

1. Select **Grant admin consent for...**, then select **Yes** to provide admin consent for the selected permission.

1. Select **Certificates and secrets** under **Manage**, then select **New client secret**.

1. Enter a description, choose a duration, and select **Add**.

1. Copy the secret from the **Value** column, you will need it in the next steps.

    > [!IMPORTANT]
    > This client secret is never shown again, so make sure you copy it now.

## Configure sample

1. Make a copy of [./src/local.settings.json.tmpl](/src/local.settings.json.tmpl) named **./src/local.settings.json**.

1. Set the following properties in **./src/local.settings.json**:

    - `GraphOptions:ClientId`: set to the **Application (client) ID** value you copied
    - `GraphOptions:ClientSecret`: set to the client secret you created
    - `GraphOptions:TenantId`: set to the **Directory (tenant) ID** value you copied
