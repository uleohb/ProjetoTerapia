using ProjetoTerapia.Models;
using ProjetoTerapia.Services;

using Microsoft.EntityFrameworkCore;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.RateLimiting;

using System.Threading.RateLimiting;


var builder = WebApplication.CreateBuilder(args);


// =============================================================
// SERVIÇOS
// =============================================================

builder.Services.AddScoped<EmailService>();


// =============================================================
// BANCO DE DADOS
// =============================================================

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            );

            sqlOptions.CommandTimeout(60);
        }));


// =============================================================
// SESSÃO
// =============================================================

builder.Services.AddSession(options =>
{
    /*
     * Mantemos 30 dias porque essa sessão também é utilizada
     * pelas contas normais da plataforma.
     *
     * A área administrativa terá uma expiração própria,
     * bem menor, que implementaremos separadamente.
     */
    options.IdleTimeout = TimeSpan.FromDays(30);

    options.Cookie.Name = ".AlinhaVida.Session";

    options.Cookie.HttpOnly = true;

    options.Cookie.IsEssential = true;

    options.Cookie.SameSite = SameSiteMode.Lax;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;
});


// =============================================================
// AUTENTICAÇÃO
// =============================================================

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme =
            CookieAuthenticationDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            GoogleDefaults.AuthenticationScheme;
    })

    .AddCookie(options =>
    {
        options.Cookie.Name =
            ".AlinhaVida.Authentication";

        options.Cookie.HttpOnly = true;

        options.Cookie.SecurePolicy =
            CookieSecurePolicy.Always;

        options.Cookie.SameSite =
            SameSiteMode.Lax;

        options.ExpireTimeSpan =
            TimeSpan.FromHours(8);

        options.SlidingExpiration =
            true;
    })

    .AddGoogle(options =>
    {
        options.ClientId =
            builder.Configuration[
                "Authentication:GoogleAuth:ClientId"
            ]!;

        options.ClientSecret =
            builder.Configuration[
                "Authentication:GoogleAuth:ClientSecret"
            ]!;

        options.CallbackPath =
            "/signin-google";


        options.CorrelationCookie.HttpOnly =
            true;

        options.CorrelationCookie.SameSite =
            SameSiteMode.Lax;

        options.CorrelationCookie.SecurePolicy =
            CookieSecurePolicy.Always;
    });


// =============================================================
// RATE LIMITING
// =============================================================

builder.Services.AddRateLimiter(options =>
{
    /*
     * Quando o limite for excedido:
     *
     * HTTP 429 = Too Many Requests
     */
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;


    /*
     * Proteção destinada à página administrativa.
     *
     * Esse limite NÃO é ainda o bloqueio de
     * "5 senhas erradas".
     *
     * Ele é uma camada adicional contra bots
     * disparando muitas requisições rapidamente.
     */
    options.AddPolicy(
        "admin-login",
        httpContext =>
        {
            var enderecoIp =
                httpContext.Connection.RemoteIpAddress?
                    .ToString()
                ?? "desconhecido";


            return RateLimitPartition
                .GetFixedWindowLimiter(
                    partitionKey: enderecoIp,

                    factory: _ =>
                        new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 20,

                            Window =
                                TimeSpan.FromMinutes(1),

                            QueueLimit = 0,

                            QueueProcessingOrder =
                                QueueProcessingOrder
                                    .OldestFirst,

                            AutoReplenishment = true
                        });
        });
});


// =============================================================
// RAZOR / CONTROLLERS / HTTP
// =============================================================

builder.Services.AddRazorPages();

builder.Services.AddControllers();

builder.Services.AddHttpClient();


var app = builder.Build();


// =============================================================
// TRATAMENTO DE ERROS
// =============================================================

if (!app.Environment.IsDevelopment())
{
    /*
     * Em produção nunca mostramos detalhes internos
     * das exceções ao visitante.
     */
    app.UseExceptionHandler("/Error");

    app.UseHsts();
}


// =============================================================
// CABEÇALHOS BÁSICOS DE SEGURANÇA
// =============================================================

app.Use(async (context, next) =>
{
    context.Response.Headers[
        "X-Content-Type-Options"
    ] = "nosniff";


    context.Response.Headers[
        "X-Frame-Options"
    ] = "DENY";


    context.Response.Headers[
        "Referrer-Policy"
    ] = "strict-origin-when-cross-origin";


    await next();
});


// =============================================================
// MIDDLEWARES
// =============================================================

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();


// O rate limiter precisa estar no pipeline.
app.UseRateLimiter();


app.UseSession();

app.UseAuthentication();

app.UseAuthorization();


// =============================================================
// HOME
// =============================================================

app.MapGet("/", context =>
{
    context.Response.Redirect("/Teste");

    return Task.CompletedTask;
});


// =============================================================
// RAZOR PAGES / CONTROLLERS
// =============================================================

app.MapRazorPages();

app.MapControllers();


// =============================================================
// RUN
// =============================================================

app.Run();