using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Reviews;
using TMDbLib.Objects.Search;
using DataParserToDB.Data.Entities;

namespace DataParserToDB.Data.Mappers;

public static class MovieMapper
{
    public static MovieEntity ToEntity(this Movie movie)
    {
        var entity = new MovieEntity
        {
            Id = movie.Id,
            Adult = movie.Adult,
            BackdropPath = movie.BackdropPath,
            BelongsToCollectionId = movie.BelongsToCollection?.Id,
            Budget = movie.Budget,
            Homepage = movie.Homepage,
            ImdbId = movie.ImdbId,
            OriginalLanguage = movie.OriginalLanguage,
            OriginalTitle = movie.OriginalTitle,
            Overview = movie.Overview,
            Popularity = movie.Popularity,
            PosterPath = movie.PosterPath,
            ReleaseDate = movie.ReleaseDate,
            Revenue = movie.Revenue,
            Runtime = movie.Runtime,
            Status = movie.Status,
            Tagline = movie.Tagline,
            Title = movie.Title,
            Video = movie.Video,
            VoteAverage = movie.VoteAverage,
            VoteCount = movie.VoteCount,
            ExternalIds = movie.ExternalIds?.ToEntity(),
        };

        if (movie.BelongsToCollection is not null)
            entity.Collection = movie.BelongsToCollection.ToEntity();

        return entity;
    }

    public static CollectionEntity ToEntity(this SearchCollection collection)
    {
        return new CollectionEntity
        {
            Id = collection.Id,
            Name = collection.Name,
            Overview = collection.Overview,
            PosterPath = collection.PosterPath,
            BackdropPath = collection.BackdropPath,
        };
    }

    public static ExternalIdsEntity? ToEntity(this ExternalIdsMovie? ids)
    {
        if (ids is null) return null;

        return new ExternalIdsEntity
        {
            FreebaseId = ids.FreebaseId,
            FreebaseMid = ids.FreebaseMid,
            TvrageId = ids.TvrageId,
            WikidataId = ids.WikidataId,
            FacebookId = ids.FacebookId,
            TwitterId = ids.TwitterId,
            InstagramId = ids.InstagramId,
        };
    }

    public static ExternalIdsEntity? ToEntity(this ExternalIdsPerson? ids)
    {
        if (ids is null) return null;

        return new ExternalIdsEntity
        {
            FreebaseId = ids.FreebaseId,
            FreebaseMid = ids.FreebaseMid,
            TvrageId = ids.TvrageId,
            WikidataId = ids.WikidataId,
            FacebookId = ids.FacebookId,
            TwitterId = ids.TwitterId,
            InstagramId = ids.InstagramId,
        };
    }

    public static ExternalIdsEntity? ToEntity(this ExternalIdsTvShow? ids)
    {
        if (ids is null) return null;

        return new ExternalIdsEntity
        {
            FreebaseId = ids.FreebaseId,
            FreebaseMid = ids.FreebaseMid,
            TvrageId = ids.TvrageId,
            WikidataId = ids.WikidataId,
            FacebookId = ids.FacebookId,
            TwitterId = ids.TwitterId,
            InstagramId = ids.InstagramId,
            TvdbId = ids.TvdbId,
        };
    }

    public static GenreEntity ToEntity(this Genre genre)
    {
        return new GenreEntity
        {
            Id = genre.Id,
            Name = genre.Name,
        };
    }

    public static ProductionCompanyEntity ToEntity(this ProductionCompany company)
    {
        return new ProductionCompanyEntity
        {
            Id = company.Id,
            Name = company.Name,
            LogoPath = company.LogoPath,
            OriginCountry = company.OriginCountry,
        };
    }

    public static ProductionCountryEntity ToEntity(this ProductionCountry country)
    {
        return new ProductionCountryEntity
        {
            Iso3166_1 = country.Iso_3166_1 ?? "",
            Name = country.Name,
        };
    }

    public static SpokenLanguageEntity ToEntity(this SpokenLanguage language)
    {
        return new SpokenLanguageEntity
        {
            Iso639_1 = language.Iso_639_1 ?? "",
            Name = language.Name,
        };
    }

    public static KeywordEntity ToEntity(this Keyword keyword)
    {
        return new KeywordEntity
        {
            Id = keyword.Id,
            Name = keyword.Name,
        };
    }

    public static List<MovieCastEntity> ToCastEntities(this Movie movie)
    {
        var result = new List<MovieCastEntity>();
        if (movie.Credits?.Cast is null) return result;

        foreach (var cast in movie.Credits.Cast)
        {
            result.Add(new MovieCastEntity
            {
                MovieId = movie.Id,
                PersonId = cast.Id,
                CreditId = cast.CreditId ?? $"{movie.Id}_{cast.Id}_{cast.CastId}",
                CastId = cast.CastId,
                Character = cast.Character,
                Order = cast.Order,
                Adult = cast.Adult,
                Gender = cast.Gender.ToString(),
                KnownForDepartment = cast.KnownForDepartment,
                OriginalName = cast.OriginalName,
                Popularity = cast.Popularity,
                ProfilePath = cast.ProfilePath,
            });
        }

        return result;
    }

    public static List<MovieCrewEntity> ToCrewEntities(this Movie movie)
    {
        var result = new List<MovieCrewEntity>();
        if (movie.Credits?.Crew is null) return result;

        foreach (var crew in movie.Credits.Crew)
        {
            result.Add(new MovieCrewEntity
            {
                MovieId = movie.Id,
                PersonId = crew.Id,
                CreditId = crew.CreditId ?? $"{movie.Id}_{crew.Id}_{crew.Department}_{crew.Job}",
                Department = crew.Department,
                Job = crew.Job,
                Adult = crew.Adult,
                Gender = crew.Gender.ToString(),
                KnownForDepartment = crew.KnownForDepartment,
                OriginalName = crew.OriginalName,
                Popularity = crew.Popularity,
                ProfilePath = crew.ProfilePath,
            });
        }

        return result;
    }

    public static List<VideoEntity> ToVideoEntities(this Movie movie)
    {
        var result = new List<VideoEntity>();
        if (movie.Videos?.Results is null) return result;

        foreach (var video in movie.Videos.Results)
        {
            result.Add(new VideoEntity
            {
                Id = video.Id ?? Guid.NewGuid().ToString(),
                MovieId = movie.Id,
                Iso3166_1 = video.Iso_3166_1,
                Iso639_1 = video.Iso_639_1,
                Key = video.Key,
                Name = video.Name,
                Official = video.Official,
                PublishedAt = video.PublishedAt,
                Site = video.Site,
                Size = video.Size,
                Type = video.Type,
            });
        }

        return result;
    }

    public static List<AlternativeTitleEntity> ToAlternativeTitleEntities(this Movie movie)
    {
        var result = new List<AlternativeTitleEntity>();
        if (movie.AlternativeTitles?.Titles is null) return result;

        int id = 0;
        foreach (var title in movie.AlternativeTitles.Titles)
        {
            result.Add(new AlternativeTitleEntity
            {
                Id = ++id,
                MovieId = movie.Id,
                Iso3166_1 = title.Iso_3166_1,
                Title = title.Title,
                Type = title.Type,
            });
        }

        return result;
    }

    public static List<ReleaseDateEntity> ToReleaseDateEntities(this Movie movie)
    {
        var result = new List<ReleaseDateEntity>();
        if (movie.ReleaseDates?.Results is null) return result;

        int id = 0;
        foreach (var container in movie.ReleaseDates.Results)
        {
            if (container.ReleaseDates is null) continue;

            foreach (var item in container.ReleaseDates)
            {
                result.Add(new ReleaseDateEntity
                {
                    Id = ++id,
                    MovieId = movie.Id,
                    Iso3166_1 = container.Iso_3166_1,
                    Certification = item.Certification,
                    Iso639_1 = item.Iso_639_1,
                    Note = item.Note,
                    ReleaseDate = item.ReleaseDate,
                    Type = (int)item.Type,
                });
            }
        }

        return result;
    }

    public static List<ReviewEntity> ToReviewEntities(this Movie movie)
    {
        var result = new List<ReviewEntity>();
        if (movie.Reviews?.Results is null) return result;

        foreach (var review in movie.Reviews.Results)
        {
            result.Add(review.ToEntity(movie.Id));
        }

        return result;
    }

    public static ReviewEntity ToEntity(this ReviewBase review, int movieId)
    {
        return new ReviewEntity
        {
            Id = review.Id ?? Guid.NewGuid().ToString(),
            MovieId = movieId,
            Author = review.Author,
            AuthorName = review.AuthorDetails?.Name,
            AuthorUsername = review.AuthorDetails?.Username,
            AuthorAvatarPath = review.AuthorDetails?.AvatarPath,
            AuthorRating = review.AuthorDetails?.Rating,
            Content = review.Content,
            Url = review.Url,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt,

            Iso639_1 = review is Review r ? r.Iso_639_1 : null,
            MediaId = review is Review r2 ? r2.MediaId : 0,
            MediaTitle = review is Review r3 ? r3.MediaTitle : null,
            MediaType = review is Review r4 ? r4.MediaType.ToString() : null,
        };
    }

    public static List<ImageDataEntity> ToImageEntities(this Movie movie)
    {
        var result = new List<ImageDataEntity>();
        if (movie.Images is null) return result;

        int id = 0;

        if (movie.Images.Backdrops is not null)
        {
            foreach (var img in movie.Images.Backdrops)
            {
                result.Add(img.ToEntity(movie.Id, "backdrop", ++id));
            }
        }

        if (movie.Images.Posters is not null)
        {
            foreach (var img in movie.Images.Posters)
            {
                result.Add(img.ToEntity(movie.Id, "poster", ++id));
            }
        }

        if (movie.Images.Logos is not null)
        {
            foreach (var img in movie.Images.Logos)
            {
                result.Add(img.ToEntity(movie.Id, "logo", ++id));
            }
        }

        return result;
    }

    public static List<PersonEntity> ToPersonEntities(this Movie movie)
    {
        var people = new Dictionary<int, PersonEntity>();

        if (movie.Credits?.Cast is not null)
        {
            foreach (var cast in movie.Credits.Cast)
            {
                if (people.ContainsKey(cast.Id)) continue;
                people[cast.Id] = new PersonEntity
                {
                    Id = cast.Id,
                    Name = cast.Name,
                    Adult = cast.Adult,
                    Gender = cast.Gender.ToString(),
                    KnownForDepartment = cast.KnownForDepartment,
                    Popularity = cast.Popularity,
                    ProfilePath = cast.ProfilePath,
                };
            }
        }

        if (movie.Credits?.Crew is not null)
        {
            foreach (var crew in movie.Credits.Crew)
            {
                if (people.ContainsKey(crew.Id)) continue;
                people[crew.Id] = new PersonEntity
                {
                    Id = crew.Id,
                    Name = crew.Name,
                    Adult = crew.Adult,
                    Gender = crew.Gender.ToString(),
                    KnownForDepartment = crew.KnownForDepartment,
                    Popularity = crew.Popularity,
                    ProfilePath = crew.ProfilePath,
                };
            }
        }

        return people.Values.ToList();
    }

    private static ImageDataEntity ToEntity(this ImageData image, int movieId, string type, int id)
    {
        return new ImageDataEntity
        {
            Id = id,
            MovieId = movieId,
            FilePath = image.FilePath,
            AspectRatio = image.AspectRatio,
            Height = image.Height,
            Width = image.Width,
            Iso639_1 = image.Iso_639_1,
            Iso3166_1 = image.Iso_3166_1,
            VoteAverage = image.VoteAverage,
            VoteCount = image.VoteCount,
            Type = type,
        };
    }
}
