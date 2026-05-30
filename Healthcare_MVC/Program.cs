
using Healthcare_System_AdavanceProgrammingProject.MVC.Services;
using HealthcareClinic.API.Data;
using HealthcareClinic.API.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. SERVICES CONFIGURATION CONTAINER (DI)
// ==========================================

// Add standard MVC controller views rendering engine pipelines
builder.Services.AddControllersWithViews();

// Pull the database connection string layout dynamically out of appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found inside appsettings.json.");

// Inject the shared DbContext infrastructure directly into your MVC pipeline
builder.Services.AddDbContext<ClinicDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register your generic underlying HttpClient background engine
builder.Services.AddHttpClient();
// Add server-side session store and session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Fetch the API Base URL out of appsettings.json dynamically
var apiBaseUrl = builder.Configuration["ClinicApiSettings:BaseUrl"]
    ?? "https://localhost:7123/"; // Safe fallback

// Explicitly register your custom ClinicApiService using the dynamic URL
builder.Services.AddHttpClient<ClinicApiService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});
builder.Services.AddScoped<NotificationService>();

// Configure Secure Local Web Browser Session Cookie Authorization tracking schemas
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";     // Destination if an unauthorized user tries to see a secure dashboard
        options.LogoutPath = "/Account/Logout";   // Clearance path tracking redirect destination
        options.ExpireTimeSpan = TimeSpan.FromHours(3);
        options.SlidingExpiration = true;         // Refreshes the login window if the employee remains active
    });

var app = builder.Build();

// ==========================================
// 2. HTTP REQUEST PROCESSING PIPELINE (MIDDLEWARE)
// ==========================================

// Configure system global routing exceptions handlers
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Allows your application to read css styles and your liveAlerts.js file

app.UseRouting();
app.UseSession();
// CRITICAL SECURITY ORDERING: Enforce authentication checks BEFORE loading authorization roles
app.UseAuthentication();
app.UseAuthorization();

// Map your traditional URL mapping schemas onto the solution architecture matrix
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Execute runtime engine tasks
app.Run();