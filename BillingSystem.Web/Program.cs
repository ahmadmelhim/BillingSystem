using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using BillingSystem.Web.Services;
using BillingSystem.Web.Services.ApiClients;
using BillingSystem.Core.Interfaces;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// Configure HttpClient for API communication
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7293";

builder.Services.AddScoped(sp => {
    var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
    
    // TODO: Add JWT token to Authorization header when implemented
    // var token = await localStorage.GetItemAsync<string>("authToken");
    // if (!string.IsNullOrEmpty(token))
    // {
    //     httpClient.DefaultRequestHeaders.Authorization = 
    //         new AuthenticationHeaderValue("Bearer", token);
    // }
    
    return httpClient;
});

// Register API Client Services (both as API service and as base service interface)
builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<IAuthApiService>(sp => sp.GetRequiredService<AuthApiService>());
builder.Services.AddScoped<IAuthService>(sp => sp.GetRequiredService<AuthApiService>());

builder.Services.AddScoped<CustomerApiService>();
builder.Services.AddScoped<ICustomerApiService>(sp => sp.GetRequiredService<CustomerApiService>());
builder.Services.AddScoped<ICustomerService>(sp => sp.GetRequiredService<CustomerApiService>());

builder.Services.AddScoped<InvoiceApiService>();
builder.Services.AddScoped<IInvoiceApiService>(sp => sp.GetRequiredService<InvoiceApiService>());
builder.Services.AddScoped<IInvoiceService>(sp => sp.GetRequiredService<InvoiceApiService>());
builder.Services.AddScoped<IPdfService>(sp => sp.GetRequiredService<InvoiceApiService>());

builder.Services.AddScoped<PaymentApiService>();
builder.Services.AddScoped<IPaymentApiService>(sp => sp.GetRequiredService<PaymentApiService>());

builder.Services.AddScoped<UserApiService>();
builder.Services.AddScoped<IUserApiService>(sp => sp.GetRequiredService<UserApiService>());
builder.Services.AddScoped<IUserService>(sp => sp.GetRequiredService<UserApiService>());

// Register other services
builder.Services.AddScoped<LanguageService>();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
