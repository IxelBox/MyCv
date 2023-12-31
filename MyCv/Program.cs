using Microsoft.AspNetCore.HttpOverrides;
using MyCv;
using MyCv.Database;
using MyCv.Export.Pdf;
using MyCv.Model.Providers;
using YamlDotNet.Serialization;

var preBuilder = WebApplication.CreateBuilder(args);
var preBuild = preBuilder.Build();
var webHostEnvironment = preBuild.Services.GetService<IWebHostEnvironment>() ?? throw new NullReferenceException("Web Host Environment can't initialize!");
var myCvYaml =Path.Combine(webHostEnvironment.WebRootPath, "data", "mycv", "mycv.yaml");
await preBuild.DisposeAsync();

var deserializer = new DeserializerBuilder().Build();
var data = deserializer.Deserialize<SideStructure>(File.ReadAllText(myCvYaml));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MyCvContext>();
builder.Services.AddSingleton<IWorkingDirectoryProvider, PdfWorkingDirectoryProvider>();
builder.Services.AddSingleton<IFileExporter, PdfExporter>();
builder.Services.AddSingleton<ApplicationSecretProvider>();
builder.Services.AddSingleton<RequestBlocker>();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton(typeof(SideStructure), data);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                       ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
