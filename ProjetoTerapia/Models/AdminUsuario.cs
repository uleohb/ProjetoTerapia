using System;

namespace ProjetoTerapia.Models
{
    public class AdminUsuario
    {
        public int Id { get; set; }

        public string Nome { get; set; } = "";

        public string Email { get; set; } = "";

        public string SenhaHash { get; set; } = "";

        public string Perfil { get; set; } = "Operacional";

        public bool Ativo { get; set; } = true;

        public DateTime DataCriacao { get; set; } = DateTime.Now;
    }
}