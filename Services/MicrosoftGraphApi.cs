using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using Newtonsoft.Json;
using TenantManagement.Models;

namespace TenantManagement.Services {

  public class MicrosoftGraphApi {

    private ITokenAcquisition tokenAcquisition { get; }

    public MicrosoftGraphApi(IConfiguration configuration, ITokenAcquisition tokenAcquisition) {
      this.tokenAcquisition = tokenAcquisition;
    }

    public static readonly string[] RequiredAdminScopes = new string[] {
        "Application.ReadWrite.All",
        "Group.ReadWrite.All"
    };

    public string GetAccessToken() {
      return this.tokenAcquisition.GetAccessTokenForUserAsync(RequiredAdminScopes).Result;
    }

    public GraphServiceClient GetGraphServiceClient() {
      return new GraphServiceClient(
        new DelegateAuthenticationProvider((requestMessage) => {
          requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", GetAccessToken());
          return Task.CompletedTask;
        }));
    }

    public async Task<IList<Application>> GetApplications() {
      var graphClient = GetGraphServiceClient();
      var apps = await graphClient.Applications.Request().GetAsync();
      return apps.CurrentPage.OrderBy(app => app.DisplayName).ToList();
    }

    public PowerBiAppIdentity CreateAzureAdApplication(string DisplayName) {

      var graphClient = GetGraphServiceClient();

      var application = new Application {
        DisplayName = DisplayName,
      };

      application = graphClient.Applications.Request().AddAsync(application).Result;

      var passwordCredential = new PasswordCredential {
        StartDateTime = System.DateTime.Now,
        EndDateTime = System.DateTime.Now.AddYears(1),
        KeyId = System.Guid.NewGuid()
      };

      var password = graphClient.Applications[application.Id].AddPassword(passwordCredential).Request().PostAsync().Result;
      string clientSecret = password.SecretText;

      var servicePrincipal = new ServicePrincipal {
        AppId = application.AppId
      };

      servicePrincipal = graphClient.ServicePrincipals.Request().AddAsync(servicePrincipal).Result;

      var pbiAppGroup = graphClient.Groups.Request().Filter("displayName eq 'Power BI Apps'").GetAsync().Result.First();

      graphClient.Groups[pbiAppGroup.Id].Members.References.Request().AddAsync(new DirectoryObject {
        Id = servicePrincipal.Id,
      }).Wait();

      return new PowerBiAppIdentity {
        ApplicationObjectId = application.Id,
        ApplicationId = application.AppId,
        Name = DisplayName,
        ClientSecret = clientSecret,
        ServicePrincipalId = servicePrincipal.Id,
        TenantId = servicePrincipal.AppOwnerOrganizationId.ToString(),
        Exclusive = false
      };

    }

    public void DeleteAzureAdApplication(string ApplicationObjectId) {
      var graphClient = GetGraphServiceClient();
      graphClient.Applications[ApplicationObjectId].Request().DeleteAsync();
    }

  }
}
