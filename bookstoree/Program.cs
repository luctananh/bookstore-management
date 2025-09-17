using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using bookstoree.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims; // Added for ClaimTypes
using System.Text.Json.Serialization; // Added for ReferenceHandler.Preserve
using bookstoree.Services;
using Npgsql.EntityFrameworkCore.PostgreSQL; // Added for PostgreSQL

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<bookstoreeContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SupabaseConnection") ?? throw new InvalidOperationException("Connection string 'SupabaseConnection' not found.")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentStoreService>();

// Add authentication services
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Users/Login"; // Set your login path
        options.AccessDeniedPath = "/Home/AccessDenied"; // Set your access denied path
        options.ClaimsIssuer = "bookstoree"; // A unique issuer for your application
        options.Cookie.Name = "bookstoreeAuth"; // A unique name for your authentication cookie
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Use Always in production with HTTPS
    });

// Add services to the container.
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = "bookstoree_"; // Optional: prefix for keys in Redis
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Make the session cookie essential
});

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    });

var app = builder.Build();

app.Urls.Add("http://+:8080");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Add this after UseRouting and before UseAuthentication/UseAuthorization

app.UseAuthentication(); // Add this before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
