using System.Collections.Generic;
using ProjetoTerapia.Models;

namespace ProjetoTerapia
{
    public static class BancoFake
    {
        public static List<Clinica> Clinicas { get; set; } = new List<Clinica>();
    }
}