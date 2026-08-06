using System;
using System.Collections.Generic;

namespace ProjetoTerapia.Models
{
    public class Vendedor
    {
        public int Id { get; set; }

        public string Nome { get; set; } = "";

        public string Email { get; set; } = "";

        public string Telefone { get; set; } = "";

        public string CodigoIndicacao { get; set; } = "";

        public string SenhaHash { get; set; } = "";

        public bool Ativo { get; set; } = true;

        public decimal PercentualComissao { get; set; } = 20;

        public string? ChavePix { get; set; }

        public DateTime DataCadastro { get; set; } = DateTime.Now;

        public List<VendaVendedor> Vendas { get; set; } = new();
    }
}