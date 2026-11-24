using System.ComponentModel.DataAnnotations;

namespace ProjetoTerapia.Models
{
    public class ConteudoProfissional
    {
        public int Id { get; set; }

        // =====================================================
        // PROFISSIONAL
        // =====================================================

        public int ClinicaId { get; set; }

        public Clinica? Clinica { get; set; }


        // =====================================================
        // TIPO DE CONTEÚDO
        // =====================================================

        [Required]
        [StringLength(20)]
        public string Tipo { get; set; } = "";
        // Valores esperados:
        // "Imagem"
        // "Video"


        // =====================================================
        // CONTEÚDO
        // =====================================================

        [StringLength(120)]
        public string? Titulo { get; set; }

        [StringLength(1000)]
        public string? Legenda { get; set; }

        [Required]
        [StringLength(1000)]
        public string UrlArquivo { get; set; } = "";

        [StringLength(1000)]
        public string? UrlThumbnail { get; set; }


        // =====================================================
        // ORGANIZAÇÃO
        // =====================================================

        public int Ordem { get; set; } = 0;

        public bool Ativo { get; set; } = true;


        // =====================================================
        // CONTROLE
        // =====================================================

        public DateTime DataCadastro { get; set; } = DateTime.Now;

        public DateTime? DataAtualizacao { get; set; }
    }
}