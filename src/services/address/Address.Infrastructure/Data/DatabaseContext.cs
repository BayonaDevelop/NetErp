using Address.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Address.Infrastructure.Data;

public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
  public virtual DbSet<Core.Entities.Address> Addresses { get; set; }

  public virtual DbSet<City> Cities { get; set; }

  public virtual DbSet<Country> Countries { get; set; }

  public virtual DbSet<Locality> Localities { get; set; }

  public virtual DbSet<Municipality> Municipalities { get; set; }

  public virtual DbSet<Settlement> Settlements { get; set; }

  public virtual DbSet<SettlementType> SettlementTypes { get; set; }

  public virtual DbSet<Street> Streets { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.HasDefaultSchema("Addresses");

    modelBuilder.Entity<Core.Entities.Address>(entity =>
    {
      entity.HasKey(e => e.Id).HasName("pk_Address");

      entity.Property(e => e.ZipCode).HasMaxLength(15);
      entity.Property(e => e.ExternalNumber).HasMaxLength(10);
      entity.Property(e => e.InternalNumber).HasMaxLength(10);
      entity.Property(e => e.StreetAid).HasColumnName("StreetAId");
      entity.Property(e => e.StreetBid).HasColumnName("StreetBId");

      entity.HasOne(d => d.StreetA).WithMany(p => p.AddressStreetAs)
          .HasForeignKey(d => d.StreetAid)
          .HasConstraintName("fk_Address_Street_A");

      entity.HasOne(d => d.StreetB).WithMany(p => p.AddressStreetBs)
          .HasForeignKey(d => d.StreetBid)
          .HasConstraintName("fk_Address_Street_B");

      entity.HasOne(d => d.Street).WithMany(p => p.AddressStreets)
          .HasForeignKey(d => d.StreetId)
          .OnDelete(DeleteBehavior.ClientSetNull)
          .HasConstraintName("fk_Address_Street");
    });

    modelBuilder.Entity<City>(entity =>
    {
      entity.HasKey(e => e.Id).HasName("pk_City");

      entity.ToTable("City");

      entity.Property(e => e.Iso).HasMaxLength(10);

      entity.HasOne(d => d.Country).WithMany(p => p.Cities)
          .HasForeignKey(d => d.CountryId)
          .OnDelete(DeleteBehavior.ClientSetNull)
          .HasConstraintName("fk_City_Country");
    });

    modelBuilder.Entity<Country>(entity =>
    {
      entity.HasKey(e => e.Id).HasName("pk_Country");

      entity.ToTable("Country");

      entity.Property(e => e.Iso2).HasMaxLength(5);
      entity.Property(e => e.Iso3).HasMaxLength(5);
      entity.Property(e => e.Region).HasMaxLength(120);
    });

    modelBuilder.Entity<Locality>(entity =>
    {
      entity.HasKey(e => e.Id).HasName("pk_Locality");

      entity.ToTable("Locality");

      entity.Property(e => e.Latitude).HasColumnType("decimal(16, 8)");
      entity.Property(e => e.Longitude).HasColumnType("decimal(16, 8)");

      entity.HasOne(d => d.Municipality).WithMany(p => p.Localities)
          .HasForeignKey(d => d.MunicipalityId)
          .OnDelete(DeleteBehavior.ClientSetNull)
          .HasConstraintName("fk_Locality_Municipality");

      entity.HasIndex(e => new { e.MunicipalityId, e.Name })
          .IsUnique()
          .HasDatabaseName("ux_Locality_Municipality_Name");
    });

    modelBuilder.Entity<Municipality>(entity =>
    {
      entity.HasKey(e => e.Id).HasName("pk_Municipality");

      entity.ToTable("Municipality");

      entity.Property(e => e.Iso).HasMaxLength(10);

      entity.HasOne(d => d.City).WithMany(p => p.Municipalities)
          .HasForeignKey(d => d.CityId)
          .OnDelete(DeleteBehavior.ClientSetNull)
          .HasConstraintName("fk_Municipality_City");
    });

    modelBuilder.Entity<Settlement>(entity =>
    {
      entity.HasKey(e => e.Id).HasName("pk_Settlement");

      entity.ToTable("Settlement");

      entity.Property(e => e.ZipCode).HasMaxLength(20);

      entity.HasOne(d => d.Locality).WithMany(p => p.Settlements)
          .HasForeignKey(d => d.LocalityId)
          .OnDelete(DeleteBehavior.ClientSetNull)
          .HasConstraintName("fk_Settlement_Locality");

      entity.HasOne(d => d.SettlementType).WithMany(p => p.Settlements)
          .HasForeignKey(d => d.SettlementTypeId)
          .OnDelete(DeleteBehavior.ClientSetNull)
          .HasConstraintName("fk_Settlement_Type");

      entity.HasIndex(e => new { e.LocalityId, e.ZipCode, e.Name })
          .IsUnique()
          .HasDatabaseName("ux_Settlement_Locality_ZipCode_Name");
    });

    modelBuilder.Entity<SettlementType>(entity =>
    {
      entity.HasKey(e => e.Id).HasName("pk_SettlementType");

      entity.ToTable("SettlementType");
    });

    modelBuilder.Entity<Street>(entity =>
    {
      entity.HasKey(e => e.Id).HasName("pk_Street");

      entity.ToTable("Street");

      entity.HasOne(d => d.Settlement).WithMany(p => p.Streets)
          .HasForeignKey(d => d.SettlementId)
          .OnDelete(DeleteBehavior.ClientSetNull)
          .HasConstraintName("fk_StreetSettlement");

      entity.HasIndex(e => new { e.SettlementId, e.Name })
          .IsUnique()
          .HasDatabaseName("ux_Street_Settlement_Name");
    });
  }
}
