namespace DataParserToDB.Data.Entities;

public class MovieEntity
{
    public int Id { get; set; }
    public bool Adult { get; set; }
    public string? BackdropPath { get; set; }
    public int? BelongsToCollectionId { get; set; }
    public long Budget { get; set; }
    public string? Homepage { get; set; }
    public string? ImdbId { get; set; }
    public string? OriginalLanguage { get; set; }
    public string? OriginalTitle { get; set; }
    public string? Overview { get; set; }
    public double? Popularity { get; set; }
    public string? PosterPath { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public long Revenue { get; set; }
    public int? Runtime { get; set; }
    public string? Status { get; set; }
    public string? Tagline { get; set; }
    public string? Title { get; set; }
    public bool Video { get; set; }
    public double VoteAverage { get; set; }
    public int VoteCount { get; set; }
    public DateTime? RefreshedAt { get; set; }

    public CollectionEntity? Collection { get; set; }
    public ExternalIdsEntity? ExternalIds { get; set; }

    public ICollection<GenreEntity> Genres { get; set; } = new List<GenreEntity>();
    public ICollection<ProductionCompanyEntity> ProductionCompanies { get; set; } = new List<ProductionCompanyEntity>();
    public ICollection<ProductionCountryEntity> ProductionCountries { get; set; } = new List<ProductionCountryEntity>();
    public ICollection<SpokenLanguageEntity> SpokenLanguages { get; set; } = new List<SpokenLanguageEntity>();
    public ICollection<KeywordEntity> Keywords { get; set; } = new List<KeywordEntity>();
    public ICollection<MovieCastEntity> Cast { get; set; } = new List<MovieCastEntity>();
    public ICollection<MovieCrewEntity> Crew { get; set; } = new List<MovieCrewEntity>();
    public ICollection<VideoEntity> Videos { get; set; } = new List<VideoEntity>();
    public ICollection<AlternativeTitleEntity> AlternativeTitles { get; set; } = new List<AlternativeTitleEntity>();
    public ICollection<ReleaseDateEntity> ReleaseDates { get; set; } = new List<ReleaseDateEntity>();
    public ICollection<ReviewEntity> Reviews { get; set; } = new List<ReviewEntity>();
    public ICollection<ImageDataEntity> Images { get; set; } = new List<ImageDataEntity>();
}
