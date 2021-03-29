using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TenantManagement.Models {

  public class PowerBiAppIdentity {
    [Key]
    public string Name { get; set; }
    public string ApplicationId { get; set; }
    public string ApplicationObjectId { get; set; }
    public string ServicePrincipalId { get; set; }
    public string ClientSecret { get; set; }
    public string TenantId { get; set; }
    public bool Exclusive { get; set; }
    public virtual List<PowerBiTenant> Tenants { get; set; }
  }

  public class PowerBiTenant {
    [Key]
    public string Name { get; set; }
    public string WorkspaceId { get; set; }
    public string WorkspaceUrl { get; set; }
    public string DatabaseServer { get; set; }
    public string DatabaseName { get; set; }
    public string DatabaseUserName { get; set; }
    public string DatabaseUserPassword { get; set; }
    public string Owner { get; set; }
    public PowerBiAppIdentity AppIdentity { get; set; }
  }

  public class TenantManagementDB : DbContext {

    public TenantManagementDB(DbContextOptions<TenantManagementDB> options)
    : base(options) { }

    public DbSet<PowerBiAppIdentity> AppIdentities { get; set; }
    public DbSet<PowerBiTenant> Tenants { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {

      modelBuilder.Entity<PowerBiTenant>()
            .HasOne(tenant => tenant.AppIdentity)
            .WithMany(appIdentity => appIdentity.Tenants)
            .HasForeignKey(tenant => tenant.Owner);

    }

  }
}
