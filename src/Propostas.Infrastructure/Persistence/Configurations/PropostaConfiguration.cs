using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Propostas.Domain.Entities;

namespace Propostas.Infrastructure.Persistence.Configurations;

internal sealed class PropostaConfiguration : IEntityTypeConfiguration<Proposta>
{
    public void Configure(EntityTypeBuilder<Proposta> builder)
    {
        builder.ToTable("Propostas");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.HasIndex(p => p.ClienteId).IsUnique();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsUnicode(false);

        builder.Property(p => p.RendaMensal).HasPrecision(18, 2);
        builder.Property(p => p.LimitePorCartao).HasPrecision(18, 2);
        builder.Ignore(p => p.LimiteTotal);
    }
}
