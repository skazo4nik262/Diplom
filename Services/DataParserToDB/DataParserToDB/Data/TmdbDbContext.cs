using Microsoft.EntityFrameworkCore;
using DataParserToDB.Data.Entities;

namespace DataParserToDB.Data;

public class TmdbDbContext : DbContext
{
    public TmdbDbContext(DbContextOptions<TmdbDbContext> options) : base(options)
    {
    }

    public DbSet<MovieEntity> Movies => Set<MovieEntity>();
    public DbSet<PersonEntity> People => Set<PersonEntity>();
    public DbSet<GenreEntity> Genres => Set<GenreEntity>();
    public DbSet<ProductionCompanyEntity> ProductionCompanies => Set<ProductionCompanyEntity>();
    public DbSet<ProductionCountryEntity> ProductionCountries => Set<ProductionCountryEntity>();
    public DbSet<SpokenLanguageEntity> SpokenLanguages => Set<SpokenLanguageEntity>();
    public DbSet<KeywordEntity> Keywords => Set<KeywordEntity>();
    public DbSet<CollectionEntity> Collections => Set<CollectionEntity>();
    public DbSet<MovieCastEntity> MovieCast => Set<MovieCastEntity>();
    public DbSet<MovieCrewEntity> MovieCrew => Set<MovieCrewEntity>();
    public DbSet<VideoEntity> Videos => Set<VideoEntity>();
    public DbSet<AlternativeTitleEntity> AlternativeTitles => Set<AlternativeTitleEntity>();
    public DbSet<ReleaseDateEntity> ReleaseDates => Set<ReleaseDateEntity>();
    public DbSet<ReviewEntity> Reviews => Set<ReviewEntity>();
    public DbSet<ImageDataEntity> Images => Set<ImageDataEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<UserMovieEntity> UserMovies => Set<UserMovieEntity>();
    public DbSet<UserPlaylistEntity> UserPlaylists => Set<UserPlaylistEntity>();
    public DbSet<UserPlaylistItemEntity> UserPlaylistItems => Set<UserPlaylistItemEntity>();
    public DbSet<MovieEmbeddingEntity> MovieEmbeddings => Set<MovieEmbeddingEntity>();
    public DbSet<UserTasteEntity> UserTaste => Set<UserTasteEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MovieEntity>(entity =>
        {
            entity.ToTable("Movies");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title).IsUnicode().HasMaxLength(1000);
            entity.Property(e => e.OriginalTitle).IsUnicode().HasMaxLength(1000);
            entity.Property(e => e.Overview).IsUnicode();
            entity.Property(e => e.Tagline).IsUnicode().HasMaxLength(1000);
            entity.Property(e => e.Homepage).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(500);
            entity.Property(e => e.OriginalLanguage).HasMaxLength(500);
            entity.Property(e => e.ImdbId).HasMaxLength(200);
            entity.Property(e => e.BackdropPath).HasMaxLength(500);
            entity.Property(e => e.PosterPath).HasMaxLength(500);

            entity.HasIndex(e => e.ReleaseDate);
            entity.HasIndex(e => e.VoteAverage);
            entity.HasIndex(e => e.Popularity);
            entity.HasIndex(e => e.OriginalLanguage);
            entity.HasIndex(e => e.Title);

            entity.HasOne(e => e.Collection)
                  .WithMany(c => c.Movies)
                  .HasForeignKey(e => e.BelongsToCollectionId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.OwnsOne(e => e.ExternalIds, ext =>
            {
                ext.Property(p => p.FreebaseId).HasMaxLength(500).HasColumnName("FreebaseId");
                ext.Property(p => p.FreebaseMid).HasMaxLength(500).HasColumnName("FreebaseMid");
                ext.Property(p => p.TvrageId).HasMaxLength(500).HasColumnName("TvrageId");
                ext.Property(p => p.WikidataId).HasMaxLength(500).HasColumnName("WikidataId");
                ext.Property(p => p.FacebookId).HasMaxLength(200).HasColumnName("FacebookId");
                ext.Property(p => p.TwitterId).HasMaxLength(200).HasColumnName("TwitterId");
                ext.Property(p => p.InstagramId).HasMaxLength(200).HasColumnName("InstagramId");
                ext.Property(p => p.TvdbId).HasMaxLength(500).HasColumnName("TvdbId");
            });

            entity.HasMany(e => e.Genres)
                  .WithMany(g => g.Movies)
                  .UsingEntity<Dictionary<string, object>>("MovieGenres",
                      j => j.HasOne<GenreEntity>().WithMany().HasForeignKey("GenreId"),
                      j => j.HasOne<MovieEntity>().WithMany().HasForeignKey("MovieId"),
                      j =>
                      {
                          j.HasKey("MovieId", "GenreId");
                          j.ToTable("MovieGenres");
                      });

            entity.HasMany(e => e.ProductionCompanies)
                  .WithMany(g => g.Movies)
                  .UsingEntity<Dictionary<string, object>>("MovieProductionCompanies",
                      j => j.HasOne<ProductionCompanyEntity>().WithMany().HasForeignKey("CompanyId"),
                      j => j.HasOne<MovieEntity>().WithMany().HasForeignKey("MovieId"),
                      j =>
                      {
                          j.HasKey("MovieId", "CompanyId");
                          j.ToTable("MovieProductionCompanies");
                      });

            entity.HasMany(e => e.ProductionCountries)
                  .WithMany(g => g.Movies)
                  .UsingEntity<Dictionary<string, object>>("MovieProductionCountries",
                      j => j.HasOne<ProductionCountryEntity>().WithMany().HasForeignKey("Iso3166_1"),
                      j => j.HasOne<MovieEntity>().WithMany().HasForeignKey("MovieId"),
                      j =>
                      {
                          j.HasKey("MovieId", "Iso3166_1");
                          j.ToTable("MovieProductionCountries");
                      });

            entity.HasMany(e => e.SpokenLanguages)
                  .WithMany(g => g.Movies)
                  .UsingEntity<Dictionary<string, object>>("MovieSpokenLanguages",
                      j => j.HasOne<SpokenLanguageEntity>().WithMany().HasForeignKey("Iso639_1"),
                      j => j.HasOne<MovieEntity>().WithMany().HasForeignKey("MovieId"),
                      j =>
                      {
                          j.HasKey("MovieId", "Iso639_1");
                          j.ToTable("MovieSpokenLanguages");
                      });

            entity.HasMany(e => e.Keywords)
                  .WithMany(g => g.Movies)
                  .UsingEntity<Dictionary<string, object>>("MovieKeywords",
                      j => j.HasOne<KeywordEntity>().WithMany().HasForeignKey("KeywordId"),
                      j => j.HasOne<MovieEntity>().WithMany().HasForeignKey("MovieId"),
                      j =>
                      {
                          j.HasKey("MovieId", "KeywordId");
                          j.ToTable("MovieKeywords");
                      });

            entity.HasMany(e => e.Cast)
                  .WithOne(c => c.Movie)
                  .HasForeignKey(c => c.MovieId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Crew)
                  .WithOne(c => c.Movie)
                  .HasForeignKey(c => c.MovieId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Videos)
                  .WithOne(v => v.Movie)
                  .HasForeignKey(v => v.MovieId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.AlternativeTitles)
                  .WithOne(a => a.Movie)
                  .HasForeignKey(a => a.MovieId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ReleaseDates)
                  .WithOne(r => r.Movie)
                  .HasForeignKey(r => r.MovieId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Reviews)
                  .WithOne(r => r.Movie)
                  .HasForeignKey(r => r.MovieId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Images)
                  .WithOne(i => i.Movie)
                  .HasForeignKey(i => i.MovieId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonEntity>(entity =>
        {
            entity.ToTable("People");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).IsUnicode().HasMaxLength(500);
            entity.Property(e => e.Biography).IsUnicode();
            entity.Property(e => e.PlaceOfBirth).IsUnicode().HasMaxLength(500);
            entity.Property(e => e.Homepage).HasMaxLength(500);
            entity.Property(e => e.ImdbId).HasMaxLength(200);
            entity.Property(e => e.ProfilePath).HasMaxLength(500);
            entity.Property(e => e.Gender).HasMaxLength(200);
            entity.Property(e => e.KnownForDepartment).HasMaxLength(500);

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Popularity);

            entity.OwnsOne(e => e.ExternalIds, ext =>
            {
                ext.Property(p => p.FreebaseId).HasMaxLength(500).HasColumnName("FreebaseId");
                ext.Property(p => p.FreebaseMid).HasMaxLength(500).HasColumnName("FreebaseMid");
                ext.Property(p => p.TvrageId).HasMaxLength(500).HasColumnName("TvrageId");
                ext.Property(p => p.WikidataId).HasMaxLength(500).HasColumnName("WikidataId");
                ext.Property(p => p.FacebookId).HasMaxLength(200).HasColumnName("FacebookId");
                ext.Property(p => p.TwitterId).HasMaxLength(200).HasColumnName("TwitterId");
                ext.Property(p => p.InstagramId).HasMaxLength(200).HasColumnName("InstagramId");
                ext.Property(p => p.TvdbId).HasMaxLength(500).HasColumnName("TvdbId");
            });
        });

        modelBuilder.Entity<GenreEntity>(entity =>
        {
            entity.ToTable("Genres");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsUnicode().HasMaxLength(500);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<ProductionCompanyEntity>(entity =>
        {
            entity.ToTable("ProductionCompanies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsUnicode().HasMaxLength(500);
            entity.Property(e => e.LogoPath).HasMaxLength(500);
            entity.Property(e => e.OriginCountry).HasMaxLength(500);
        });

        modelBuilder.Entity<ProductionCountryEntity>(entity =>
        {
            entity.ToTable("ProductionCountries");
            entity.HasKey(e => e.Iso3166_1);
            entity.Property(e => e.Iso3166_1).HasMaxLength(500);
            entity.Property(e => e.Name).IsUnicode().HasMaxLength(200);
        });

        modelBuilder.Entity<SpokenLanguageEntity>(entity =>
        {
            entity.ToTable("SpokenLanguages");
            entity.HasKey(e => e.Iso639_1);
            entity.Property(e => e.Iso639_1).HasMaxLength(500);
            entity.Property(e => e.Name).IsUnicode().HasMaxLength(200);
        });

        modelBuilder.Entity<KeywordEntity>(entity =>
        {
            entity.ToTable("Keywords");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsUnicode().HasMaxLength(200);
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<CollectionEntity>(entity =>
        {
            entity.ToTable("Collections");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsUnicode().HasMaxLength(500);
            entity.Property(e => e.Overview).IsUnicode();
            entity.Property(e => e.PosterPath).HasMaxLength(500);
            entity.Property(e => e.BackdropPath).HasMaxLength(500);
        });

        modelBuilder.Entity<MovieCastEntity>(entity =>
        {
            entity.ToTable("MovieCast");
            entity.HasKey(e => new { e.MovieId, e.CreditId });

            entity.Property(e => e.CreditId).HasMaxLength(500);
            entity.Property(e => e.Character).IsUnicode().HasMaxLength(500);
            entity.Property(e => e.Gender).HasMaxLength(200);
            entity.Property(e => e.KnownForDepartment).HasMaxLength(500);
            entity.Property(e => e.OriginalName).IsUnicode().HasMaxLength(500);
            entity.Property(e => e.ProfilePath).HasMaxLength(500);

            entity.HasIndex(e => e.PersonId);
            entity.HasIndex(e => e.MovieId);

            entity.HasOne(e => e.Person)
                  .WithMany(p => p.CastMovies)
                  .HasForeignKey(e => e.PersonId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MovieCrewEntity>(entity =>
        {
            entity.ToTable("MovieCrew");
            entity.HasKey(e => new { e.MovieId, e.CreditId });

            entity.Property(e => e.CreditId).HasMaxLength(500);
            entity.Property(e => e.Department).HasMaxLength(500);
            entity.Property(e => e.Job).HasMaxLength(200);
            entity.Property(e => e.Gender).HasMaxLength(200);
            entity.Property(e => e.KnownForDepartment).HasMaxLength(500);
            entity.Property(e => e.OriginalName).IsUnicode().HasMaxLength(500);
            entity.Property(e => e.ProfilePath).HasMaxLength(500);

            entity.HasIndex(e => e.PersonId);
            entity.HasIndex(e => e.MovieId);
            entity.HasIndex(e => e.Job);

            entity.HasOne(e => e.Person)
                  .WithMany(p => p.CrewMovies)
                  .HasForeignKey(e => e.PersonId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VideoEntity>(entity =>
        {
            entity.ToTable("Videos");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasMaxLength(500);
            entity.Property(e => e.Iso3166_1).HasMaxLength(500);
            entity.Property(e => e.Iso639_1).HasMaxLength(500);
            entity.Property(e => e.Key).HasMaxLength(500);
            entity.Property(e => e.Name).IsUnicode().HasMaxLength(500);
            entity.Property(e => e.Site).HasMaxLength(500);
            entity.Property(e => e.Type).HasMaxLength(500);

            entity.HasIndex(e => e.MovieId);
        });

        modelBuilder.Entity<AlternativeTitleEntity>(entity =>
        {
            entity.ToTable("AlternativeTitles");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Iso3166_1).HasMaxLength(500);
            entity.Property(e => e.Title).IsUnicode().HasMaxLength(1000);
            entity.Property(e => e.Type).HasMaxLength(500);

            entity.HasIndex(e => e.MovieId);
        });

        modelBuilder.Entity<ReleaseDateEntity>(entity =>
        {
            entity.ToTable("ReleaseDates");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Iso3166_1).HasMaxLength(500);
            entity.Property(e => e.Certification).HasMaxLength(500);
            entity.Property(e => e.Iso639_1).HasMaxLength(500);
            entity.Property(e => e.Note).IsUnicode().HasMaxLength(1000);

            entity.HasIndex(e => e.MovieId);
        });

        modelBuilder.Entity<ReviewEntity>(entity =>
        {
            entity.ToTable("Reviews");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasMaxLength(500);
            entity.Property(e => e.Author).HasMaxLength(200);
            entity.Property(e => e.AuthorName).IsUnicode().HasMaxLength(500);
            entity.Property(e => e.AuthorUsername).HasMaxLength(200);
            entity.Property(e => e.AuthorAvatarPath).HasMaxLength(500);
            entity.Property(e => e.Content).IsUnicode();
            entity.Property(e => e.Url).HasMaxLength(1000);
            entity.Property(e => e.Iso639_1).HasMaxLength(500);
            entity.Property(e => e.MediaTitle).IsUnicode().HasMaxLength(1000);
            entity.Property(e => e.MediaType).HasMaxLength(500);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.MovieId);
        });

        modelBuilder.Entity<ImageDataEntity>(entity =>
        {
            entity.ToTable("Images");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FilePath).HasMaxLength(500);
            entity.Property(e => e.Iso639_1).HasMaxLength(500);
            entity.Property(e => e.Iso3166_1).HasMaxLength(500);
            entity.Property(e => e.Type).HasMaxLength(200);

            entity.HasIndex(e => e.MovieId);
            entity.HasIndex(e => e.Type);
        });

        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Login).HasMaxLength(255);
            entity.Property(e => e.PasswordHash).HasColumnName("user_password").HasMaxLength(500);
            entity.Property(e => e.Role).HasColumnName("user_role").HasDefaultValue(1);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.Login).IsUnique();
        });

        modelBuilder.Entity<UserMovieEntity>(entity =>
        {
            entity.ToTable("UserMovies");
            entity.HasKey(e => new { e.UserId, e.MovieId });
            entity.Property(e => e.Status).HasMaxLength(200);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Movie).WithMany().HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserPlaylistEntity>(entity =>
        {
            entity.ToTable("UserPlaylists");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(500).IsRequired();
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserPlaylistItemEntity>(entity =>
        {
            entity.ToTable("UserPlaylistItems");
            entity.HasKey(e => new { e.PlaylistId, e.MovieId });
            entity.HasOne(e => e.Playlist).WithMany(c => c.Items).HasForeignKey(e => e.PlaylistId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Movie).WithMany().HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MovieEmbeddingEntity>(entity =>
        {
            entity.ToTable("MovieEmbeddings");
            entity.HasKey(e => e.MovieId);
            entity.Property(e => e.Embedding);
            entity.HasOne(e => e.Movie).WithMany().HasForeignKey(e => e.MovieId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserTasteEntity>(entity =>
        {
            entity.ToTable("UserTaste");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Embedding);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
