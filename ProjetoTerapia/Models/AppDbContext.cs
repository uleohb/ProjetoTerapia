using Microsoft.EntityFrameworkCore;

namespace ProjetoTerapia.Models
{
    public class AppDbContext : DbContext
    {
        public DbSet<Clinica> Clinicas { get; set; }

        public DbSet<RegistroHumor> RegistrosHumor { get; set; }

        public DbSet<ResultadoTestePaciente> ResultadosTestePacientes { get; set; }

        public DbSet<RecuperacaoSenhaPaciente> RecuperacoesSenhaPacientes { get; set; }

        public DbSet<Paciente> Pacientes { get; set; }

        public DbSet<RecuperacaoSenha> RecuperacoesSenha { get; set; }

        public DbSet<Agendamento> Agendamentos { get; set; }

        public DbSet<Consulta> Consultas { get; set; }

        public DbSet<DivulgacaoRegional> DivulgacoesRegionais { get; set; }

        public DbSet<Vendedor> Vendedores { get; set; }

        public DbSet<VendaVendedor> VendasVendedores { get; set; }

        public DbSet<AdminUsuario> AdminUsuarios { get; set; }

        public DbSet<AdminLog> AdminLogs { get; set; }

        public DbSet<AlteracaoClinica> AlteracoesClinicas { get; set; }

        public DbSet<ConteudoProfissional> ConteudosProfissionais { get; set; }


        // =====================================================
        // ASSINATURA PROFISSIONAL
        // =====================================================

        public DbSet<AssinaturaProfissional>
            AssinaturasProfissionais
        { get; set; }


        // =====================================================
        // TERMOS PROFISSIONAIS
        // =====================================================

        public DbSet<AceiteTermoProfissional>
            AceitesTermosProfissionais
        { get; set; }


        // =====================================================
        // CONSTRUTOR
        // =====================================================

        public AppDbContext(
            DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }


        // =====================================================
        // MODEL BUILDER
        // =====================================================

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // =================================================
            // CONTEÚDOS PROFISSIONAIS
            // =================================================

            modelBuilder
                .Entity<ConteudoProfissional>()
                .HasOne(c => c.Clinica)
                .WithMany()
                .HasForeignKey(c => c.ClinicaId)
                .OnDelete(DeleteBehavior.Cascade);


            // =================================================
            // ASSINATURA PROFISSIONAL
            // =================================================

            modelBuilder
                .Entity<AssinaturaProfissional>()
                .HasOne(a => a.Clinica)
                .WithMany(c =>
                    c.AssinaturasProfissionais
                )
                .HasForeignKey(a =>
                    a.ClinicaId
                )
                .OnDelete(
                    DeleteBehavior.SetNull
                );


            /*
             * Valor monetário.
             */
            modelBuilder
                .Entity<AssinaturaProfissional>()
                .Property(a => a.Valor)
                .HasPrecision(18, 2);


            /*
             * Facilita encontrar todas as assinaturas
             * pertencentes a uma clínica.
             */
            modelBuilder
                .Entity<AssinaturaProfissional>()
                .HasIndex(a =>
                    a.ClinicaId
                );


            /*
             * Uma assinatura do Mercado Pago possui
             * um único PreapprovalId.
             *
             * Como o campo é nullable, o SQL Server
             * permite registros ainda não enviados
             * ao Mercado Pago.
             */
            modelBuilder
                .Entity<AssinaturaProfissional>()
                .HasIndex(a =>
                    a.MercadoPagoPreapprovalId
                )
                .IsUnique();


            /*
             * ExternalReference também será usado
             * para localizar a assinatura no webhook.
             */
            modelBuilder
                .Entity<AssinaturaProfissional>()
                .HasIndex(a =>
                    a.ExternalReference
                );


            // =================================================
            // ACEITE DOS TERMOS
            // =================================================

            modelBuilder
                .Entity<AceiteTermoProfissional>()
                .HasOne(a =>
                    a.Clinica
                )
                .WithMany(c =>
                    c.AceitesTermosProfissionais
                )
                .HasForeignKey(a =>
                    a.ClinicaId
                )
                .OnDelete(
                    DeleteBehavior.SetNull
                );


            modelBuilder
                .Entity<AceiteTermoProfissional>()
                .Property(a =>
                    a.ValorInformado
                )
                .HasPrecision(18, 2);


            modelBuilder
                .Entity<AceiteTermoProfissional>()
                .HasIndex(a =>
                    a.ClinicaId
                );


            modelBuilder
                .Entity<AceiteTermoProfissional>()
                .HasIndex(a =>
                    new
                    {
                        a.ClinicaId,
                        a.VersaoTermos
                    }
                );


            // =================================================
            // CLÍNICA
            // =================================================

            modelBuilder
                .Entity<Clinica>()
                .Property(c =>
                    c.Valor
                )
                .HasPrecision(18, 2);


            modelBuilder
                .Entity<Clinica>()
                .Property(c =>
                    c.ValorPlano
                )
                .HasPrecision(18, 2);
        }
    }
}