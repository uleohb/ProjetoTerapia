using Microsoft.AspNetCore.Mvc;
using ProjetoTerapia.Models;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly AppDbContext _context;

    public WebhookController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Receber()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        Console.WriteLine("Webhook recebido:");
        Console.WriteLine(body);

        // Aqui depois vamos validar pagamento real

        return Ok();
    }
}