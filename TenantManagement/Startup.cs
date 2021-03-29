
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TenantManagement.Services;
using TenantManagement.Models;

namespace TenantManagement {

  public class Startup {

    public Startup(IConfiguration configuration) {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services) {

      string connectString = Configuration["TenantManagementDB:ConnectString"];
      services.AddDbContext<TenantManagementDB>(opt => opt.UseSqlServer(connectString));

      services
       .AddMicrosoftIdentityWebAppAuthentication(Configuration)
       .EnableTokenAcquisitionToCallDownstreamApi(MicrosoftGraphApi.RequiredAdminScopes)
       .AddInMemoryTokenCaches();

      services.AddScoped(typeof(TenantManagementDB));
      services.AddScoped(typeof(TenantManagementDbService));
      services.AddScoped(typeof(MicrosoftGraphApi));
      services.AddScoped(typeof(PowerBiServiceApi));

      services.AddControllersWithViews(options => {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        options.Filters.Add(new AuthorizeFilter(policy));
      });

      services
        .AddRazorPages()
        .AddMicrosoftIdentityUI();
    
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {

      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
      }
      else {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
      }
      app.UseHttpsRedirection();
      app.UseStaticFiles();

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints => {
        endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        endpoints.MapRazorPages();
      });

    }
  }
}
