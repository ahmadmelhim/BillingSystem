using System.Text;
using BillingSystem.Infrastructure.Configuration;
using BillingSystem.Examples;
using BillingSystem.Core.Interfaces;
using BillingSystem.Infrastructure.Data;
using BillingSystem.Infrastructure.Services.Auth;
using BillingSystem.Infrastructure.Services.Business;
using BillingSystem.Infrastructure.Services.Email;
using BillingSystem.Infrastructure.Services.Pdf;
using BillingSystem.Infrastructure.Services.Localization;
using BillingSystem.Infrastructure.Services; // For SystemSettingsService
using BillingSystem.Infrastructure.BackgroundServices;
using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MudBlazor.Services;
using QuestPDF.Infrastructure;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// ===== QuestPDF License =====
QuestPDF.Settings.License = LicenseType.Community;

// ===== DbContext =====
// DbContextFactory for Blazor Server - allows creating multiple contexts
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Scoped DbContext for dependency injection in repositories
// Each request/circuit gets its own DbContext instance from the factory
builder.Services.AddScoped<ApplicationDbContext>(sp =>
{
    var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    return factory.CreateDbContext();
});

// ===== Repositories =====
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();

// ===== Blazor =====
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();

// ===== Application Services =====
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPdfService, InvoicePdfService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAdminService, AdminService>(); // Admin-only service
// AuthService is registered via AddHttpClient below
builder.Services.AddSingleton<LanguageService>(); // Language service
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>(); // System Settings
builder.Services.AddScoped<IAuditService, AuditService>(); // Audit Logs

// ===== Rate Limiting =====
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();



// ===== Email =====
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));

builder.Services.AddScoped<IEmailService, EmailService>();

// ===== HttpClient & AuthService =====
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
{
    var baseUrl = builder.Configuration.GetValue<string>("BaseUrl") ?? "https://localhost:7060/";
    client.BaseAddress = new Uri(baseUrl);
});

// ===== MudBlazor =====
builder.Services.AddMudServices();

// ===== Background Services =====
builder.Services.AddHostedService<OverdueInvoiceWorker>();

// ===== Controllers (API) =====
builder.Services.AddControllers();

// ===== JWT Settings =====
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection.GetValue<string>("Key");
var jwtIssuer = jwtSection.GetValue<string>("Issuer");
var jwtAudience = jwtSection.GetValue<string>("Audience");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// ===== Authorization + Roles =====
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("AccountantOnly", policy => policy.RequireRole("Accountant"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer"));
});

var app = builder.Build();

// ===== Security Headers =====
app.Use(async (context, next) =>
{
    // Prevent MIME-sniffing attacks
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    
    // Prevent clickjacking attacks
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    
    // Enable XSS protection
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    
    // Referrer Policy
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    
    // Content Security Policy (basic)
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline' 'unsafe-eval';");
    
    await next();
});

// ===== Middleware Pipeline =====
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Rate Limiting Middleware (before Authentication)
app.UseIpRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
