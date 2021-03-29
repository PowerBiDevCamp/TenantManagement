using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.PowerBI.Api.Models.Credentials;
using Microsoft.Rest;
using TenantManagement.Models;

namespace TenantManagement.Services {

  public class ClientCredentialResponse {
    public String access_token;
    public String expires_in;
    public String ext_expires_in;
    public String token_type;
  }

  public class EmbeddedReportViewModel {
    public string ReportId;
    public string Name;
    public string EmbedUrl;
    public string Token;
    public string TenantName;
  }

  public class PowerBiTenantDetails : PowerBiTenant {
    public IList<Report> Reports { get; set; }
    public IList<Dataset> Datasets { get; set; }
    public IList<GroupUser> Memebers { get; set; }
  }

  public class PowerBiServiceApi {

    private readonly TenantManagementDbService tenantManagementDbService;
    private readonly IConfiguration Configuration;
    private readonly IWebHostEnvironment Env;

    public PowerBiServiceApi(TenantManagementDbService tenantManagementDbService, IConfiguration configuration, IWebHostEnvironment env) {
      this.tenantManagementDbService = tenantManagementDbService;
      this.Configuration = configuration;
      this.Env = env;
    }

    public string GetAccessToken(PowerBiAppIdentity appIdentity) {

      string TenantId = appIdentity.TenantId;
      string ClientId = appIdentity.ApplicationId;
      string ClientSecret = appIdentity.ClientSecret;

      // construct URL for client credentials flow
      String aadTokenEndpoint = "https://login.microsoftonline.com/" + TenantId + "/oauth2/v2.0/token";

      var postBody = new List<KeyValuePair<string, string>>();
      postBody.Add(new KeyValuePair<string, string>("client_id", ClientId));
      postBody.Add(new KeyValuePair<string, string>("client_info", "1"));
      postBody.Add(new KeyValuePair<string, string>("client_secret", ClientSecret));
      postBody.Add(new KeyValuePair<string, string>("scope", "https://analysis.windows.net/powerbi/api/.default"));
      postBody.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));

      HttpClient client = new HttpClient();
      client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
      var response = client.PostAsync(aadTokenEndpoint, new FormUrlEncodedContent(postBody)).Result;

      // this code might be required to wait a few seconds if application was just created in Azure AD
      int retryCount = 0;
      while (response.StatusCode != HttpStatusCode.OK && retryCount < 10) {
        System.Threading.Thread.Sleep(2000);
        response = client.PostAsync(aadTokenEndpoint, new FormUrlEncodedContent(postBody)).Result;
        retryCount += 1;
      }

      string json = response.Content.ReadAsStringAsync().Result;
      ClientCredentialResponse clientCredential = JsonConvert.DeserializeObject<ClientCredentialResponse>(json);
      return clientCredential.access_token;

    }

    public PowerBIClient GetPowerBiClient(PowerBiAppIdentity appIdentity) {
      string accessToken = GetAccessToken(appIdentity);
      var tokenCredentials = new TokenCredentials(accessToken, "Bearer");
      return new PowerBIClient(new Uri("https://api.powerbi.com/"), tokenCredentials);
    }

    public Dataset GetDataset(PowerBIClient pbiClient, Guid WorkspaceId, string DatasetName) {
      var datasets = pbiClient.Datasets.GetDatasetsInGroup(WorkspaceId).Value;
      foreach (var dataset in datasets) {
        if (dataset.Name.Equals(DatasetName)) {
          return dataset;
        }
      }
      return null;
    }

    public async Task<IList<Group>> GetTenantWorkspaces(PowerBIClient pbiClient) {
      var workspaces = (await pbiClient.Groups.GetGroupsAsync()).Value;
      return workspaces;
    }

    public PowerBiTenant OnboardNewTenant(PowerBiTenant tenant) {

      PowerBiAppIdentity appIdentity = tenant.AppIdentity;

      PowerBIClient pbiClient = this.GetPowerBiClient(appIdentity);

      // create new app workspace
      GroupCreationRequest request = new GroupCreationRequest(tenant.Name);
      Group workspace = pbiClient.Groups.CreateGroup(request);

      tenant.WorkspaceId = workspace.Id.ToString();
      tenant.WorkspaceUrl = "https://app.powerbi.com/groups/" + workspace.Id.ToString() + "/";

      // add user as new workspace admin to make demoing easier
      string adminUser = Configuration["DemoSettings:AdminUser"];
      if (!string.IsNullOrEmpty(adminUser)) {
        pbiClient.Groups.AddGroupUser(workspace.Id, new GroupUser {
          EmailAddress = adminUser,
          GroupUserAccessRight = "Admin"
        });
      }

      // upload sample PBIX file #1
      string pbixPath = this.Env.WebRootPath + @"/PBIX/DatasetTemplate.pbix";
      string importName = "Sales";
      PublishPBIX(pbiClient, workspace.Id, pbixPath, importName);

      Dataset dataset = GetDataset(pbiClient, workspace.Id, importName);

      UpdateMashupParametersRequest req = new UpdateMashupParametersRequest(new List<UpdateMashupParameterDetails>() {
        new UpdateMashupParameterDetails { Name = "DatabaseServer", NewValue = tenant.DatabaseServer },
        new UpdateMashupParameterDetails { Name = "DatabaseName", NewValue = tenant.DatabaseName }
      });

      pbiClient.Datasets.UpdateParametersInGroup(workspace.Id, dataset.Id, req);

      PatchSqlDatasourceCredentials(pbiClient, workspace.Id, dataset.Id, tenant.DatabaseUserName, tenant.DatabaseUserPassword);

      pbiClient.Datasets.RefreshDatasetInGroup(workspace.Id, dataset.Id);

      return tenant;
    }

    public PowerBiTenantDetails GetTenantDetails(PowerBiTenant tenant) {

      PowerBiAppIdentity appIdentity = tenant.AppIdentity;
      PowerBIClient pbiClient = this.GetPowerBiClient(appIdentity);

      return new PowerBiTenantDetails {
        Name = tenant.Name,
        AppIdentity = tenant.AppIdentity,
        DatabaseName = tenant.DatabaseName,
        DatabaseServer = tenant.DatabaseServer,
        DatabaseUserName = tenant.DatabaseUserName,
        DatabaseUserPassword = tenant.DatabaseUserPassword,
        Owner = tenant.Owner,
        WorkspaceId = tenant.WorkspaceId,
        WorkspaceUrl = tenant.WorkspaceUrl,
        Memebers = pbiClient.Groups.GetGroupUsers(new Guid(tenant.WorkspaceId)).Value,
        Datasets = pbiClient.Datasets.GetDatasetsInGroup(new Guid(tenant.WorkspaceId)).Value,
        Reports = pbiClient.Reports.GetReportsInGroup(new Guid(tenant.WorkspaceId)).Value
      };

    }


    public PowerBiTenant CreateAppWorkspace(PowerBIClient pbiClient, PowerBiTenant tenant) {

      // create new app workspace
      GroupCreationRequest request = new GroupCreationRequest(tenant.Name);
      Group workspace = pbiClient.Groups.CreateGroup(request);

      // add user as new workspace admin to make demoing easier
      string adminUser = Configuration["DemoSettings:AdminUser"];
      if (!string.IsNullOrEmpty(adminUser)) {
        pbiClient.Groups.AddGroupUser(workspace.Id, new GroupUser {
          EmailAddress = adminUser,
          GroupUserAccessRight = "Admin"
        });
      }

      tenant.WorkspaceId = workspace.Id.ToString();

      return tenant;
    }

    public void DeleteWorkspace(PowerBiTenant tenant) {
      PowerBIClient pbiClient = this.GetPowerBiClient(tenant.AppIdentity);
      Guid workspaceIdGuid = new Guid(tenant.WorkspaceId);
      pbiClient.Groups.DeleteGroup(workspaceIdGuid);
    }

    public void PublishPBIX(PowerBIClient pbiClient, Guid WorkspaceId, string PbixFilePath, string ImportName) {

      FileStream stream = new FileStream(PbixFilePath, FileMode.Open, FileAccess.Read);

      var import = pbiClient.Imports.PostImportWithFileInGroup(WorkspaceId, stream, ImportName);

      while (import.ImportState != "Succeeded") {
        import = pbiClient.Imports.GetImportInGroup(WorkspaceId, import.Id);
      }

    }

    public void PatchSqlDatasourceCredentials(PowerBIClient pbiClient, Guid WorkspaceId, string DatasetId, string SqlUserName, string SqlUserPassword) {

      var datasources = (pbiClient.Datasets.GetDatasourcesInGroup(WorkspaceId, DatasetId)).Value;

      // find the target SQL datasource
      foreach (var datasource in datasources) {
        if (datasource.DatasourceType.ToLower() == "sql") {
          // get the datasourceId and the gatewayId
          var datasourceId = datasource.DatasourceId;
          var gatewayId = datasource.GatewayId;
          // Create UpdateDatasourceRequest to update Azure SQL datasource credentials
          UpdateDatasourceRequest req = new UpdateDatasourceRequest {
            CredentialDetails = new CredentialDetails(
              new BasicCredentials(SqlUserName, SqlUserPassword),
              PrivacyLevel.None,
              EncryptedConnection.NotEncrypted)
          };
          // Execute Patch command to update Azure SQL datasource credentials
          pbiClient.Gateways.UpdateDatasource((Guid)gatewayId, (Guid)datasourceId, req);
        }
      };

    }

    public async Task<EmbeddedReportViewModel> GetReportEmbeddingData(string AppIdentity, string Tenant) {

      var appIdentity = this.tenantManagementDbService.GetAppIdentity(AppIdentity);
      PowerBIClient pbiClient = GetPowerBiClient(appIdentity);

      var tenant = this.tenantManagementDbService.GetTenant(Tenant);
      Guid workspaceId = new Guid(tenant.WorkspaceId);
      var reports = (await pbiClient.Reports.GetReportsInGroupAsync(workspaceId)).Value;

      var report = reports.Where(report => report.Name.Equals("Sales")).First();

      GenerateTokenRequest generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "View");

      // call to Power BI Service API and pass GenerateTokenRequest object to generate embed token
      string embedToken = pbiClient.Reports.GenerateTokenInGroup(workspaceId, report.Id,
                                                                 generateTokenRequestParameters).Token;

      return new EmbeddedReportViewModel {
        ReportId = report.Id.ToString(),
        Name = report.Name,
        EmbedUrl = report.EmbedUrl,
        Token = embedToken,
        TenantName = Tenant
      };

    }

  }
}