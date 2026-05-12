using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoTerapia.Pages
{
    public class ExternalLoginModel : PageModel
    {
        public IActionResult OnGet(string provider)
        {
            var redirectUrl = Url.Page(
                "/ExternalLogin",
                pageHandler: "Callback");

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            return Challenge(properties, provider);
        }

        public async Task<IActionResult> OnGetCallbackAsync()
        {
            var result = await HttpContext.AuthenticateAsync();

            if (!result.Succeeded)
            {
                return RedirectToPage("/LoginClinica");
            }

            return RedirectToPage("/Teste");
        }
    }
}