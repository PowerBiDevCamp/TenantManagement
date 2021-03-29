using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TenantManagement.Models;
using TenantManagement.Services;

namespace TenantManagement.Controllers {

  [Authorize]
  public class HomeController : Controller {

    private MicrosoftGraphApi microsoftGraphApi;
    private PowerBiServiceApi powerBiServiceApi;
    private TenantManagementDbService tenantManagementDbService;

    public HomeController(MicrosoftGraphApi microsoftGraphApi, PowerBiServiceApi powerBiServiceApi, TenantManagementDbService tenantManagementDbService) {
      this.microsoftGraphApi = microsoftGraphApi;
      this.powerBiServiceApi = powerBiServiceApi;
      this.tenantManagementDbService = tenantManagementDbService;
    }

    [AllowAnonymous]
    public IActionResult Index() {
      return View();
    }

    public IActionResult AppIdentities() {

      var viewModel = this.tenantManagementDbService.GetAppIdentities();
      return View(viewModel);
    }

    public IActionResult AppIdentity(string appIdentity) {
      var viewModel = this.tenantManagementDbService.GetAppIdentity(appIdentity);
      return View(viewModel);
    }

    public IActionResult DeleteAppIdentity(string applicationObjectId) {
      this.microsoftGraphApi.DeleteAzureAdApplication(applicationObjectId);
      this.tenantManagementDbService.DeleteAppIdentity(applicationObjectId);
      return RedirectToAction("AppIdentities");
    }

    public class CreateAppIdentityModel {
      public string AppIdentityName { get; set; }
    }

    public IActionResult CreateAppIdentity() {
      var model = new CreateAppIdentityModel {
        AppIdentityName = this.tenantManagementDbService.GetNextAppIdentityName()
      };
      return View(model);
    }

    [HttpPost]
    public IActionResult CreateAppIdentity(string AppIdentityName) {
      PowerBiAppIdentity appIdentity = this.microsoftGraphApi.CreateAzureAdApplication(AppIdentityName);
      this.tenantManagementDbService.CreateAppIdentity(appIdentity);
      return RedirectToAction("AppIdentities");
    }

    public async Task<IActionResult> Applications() {
      var model = await microsoftGraphApi.GetApplications();
      return View(model);
    }

    public IActionResult Tenants() {
      var model = tenantManagementDbService.GetTenants();
      return View(model);
    }

    public IActionResult Tenant(string Name) {
      var model = tenantManagementDbService.GetTenant(Name);
      var modelWithDetails = powerBiServiceApi.GetTenantDetails(model);
      return View(modelWithDetails);
    }

    public class OnboardTenantModel {
      public string TenantName { get; set; }
      public string SuggestedDatabase { get; set; }
      public List<SelectListItem> DatabaseOptions { get; set; }
      public string SuggestedAppIdentity { get; set; }
      public List<SelectListItem> AppIdentityOptions { get; set; }
    }

    public IActionResult OnboardTenant() {

      var model = new OnboardTenantModel {
        TenantName = this.tenantManagementDbService.GetNextTenantName(),
        SuggestedAppIdentity = this.tenantManagementDbService.GetNextAppIdentityInPool()?.Name,
        AppIdentityOptions = this.tenantManagementDbService.GetAppIdentities().Select(appIdentitiy => new SelectListItem {
          Text = appIdentitiy.Name,
          Value = appIdentitiy.Name
        }).ToList(),
        SuggestedDatabase = "WingtipSales",
        DatabaseOptions = new List<SelectListItem> {
          new SelectListItem{ Text="AcmeCorpSales", Value="AcmeCorpSales" },
          new SelectListItem{ Text="ContosoSales", Value="ContosoSales" },
          new SelectListItem{ Text="MegaCorpSales", Value="MegaCorpSales" }
        }
      };

      return View(model);
    }

    [HttpPost]
    public IActionResult OnboardTenant(string TenantName, string DatabaseServer, string DatabaseName, string DatabaseUserName, string DatabaseUserPassword, string AppIdentity, string Exclusive) {

      var tenant = new PowerBiTenant {
        Name = TenantName,
        DatabaseServer = DatabaseServer,
        DatabaseName = DatabaseName,
        DatabaseUserName = DatabaseUserName,
        DatabaseUserPassword = DatabaseUserPassword,
      };

      if (Exclusive.Equals("True")) {
        var AppIdentityName = this.tenantManagementDbService.GetNextAppIdentityName();
        PowerBiAppIdentity appIdentity = this.microsoftGraphApi.CreateAzureAdApplication(AppIdentityName);
        appIdentity.Exclusive = true;
        this.tenantManagementDbService.CreateAppIdentity(appIdentity);
        tenant.AppIdentity = appIdentity;
      }
      else {
        tenant.AppIdentity = this.tenantManagementDbService.GetAppIdentity(AppIdentity);
      }

      tenant = this.powerBiServiceApi.OnboardNewTenant(tenant);
      this.tenantManagementDbService.OnboardNewTenant(tenant);

      return RedirectToAction("Tenants");

    }

    public IActionResult DeleteTenant(string TenantName) {
      var tenant = this.tenantManagementDbService.GetTenant(TenantName);
      this.powerBiServiceApi.DeleteWorkspace(tenant);
      this.tenantManagementDbService.DeleteTenant(tenant);
      return RedirectToAction("Tenants");
    }

    public IActionResult Embed(string AppIdentity, string Tenant) {
      var viewModel = this.powerBiServiceApi.GetReportEmbeddingData(AppIdentity, Tenant).Result;
      return View(viewModel);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

  }
}
