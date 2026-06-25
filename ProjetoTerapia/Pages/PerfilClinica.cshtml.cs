using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoTerapia.Pages
{
    public class PerfilClinicaModel : PageModel
    {
        private readonly AppDbContext _context;

        public Clinica Clinica { get; set; } = new();

        public List<string> ListaEspecialidades { get; set; } = new();

        public bool EhDonoPerfil { get; set; } = false;

        public PerfilClinicaModel(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet(int? id)
        {
            var clinicaLogada = HttpContext.Session.GetString("ClinicaLogada");
            var pacienteLogado = HttpContext.Session.GetString("PacienteLogado");

            if (id.HasValue)
            {
                Clinica = _context.Clinicas
                    .FirstOrDefault(c => c.Id == id.Value)!;

                if (Clinica == null)
                {
                    return NotFound();
                }

                EhDonoPerfil =
                    pacienteLogado == null &&
                    clinicaLogada != null &&
                    int.TryParse(clinicaLogada, out int clinicaLogadaId) &&
                    clinicaLogadaId == Clinica.Id;

                if (!EhDonoPerfil)
                {
                    Clinica.Visualizacoes++;
                    _context.SaveChanges();
                }
            }
            else
            {
                if (clinicaLogada == null)
                {
                    return RedirectToPage("/LoginClinica");
                }

                Clinica = _context.Clinicas
                    .FirstOrDefault(c => c.Id == int.Parse(clinicaLogada))!;

                if (Clinica == null)
                {
                    return RedirectToPage("/LoginClinica");
                }

                EhDonoPerfil = true;
            }

            CarregarEspecialidades();

            return Page();
        }

        public IActionResult OnGetWhatsapp(int id)
        {
            var clinica = _context.Clinicas
                .FirstOrDefault(c => c.Id == id);

            if (clinica == null)
            {
                return NotFound();
            }

            var clinicaLogada = HttpContext.Session.GetString("ClinicaLogada");
            var pacienteLogado = HttpContext.Session.GetString("PacienteLogado");

            var ehDono =
                pacienteLogado == null &&
                clinicaLogada != null &&
                int.TryParse(clinicaLogada, out int clinicaLogadaId) &&
                clinicaLogadaId == clinica.Id;

            if (!ehDono)
            {
                clinica.CliquesWhatsapp++;
                _context.SaveChanges();
            }

            var telefone = new string((clinica.Telefone ?? "")
                .Where(char.IsDigit)
                .ToArray());

            if (string.IsNullOrWhiteSpace(telefone))
            {
                return RedirectToPage("/PerfilClinica", new { id = clinica.Id });
            }

            return Redirect($"https://wa.me/{telefone}");
        }

        private void CarregarEspecialidades()
        {
            if (!string.IsNullOrEmpty(Clinica.Especialidades))
            {
                ListaEspecialidades = Clinica.Especialidades
                    .Split(',')
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .ToList();
            }
        }
    }
}