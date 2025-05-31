using BlazorApp1.Components;
using Microsoft.AspNetCore.Components.Authorization;
using BlazorApp1.Authentication;
using BlazorApp1.Models.DTOs;
using BlazorApp1.Services.Interfaces;
using BlazorApp1.Services.Implementations;
using Microsoft.Data.SqlClient;
using BlazorApp1.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services
  .AddAuthenticationCore()
  .AddAuthorizationCore();

// register the concrete type so it can be injected by class name
builder.Services.AddScoped<BlazorApp1.Authentication.AuthStateProvider>();

builder.Services.AddTransient<SqlConnection>(sp =>
    new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddControllers();

// Register services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurb65Service, Curb65Service>();
builder.Services.AddScoped<IQriskService, QriskService>();
builder.Services.AddScoped<IFindriscService, FindriscService>();
builder.Services.AddScoped<ILabTechnicianService, LabTechnicianService>();
builder.Services.AddScoped<IReceptionistService,ReceptionistService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddSingleton<IWebHostEnvironment>(builder.Environment);
builder.Services.AddAuthenticationCore();
builder.Services.AddAuthorizationCore();

// Register custom services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider, AuthStateProvider>();

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();
app.MapControllers();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
