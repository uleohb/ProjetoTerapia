using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ProjetoTerapia.Models;
using System.Collections.Generic;
using System.Linq;

namespace ProjetoTerapia.Pages
{
    public class PerfilClinicaModel : PageModel
    {
        private readonly AppDbContext _context;


        public Clinica Clinica { get; set; } = new();


        public List<string> ListaEspecialidades { get; set; }
            = new();


        public List<ConteudoProfissional> Conteudos { get; set; }
            = new();


        public bool EhDonoPerfil { get; set; }
            = false;


        public bool PacienteLogado { get; set; }
            = false;


        public PerfilClinicaModel(
            AppDbContext context)
        {
            _context = context;
        }


        // =====================================================
        // GET
        // =====================================================

        public IActionResult OnGet(
            int? id)
        {
            var clinicaLogada =
                HttpContext.Session.GetString(
                    "ClinicaLogada"
                );


            var pacienteLogado =
                HttpContext.Session.GetString(
                    "PacienteLogado"
                );


            PacienteLogado =
                !string.IsNullOrEmpty(
                    pacienteLogado
                );


            // =================================================
            // PERFIL ABERTO PELO ID
            // =================================================

            if (id.HasValue)
            {
                Clinica =
                    _context.Clinicas
                        .FirstOrDefault(c =>
                            c.Id == id.Value
                        )!;


                if (Clinica == null)
                {
                    return NotFound();
                }


                EhDonoPerfil =
                    pacienteLogado == null &&
                    clinicaLogada != null &&
                    int.TryParse(
                        clinicaLogada,
                        out int clinicaLogadaId
                    ) &&
                    clinicaLogadaId == Clinica.Id;


                // =================================================
                // PERFIL INVISÍVEL
                // =================================================
                //
                // Se o profissional desativou a exibição pública,
                // somente o próprio profissional logado consegue
                // abrir a página de prévia.
                //
                // Visitantes e pacientes recebem 404 mesmo que
                // tentem acessar a URL diretamente pelo ID.

                if (!Clinica.ExibirPublicamente &&
                    !EhDonoPerfil)
                {
                    return NotFound();
                }


                // Não contamos visualização do próprio dono.
                if (!EhDonoPerfil)
                {
                    Clinica.Visualizacoes++;

                    _context.SaveChanges();
                }
            }

            // =================================================
            // PRÓPRIO PROFISSIONAL SEM ID
            // =================================================

            else
            {
                if (clinicaLogada == null)
                {
                    return RedirectToPage(
                        "/LoginClinica"
                    );
                }


                if (!int.TryParse(
                        clinicaLogada,
                        out int clinicaId))
                {
                    HttpContext.Session.Remove(
                        "ClinicaLogada"
                    );


                    return RedirectToPage(
                        "/LoginClinica"
                    );
                }


                Clinica =
                    _context.Clinicas
                        .FirstOrDefault(c =>
                            c.Id == clinicaId
                        )!;


                if (Clinica == null)
                {
                    HttpContext.Session.Remove(
                        "ClinicaLogada"
                    );


                    return RedirectToPage(
                        "/LoginClinica"
                    );
                }


                EhDonoPerfil =
                    true;
            }


            CarregarEspecialidades();

            CarregarConteudos();


            return Page();
        }


        // =====================================================
        // WHATSAPP
        // =====================================================

        public IActionResult OnGetWhatsapp(
            int id)
        {
            var clinica =
                _context.Clinicas
                    .FirstOrDefault(c =>
                        c.Id == id
                    );


            if (clinica == null)
            {
                return NotFound();
            }


            var clinicaLogada =
                HttpContext.Session.GetString(
                    "ClinicaLogada"
                );


            var pacienteLogado =
                HttpContext.Session.GetString(
                    "PacienteLogado"
                );


            var ehDono =
                pacienteLogado == null &&
                clinicaLogada != null &&
                int.TryParse(
                    clinicaLogada,
                    out int clinicaLogadaId
                ) &&
                clinicaLogadaId == clinica.Id;


            // Perfil invisível não deve permitir contato público
            // por URL direta. O próprio dono ainda pode abrir
            // sua página de prévia normalmente.
            if (!clinica.ExibirPublicamente &&
                !ehDono)
            {
                return NotFound();
            }


            if (!ehDono)
            {
                clinica.CliquesWhatsapp++;

                _context.SaveChanges();
            }


            var telefone =
                new string(
                    (clinica.Telefone ?? "")
                        .Where(
                            char.IsDigit
                        )
                        .ToArray()
                );


            if (string.IsNullOrWhiteSpace(
                    telefone))
            {
                return RedirectToPage(
                    "/PerfilClinica",
                    new
                    {
                        id = clinica.Id
                    }
                );
            }


            return Redirect(
                $"https://wa.me/{telefone}"
            );
        }


        // =====================================================
        // ESPECIALIDADES
        // =====================================================

        private void CarregarEspecialidades()
        {
            ListaEspecialidades.Clear();


            if (!string.IsNullOrEmpty(
                    Clinica.Especialidades))
            {
                ListaEspecialidades =
                    Clinica.Especialidades
                        .Split(',')
                        .Select(e =>
                            e.Trim()
                        )
                        .Where(e =>
                            !string.IsNullOrWhiteSpace(
                                e
                            )
                        )
                        .ToList();
            }
        }


        // =====================================================
        // CONTEÚDOS DO PERFIL
        // =====================================================

        private void CarregarConteudos()
        {
            Conteudos =
                _context
                    .ConteudosProfissionais
                    .Where(c =>
                        c.ClinicaId == Clinica.Id &&
                        c.Ativo
                    )
                    .OrderBy(c =>
                        c.Ordem
                    )
                    .ThenByDescending(c =>
                        c.DataCadastro
                    )
                    .Take(6)
                    .ToList();
        }
    }
}
