using System;

namespace ProjetoTerapia.Models
{
    public class AdminLog
    {
        public int Id { get; set; }

        public int AdminUsuarioId { get; set; }

        public AdminUsuario? AdminUsuario { get; set; }

        public string NomeAdmin { get; set; } = "";

        public string PerfilAdmin { get; set; } = "";

        public string Acao { get; set; } = "";

        public string Descricao { get; set; } = "";

        public DateTime DataAcao { get; set; } = DateTime.Now;
    }
}