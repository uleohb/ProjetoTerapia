using ProjetoTerapia.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using ProjetoTerapia.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<EmailService>();

// BANCO
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        "Server=localhost;Database=ProjetoTerapiaDB;Trusted_Connection=True;TrustServerCertificate=True"));

// SESSÃO
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// AUTH GOOGLE
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:GoogleAuth:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:GoogleAuth:ClientSecret"]!;
    options.CallbackPath = "/signin-google";
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
});

// RAZOR / CONTROLLERS / HTTP
builder.Services.AddRazorPages();

builder.Services.AddControllers();

builder.Services.AddHttpClient();

var app = builder.Build();

// ERROS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// MIDDLEWARES
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();

app.UseAuthorization();

// HOME
app.MapGet("/", context =>
{
    context.Response.Redirect("/Teste");
    return Task.CompletedTask;
});

// PAGES / CONTROLLERS
app.MapRazorPages();

app.MapControllers();

// RUN
app.Run();