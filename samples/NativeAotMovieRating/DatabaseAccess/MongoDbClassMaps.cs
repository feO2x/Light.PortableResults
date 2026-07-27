using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace NativeAotMovieRating.DatabaseAccess;

/// <summary>
/// Describes how the entities map onto BSON documents. Ratings are embedded in their movie instead
/// of living in a collection of their own: they are only ever read and written through the movie
/// they belong to, which makes the movie the natural aggregate - and makes writing one back a
/// single atomic document operation.
/// </summary>
public static class MongoDbClassMaps
{
    public const string MoviesCollectionName = "movies";
    private const string ConventionPackName = "MovieRatingConventions";

    public static void Register()
    {
        // Guid has no single canonical BSON representation, so the driver insists on being told
        // which one to use. Standard is subtype 4, the representation every current driver agrees on.
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        ConventionRegistry.Register(
            ConventionPackName,
            new ConventionPack { new CamelCaseElementNameConvention() },
            _ => true
        );

        // AutoMap rather than mapping the members by hand, because Id is declared on GuidEntity and
        // BsonClassMap only accepts explicit member mappings for the class it is registered for.
        // AutoMap walks the base classes and lets the id convention pick Id up as _id.
        BsonClassMap.TryRegisterClassMap<Movie>(map => map.AutoMap());
        BsonClassMap.TryRegisterClassMap<MovieRating>(map => map.AutoMap());
    }
}
