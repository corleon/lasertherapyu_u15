using LTU_U15.Services;
using LTU_U15.Services.Membership;
using Microsoft.AspNetCore.DataProtection;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "umbraco", "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionKeysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("LTU_U15");

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

builder.Services.AddScoped<WebinarImportService>();
builder.Services.AddControllersWithViews();
builder.Services.Configure<MembershipEmailSettings>(builder.Configuration.GetSection("Membership:Email"));
builder.Services.AddScoped<IMembershipEmailService, MembershipEmailService>();
WebApplication app = builder.Build();


await app.BootUmbracoAsync();

app.UseHttpsRedirection();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.EndpointRouteBuilder.MapControllers();
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
