using ProjetoTerapia.Models;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer("Server=localhost;Database=ProjetoTerapiaDB;Trusted_Connection=True;TrustServerCertificate=True"));
builder.Services.AddSession();
builder.Services.AddRazorPages();


var app = builder.Build();


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapRazorPages();


app.MapGet("/", context =>
{
    context.Response.Redirect("/Teste");
    return Task.CompletedTask;
});

app.Run();

