using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TeacherIdentity.AuthServer.Models.Mappings;

public class EstablishmentDomainMapping : IEntityTypeConfiguration<EstablishmentDomain>
{
    public void Configure(EntityTypeBuilder<EstablishmentDomain> builder)
    {
        builder.ToTable("establishment_domains");
        builder.Property(d => d.DomainName).IsRequired();
        builder.HasKey(d => d.DomainName);
    }
}
