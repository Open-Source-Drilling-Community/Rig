using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

if (!string.IsNullOrEmpty(builder.Configuration["RigHostURL"]))
{
    NORCE.Drilling.Rig.WebApp.Configuration.RigHostURL = builder.Configuration["RigHostURL"];
}
if (!string.IsNullOrEmpty(builder.Configuration["UnitConversionHostURL"]))
{
    NORCE.Drilling.Rig.WebApp.Configuration.UnitConversionHostURL = builder.Configuration["UnitConversionHostURL"];
}
if (!string.IsNullOrEmpty(builder.Configuration["FieldHostURL"]))
{
    NORCE.Drilling.Rig.WebApp.Configuration.FieldHostURL = builder.Configuration["FieldHostURL"];
}
if (!string.IsNullOrEmpty(builder.Configuration["ClusterHostURL"]))
{
    NORCE.Drilling.Rig.WebApp.Configuration.ClusterHostURL = builder.Configuration["ClusterHostURL"];
}

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped(_ => APIUtils.CreateHttpClient(APIUtils.HostNameRig, APIUtils.HostBasePathRig));
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
app.UsePathBase("/Rig/webapp");

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
