using MudBlazor;
using MudBlazor.Services;
using NORCE.Drilling.Rig.WebApp;
using NORCE.Drilling.Rig.WebPages;
using NORCE.Drilling.Rig.WebPages.Shared;

var builder = WebApplication.CreateBuilder(args);

WebPagesHostConfiguration webPagesConfiguration = new()
{
    RigHostURL = builder.Configuration["RigHostURL"] ?? string.Empty,
    UnitConversionHostURL = builder.Configuration["UnitConversionHostURL"] ?? string.Empty,
    FieldHostURL = builder.Configuration["FieldHostURL"] ?? string.Empty,
    ClusterHostURL = builder.Configuration["ClusterHostURL"] ?? string.Empty,
};

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<IRigWebPagesConfiguration>(webPagesConfiguration);
builder.Services.AddSingleton<IRigAPIUtils, RigAPIUtils>();
builder.Services.AddScoped<RigApiClient>();
builder.Services.AddScoped<FieldClusterApiClient>();
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

var app = builder.Build();

app.UseForwardedHeaders();
var basePath = "/rig/webapp";
app.UsePathBase(basePath);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();

