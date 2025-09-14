using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using bookstoree.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims; // Added for ClaimTypes
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<bookstoreeContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("bookstoreeContext") ?? throw new InvalidOperationException("Connection string 'bookstoreeContext' not found.")));

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
builder.Services.AddControllersWithViews();

var app = builder.Build();

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

app.UseAuthentication(); // Add this before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
