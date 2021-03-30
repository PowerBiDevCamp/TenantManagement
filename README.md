# The Tenant Management Application

Follow the steps in this document to create a new Azure AD application
for the
[TenantManagement](https://github.com/PowerBiDevCamp/TenantManagement/tree/main/TenantManagement)
sample project. To complete these steps, you will require a Microsoft
365 tenant in which you have permissions to create and manage Azure AD
applications and security groups. You will also need Power BI Service
administrator permissions to configure Power BI settings to give service
principals to ability to access the Power BI Service API. If you do not
have a Microsoft 365 environment for testing, you can create one for
free by following the steps in [Create a Development Environment for
Power BI
Embedding](https://github.com/PowerBiDevCamp/Camp-Sessions/raw/master/Create%20Power%20BI%20Development%20Environment.pdf).

## Create the Azure AD Group named Power BI Apps

Begin by navigating to the [Groups management
page](https://portal.azure.com/#blade/Microsoft_AAD_IAM/GroupsManagementMenuBlade/AllGroups)
in the Azure portal. Once you get to the **Groups** page in the Azure
portal, click the **New group** link.

<img src="Images\ReadMe\media\image1.png" style="width:6.5in;height:2.16667in" />

Xxxx

<img src="Images\ReadMe\media\image2.png" style="width:6.5in;height:3.01667in" />

X

## Configure Power BI Tenant Level Settings for Service Principal Access

Xxx

https://app.powerbi.com/admin-portal

xxx

<img src="Images\ReadMe\media\image3.png" style="width:4.38958in;height:2.87986in" alt="Graphical user interface, application Description automatically generated" />

X

<img src="Images\ReadMe\media\image4.png" style="width:4.89444in;height:3.01736in" alt="Graphical user interface, application Description automatically generated" />

X

<img src="Images\ReadMe\media\image5.png" style="width:4.69514in;height:2.27986in" alt="Graphical user interface, text, application, email Description automatically generated" />

X

<img src="Images\ReadMe\media\image6.png" style="width:6.5in;height:4.28333in" />

X

x

<img src="Images\ReadMe\media\image7.png" style="width:5.19103in;height:0.92992in" alt="Text Description automatically generated with medium confidence" />

X

<img src="Images\ReadMe\media\image8.png" style="width:6.49722in;height:3.25139in" />

X

<img src="Images\ReadMe\media\image9.png" style="width:6.49097in;height:3.27014in" />

X

<img src="Images\ReadMe\media\image10.png" style="width:6.49097in;height:4.00625in" />

X

## Create the Azure AD Application for the TenantManagement Application

When you login to the Azure portal to create the new Azure AD
application, make sure you log in using a user account in the same
tenant which contains the Power BI reports you'd like to embed. Begin by
navigating to the [App
registration](https://portal.azure.com/#blade/Microsoft_AAD_IAM/ActiveDirectoryMenuBlade/RegisteredApps)
page in the Azure portal and click the **New registration** link.

<img src="Images\ReadMe\media\image11.png" style="width:6.49722in;height:1.80972in" />

On the **Register an application** page, enter an application name such
as **Power BI Tenant Management Application** and accept the default
selection for **Supported account types** of **Accounts in this
organizational directory only**.

<img src="Images\ReadMe\media\image12.png" style="width:6.49722in;height:2.28194in" />

In the **Redirect URI** section leave the default selection of **Web**
in the dropdown box. In the textbox to the right of the dropdown, enter
a Redirect URI of **https://localhost:44300/signin-oidc**. Click the
**Register** button to create the new Azure AD application.

<img src="Images\ReadMe\media\image13.png" style="width:6.49722in;height:1.82222in" />

After creating a new Azure AD application in the Azure portal, you
should see the Azure AD application overview page which displays the
**Application ID**. Note that the ***Application ID*** is often called
the ***Client ID***, so don't let this confuse you. You will need to
copy this Application ID and store it so you can use it later to
configure the project's support for Client Credentials Flow.

<img src="Images\ReadMe\media\image14.png" style="width:6.49722in;height:2.55208in" />

Copy the **Client ID** (aka Application ID) and paste it into a text
document so you can use it later in the setup process. Note that this is
the **Client ID** value that will be used by **TenantManagement**
project to authenticate users.

<img src="Images\ReadMe\media\image15.png" style="width:6.46597in;height:1.83403in" />

Next, repeat the same step by copying the **Tenant ID** and copying that
into the text document as well.

<img src="Images\ReadMe\media\image16.png" style="width:6.27639in;height:1.77292in" />

Your text document should now contain the **Client ID** and **Tenant
ID** as shown in the following screenshot.

<img src="Images\ReadMe\media\image17.png" style="width:6.12292in;height:2.50903in" />

Next, you need to create a Client Secret for the application. Click on
the **Certificates & secrets** link in the left navigation to move to
the **Certificates & secrets** page. On the **Certificates & secrets**
page, click the **New client secret** button as shown in the following
screenshot.

<img src="Images\ReadMe\media\image18.png" style="width:6.49722in;height:3.52778in" />

In the **Add a client secret** dialog, add a text description such as
**Test Secret** and then click the **Add** button to create the new
Client Secret.

<img src="Images\ReadMe\media\image19.png" style="width:6.49722in;height:2.95694in" />

Once you have created the Client Secret, you should be able to see its
**Value** in the **Client secrets** section. Click on the **Copy to
clipboard** button to copy the Client Secret into the clipboard.

<img src="Images\ReadMe\media\image20.png" style="width:6.49097in;height:1.80347in" />

Paste the **Client Secret** into the same text document with the
**Client ID** and **Tenant ID**.

<img src="Images\ReadMe\media\image21.png" style="width:6.49722in;height:3.22708in" />

## More

Xxxx

<img src="Images\ReadMe\media\image22.png" style="width:6.49722in;height:2.25764in" />

<img src="Images\ReadMe\media\image23.png" style="width:3.79722in;height:5.325in" />
