using System.Collections.Generic;
using System.Linq;
using TenantManagement.Models;

namespace TenantManagement.Services {

  public class TenantManagementDbService {

    private readonly TenantManagementDB dbContext;

    public TenantManagementDbService(TenantManagementDB context) {
      dbContext = context;
    }

    public void CreateAppIdentity(PowerBiAppIdentity appIdentity) {
      dbContext.AppIdentities.Add(appIdentity);
      dbContext.SaveChanges();
    }

    public IList<PowerBiAppIdentity> GetAppIdentities() {

      // get app identity
      var appIdentities = dbContext.AppIdentities
                       .Select(appIdentity => appIdentity)
                       .OrderBy(appIdentity => appIdentity.Name).ToList();

      // populate Tenants collection
      foreach (var appIdentity in appIdentities) {
        appIdentity.Tenants = dbContext.Tenants.Where(tenant => tenant.Owner == appIdentity.Name).ToList();
      }

      return appIdentities;
    }

    public PowerBiAppIdentity GetAppIdentity(string appIdentityName) {
      var appIdentity = dbContext.AppIdentities.Where(appIdentity => appIdentity.Name == appIdentityName).First();
      appIdentity.Tenants = dbContext.Tenants.Where(tenant => tenant.Owner == appIdentityName).ToList();
      return appIdentity;
    }

    public void DeleteAppIdentity(string applicationObjectId) {
      var appId = dbContext.AppIdentities.Where(appIdentity => appIdentity.ApplicationObjectId == applicationObjectId).First();
      dbContext.AppIdentities.Remove(appId);
      dbContext.SaveChanges();
      return;
    }

    public string GetNextAppIdentityName() {
      var appNames = dbContext.AppIdentities.Select(appIdentity => appIdentity.Name).ToList();
      string baseName = "ServicePrincipal";
      string nextName;
      int counter = 0;
      do {
        counter += 1;
        nextName = baseName + counter.ToString("00");
      }
      while (appNames.Contains(nextName));
      return nextName;
    }

    public string GetNextTenantName() {
      var appNames = dbContext.Tenants.Select(tenant => tenant.Name).ToList();
      string baseName = "Tenant";
      string nextName;
      int counter = 0;
      do {
        counter += 1;
        nextName = baseName + counter.ToString("00");
      }
      while (appNames.Contains(nextName));
      return nextName;
    }

    public PowerBiAppIdentity GetNextAppIdentityInPool() {
      var appIdentities = GetAppIdentities().Where(appIdentity => appIdentity.Exclusive == false);
      if (appIdentities.Count() == 0) {
        return null;
      }
      IList<int> counts = appIdentities.Select(appIdentity => appIdentity.Tenants.Count()).ToList();
      int minCount = counts.Min();
      return appIdentities.Where(appIdentity => appIdentity.Tenants.Count() == minCount).First();
    }

    public void OnboardNewTenant(PowerBiTenant tenant) {
      dbContext.Tenants.Add(tenant);
      dbContext.SaveChanges();
    }

    public IList<PowerBiTenant> GetTenants() {
      return dbContext.Tenants
             .Select(tenant => tenant).OrderBy(tenant => tenant.AppIdentity)
             .OrderBy(tenant => tenant.Name).ToList();
    }

    public PowerBiTenant GetTenant(string TenantName) {
      var tenant = dbContext.Tenants.Where(tenant => tenant.Name == TenantName).First();
      tenant.AppIdentity = dbContext.AppIdentities.Where(appIdentity => appIdentity.Name == tenant.Owner).First();
      return tenant;
    }

    public void DeleteTenant(PowerBiTenant tenant) {
      dbContext.Tenants.Remove(tenant);
      dbContext.SaveChanges();
      return;
    }

  }
}
