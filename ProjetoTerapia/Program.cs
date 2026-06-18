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
});


// RAZOR
builder.Services.AddRazorPages();

builder.Services.AddControllers();

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


// PAGES
app.MapRazorPages();

app.MapControllers();


// HOME
app.MapGet("/", context =>
{
    context.Response.Redirect("/Teste");
    return Task.CompletedTask;
});


// WEBHOOK
app.MapPost("/webhook", async (HttpContext context, AppDbContext db) =>
{
    using var reader = new StreamReader(context.Request.Body);

    var body = await reader.ReadToEndAsync();

    Console.WriteLine("Webhook recebido: " + body);

    return Results.Ok();
});


// RUN
app.Run();