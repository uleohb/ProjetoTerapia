using ProjetoTerapia.Models;

public class RecuperacaoSenha
{
    public int Id { get; set; }

    public int ClinicaId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime Expiracao { get; set; }

    public bool Usado { get; set; }

    public DateTime DataCriacao { get; set; }


    public Clinica? Clinica { get; set; }
}