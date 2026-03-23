using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ProjetoTerapia.Pages
{
    public class AnsiedadeModel : PageModel
    {
        public int Porcentagem { get; set; } = -1;

        public void OnPost(int q1, int q2)
        {
            int pontos = q1 + q2;
            int total = 2;

            Porcentagem = (int)((double)pontos / total * 100);
        }
    }
}