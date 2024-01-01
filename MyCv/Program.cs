using System.Globalization;
using Microsoft.AspNetCore.HttpOverrides;
using MyCv;
using MyCv.Database;
using MyCv.Export.Pdf;
using MyCv.Model.Providers;
using YamlDotNet.Serialization;

var myCvYaml = Path.GetFullPath(Path.Combine("data","mycv.yaml"));

var deserializer = new DeserializerBuilder().Build();
var data = deserializer.Deserialize<SideStructure>(File.ReadAllText(myCvYaml));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MyCvContext>();
builder.Services.AddSingleton<IWorkingDirectoryProvider, PdfWorkingDirectoryProvider>();
builder.Services.AddSingleton<IFileExporter, PdfExporter>();
builder.Services.AddSingleton<ApplicationSecretProvider>();
builder.Services.AddSingleton<RequestBlocker>();
builder.Services.AddOutputCache(builder2 => { builder2.DefaultExpirationTimeSpan = TimeSpan.FromDays(100); });

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton(typeof(SideStructure), data);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment()) app.UseExceptionHandler("/Error");

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                       ForwardedHeaders.XForwardedProto
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache static files for 30 days
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=2592000");
        ctx.Context.Response.Headers.Append("Expires", DateTime.UtcNow.AddDays(30).ToString("R", CultureInfo.InvariantCulture));
    }
});

app.UseRouting();
app.UseOutputCache();

app.MapRazorPages();

app.Run();