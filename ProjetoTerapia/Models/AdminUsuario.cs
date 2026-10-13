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


        // =====================================================
        // SEGURANÇA DE LOGIN
        // =====================================================

        public int TentativasLoginFalhas { get; set; } = 0;

        public DateTime? BloqueadoAte { get; set; }

        public DateTime? UltimoLogin { get; set; }


        // =====================================================
        // AUTENTICAÇÃO EM DUAS ETAPAS
        // =====================================================

        public string? Codigo2FAHash { get; set; }

        public DateTime? Codigo2FAExpiraEm { get; set; }

        public DateTime? UltimoCodigo2FAEnviadoEm { get; set; }
    }
}