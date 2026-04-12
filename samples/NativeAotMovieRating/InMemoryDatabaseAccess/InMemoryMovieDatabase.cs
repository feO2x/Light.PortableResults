using System;
using System.Collections.Generic;

namespace NativeAotMovieRating.InMemoryDatabaseAccess;

public sealed class InMemoryMovieDatabase
{
    public List<Movie> Movies { get; } =
    [
        new ()
        {
            Id = Guid.Parse("5C200E1D-4A16-4572-B884-E3A3957771FC"),
            Title = "The Matrix",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("5C200E1D-4A16-4572-B884-E3A3957771FC"), UserName = "Neo",
                    Rating = 5, Comment = "Whoa."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("917B94F3-CE02-4695-97B5-8C1658D31CC3"),
            Title = "Lord of the Rings",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("917B94F3-CE02-4695-97B5-8C1658D31CC3"),
                    UserName = "Frodo", Rating = 5, Comment = "Precious!"
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("8D99222E-352D-494F-9457-37A35F0A6B22"),
            Title = "Inception",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("8D99222E-352D-494F-9457-37A35F0A6B22"),
                    UserName = "Cobb", Rating = 4, Comment = "Deep."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("64C93D64-C25F-4E7B-8B33-317A5A90E5B5"),
            Title = "Interstellar",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("64C93D64-C25F-4E7B-8B33-317A5A90E5B5"),
                    UserName = "Cooper", Rating = 5, Comment = "Don't let me leave Murph!"
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("67B665BB-8F30-464A-A91B-943C88060D9C"),
            Title = "The Dark Knight",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("67B665BB-8F30-464A-A91B-943C88060D9C"),
                    UserName = "Joker", Rating = 5, Comment = "Why so serious?"
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("868B5E87-D10E-4C3D-80B9-72E56B641B66"),
            Title = "Pulp Fiction",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("868B5E87-D10E-4C3D-80B9-72E56B641B66"),
                    UserName = "Jules", Rating = 5, Comment = "Royale with cheese."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("6D6E1289-4A00-410E-9040-363E75F57262"),
            Title = "Schindler's List",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("6D6E1289-4A00-410E-9040-363E75F57262"),
                    UserName = "Oscar", Rating = 5, Comment = "Powerful."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("7C5D766D-5D9E-4364-8140-5C138E137D2C"),
            Title = "The Shawshank Redemption",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("7C5D766D-5D9E-4364-8140-5C138E137D2C"),
                    UserName = "Andy", Rating = 5, Comment = "Get busy living."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("3E817730-C41C-4B9F-848E-531A85E6551A"),
            Title = "Forrest Gump",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("3E817730-C41C-4B9F-848E-531A85E6551A"),
                    UserName = "Forrest", Rating = 4, Comment = "Life is like a box of chocolates."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("B74A8973-2D92-4C18-97F1-30E267D2840D"),
            Title = "Fight Club",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("B74A8973-2D92-4C18-97F1-30E267D2840D"),
                    UserName = "Tyler", Rating = 5, Comment = "First rule..."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("F7954932-B8E5-4720-B955-52B3C5389650"),
            Title = "Goodfellas",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("F7954932-B8E5-4720-B955-52B3C5389650"),
                    UserName = "Henry", Rating = 5, Comment = "Funny how?"
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("29267924-E850-4E80-99E7-B0E3C7585F81"),
            Title = "The Godfather",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("29267924-E850-4E80-99E7-B0E3C7585F81"),
                    UserName = "Vito", Rating = 5, Comment = "Offer he can't refuse."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("6620C716-1051-4074-90E9-4672B6E32115"),
            Title = "12 Angry Men",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("6620C716-1051-4074-90E9-4672B6E32115"),
                    UserName = "Juror8", Rating = 5, Comment = "Brilliant drama."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("981D5E9D-A310-47F1-9788-3E9E0F07E639"),
            Title = "Seven Samurai",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("981D5E9D-A310-47F1-9788-3E9E0F07E639"),
                    UserName = "Kambei", Rating = 5, Comment = "Epic."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("11E0C665-42A9-4A99-92F6-B603D67587E2"),
            Title = "City of God",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("11E0C665-42A9-4A99-92F6-B603D67587E2"),
                    UserName = "Rocket", Rating = 4, Comment = "Intense."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("BD8E2C6A-60B0-4A3C-B7D4-4903E7E6237D"),
            Title = "Spirited Away",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("BD8E2C6A-60B0-4A3C-B7D4-4903E7E6237D"),
                    UserName = "Chihiro", Rating = 5, Comment = "Magical."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("7E732E96-8575-4775-A8F8-86C7C83C0F5E"),
            Title = "The Silence of the Lambs",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("7E732E96-8575-4775-A8F8-86C7C83C0F5E"),
                    UserName = "Hannibal", Rating = 5, Comment = "Nice chianti."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("F56C87C1-D271-41F1-A89E-70C923B2D70F"),
            Title = "Parasite",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("F56C87C1-D271-41F1-A89E-70C923B2D70F"), UserName = "Kim",
                    Rating = 5, Comment = "Masterpiece."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("7C04A02A-2330-4E15-9988-59239D80B5C1"),
            Title = "The Green Mile",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("7C04A02A-2330-4E15-9988-59239D80B5C1"),
                    UserName = "John", Rating = 5, Comment = "Boss!"
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("A3E54564-85C1-4D6F-9721-50798C5D30F4"),
            Title = "The Prestige",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("A3E54564-85C1-4D6F-9721-50798C5D30F4"),
                    UserName = "Angier", Rating = 5, Comment = "Are you watching closely?"
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("D1E2D305-B7D3-4D90-A880-934C7A8679A9"),
            Title = "Gladiator",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("D1E2D305-B7D3-4D90-A880-934C7A8679A9"),
                    UserName = "Maximus", Rating = 5, Comment = "Are you not entertained?"
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("849D64C1-30D8-49A8-9B6C-7F6F1F6D439A"),
            Title = "The Lion King",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("849D64C1-30D8-49A8-9B6C-7F6F1F6D439A"),
                    UserName = "Simba", Rating = 5, Comment = "Hakuna Matata."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("90C1A866-9311-4773-BC3D-5C94697F8799"),
            Title = "The Departed",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("90C1A866-9311-4773-BC3D-5C94697F8799"),
                    UserName = "Billy", Rating = 4, Comment = "Rat."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("A890E6D1-C9D1-4E8F-8E99-568C360C3E5A"),
            Title = "Whiplash",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("A890E6D1-C9D1-4E8F-8E99-568C360C3E5A"),
                    UserName = "Andrew", Rating = 5, Comment = "Not quite my tempo."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("184B8E1D-4A2B-4E9A-A890-E6D1C9D14E8F"),
            Title = "Django Unchained",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("184B8E1D-4A2B-4E9A-A890-E6D1C9D14E8F"),
                    UserName = "Django", Rating = 5, Comment = "I like the way you die, boy."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("D77E1E8B-6E74-4C5D-80B9-72E56B641B66"),
            Title = "The Usual Suspects",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("D77E1E8B-6E74-4C5D-80B9-72E56B641B66"),
                    UserName = "Keyser", Rating = 5, Comment = "Greatest trick..."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("7B3E8177-30C4-1C4B-9F84-8E531A85E655"),
            Title = "Psycho",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("7B3E8177-30C4-1C4B-9F84-8E531A85E655"),
                    UserName = "Norman", Rating = 5, Comment = "Mother!"
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("4A89732D-924C-1897-F130-E267D2840DB7"),
            Title = "Saving Private Ryan",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("4A89732D-924C-1897-F130-E267D2840DB7"),
                    UserName = "Miller", Rating = 5, Comment = "Earn this."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("2B8E5472-0B95-52B3-C538-9650F7954932"),
            Title = "American History X",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("2B8E5472-0B95-52B3-C538-9650F7954932"),
                    UserName = "Derek", Rating = 5, Comment = "Impactful."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("E8504E80-99E7-B0E3-C758-5F8129267924"),
            Title = "Terminator 2: Judgment Day",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("E8504E80-99E7-B0E3-C758-5F8129267924"),
                    UserName = "John", Rating = 5, Comment = "Hasta la vista."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("10514074-90E9-4672-B6E3-21156620C716"),
            Title = "The Pianist",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("10514074-90E9-4672-B6E3-21156620C716"),
                    UserName = "Wladyslaw", Rating = 5, Comment = "Heartbreaking."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("A31047F1-9788-3E9E-0F07-E639981D5E9D"),
            Title = "The Shining",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("A31047F1-9788-3E9E-0F07-E639981D5E9D"),
                    UserName = "Jack", Rating = 5, Comment = "Here's Johnny!"
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("42A94A99-92F6-B603-D675-87E211E0C665"),
            Title = "Alien",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("42A94A99-92F6-B603-D675-87E211E0C665"),
                    UserName = "Ripley", Rating = 5, Comment = "In space, no one can hear you scream."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("60B04A3C-B7D4-4903-E7E6-237DBD8E2C6A"),
            Title = "Back to the Future",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("60B04A3C-B7D4-4903-E7E6-237DBD8E2C6A"),
                    UserName = "Marty", Rating = 5, Comment = "88 miles per hour!"
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("85754775-A8F8-86C7-C83C-0F5E7E732E96"),
            Title = "Coco",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("85754775-A8F8-86C7-C83C-0F5E7E732E96"),
                    UserName = "Miguel", Rating = 5, Comment = "Remember me."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("D27141F1-A89E-70C9-23B2-D70FF56C87C1"),
            Title = "Your Name",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("D27141F1-A89E-70C9-23B2-D70FF56C87C1"),
                    UserName = "Taki", Rating = 5, Comment = "Beautiful."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("23304E15-9988-5923-9D80-B5C17C04A02A"),
            Title = "The Intouchables",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("23304E15-9988-5923-9D80-B5C17C04A02A"),
                    UserName = "Driss", Rating = 5, Comment = "Heartwarming."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("85C14D6F-9721-5079-8C5D-30F4A3E54564"),
            Title = "Spider-Man: Into the Spider-Verse",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("85C14D6F-9721-5079-8C5D-30F4A3E54564"),
                    UserName = "Miles", Rating = 5, Comment = "Leap of faith."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("B7D34D90-A880-934C-7A86-79A9D1E2D305"),
            Title = "Joker",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("B7D34D90-A880-934C-7A86-79A9D1E2D305"),
                    UserName = "Arthur", Rating = 4, Comment = "Put on a happy face."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("30D849A8-9B6C-7F6F-1F6D-439A849D64C1"),
            Title = "Avengers: Infinity War",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("30D849A8-9B6C-7F6F-1F6D-439A849D64C1"),
                    UserName = "Thanos", Rating = 5, Comment = "Perfectly balanced."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("93114773-BC3D-5C94-697F-879990C1A866"),
            Title = "Oldboy",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("93114773-BC3D-5C94-697F-879990C1A866"),
                    UserName = "Dae-su", Rating = 5, Comment = "Shocking."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("C9D14E8F-8E99-568C-360C-3E5AA890E6D1"),
            Title = "Requiem for a Dream",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("C9D14E8F-8E99-568C-360C-3E5AA890E6D1"),
                    UserName = "Harry", Rating = 4, Comment = "Depressing."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("4A2B4E9A-A890-E6D1-C9D1-4E8F184B8E1D"),
            Title = "Toy Story",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("4A2B4E9A-A890-E6D1-C9D1-4E8F184B8E1D"),
                    UserName = "Woody", Rating = 5, Comment = "You've got a friend in me."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("6E744C5D-80B9-72E5-6B64-1B66D77E1E8B"),
            Title = "Eternal Sunshine of the Spotless Mind",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("6E744C5D-80B9-72E5-6B64-1B66D77E1E8B"),
                    UserName = "Joel", Rating = 5, Comment = "Beautifully sad."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("30C41C4B-9F84-8E53-1A85-E6557B3E8177"),
            Title = "The Hunt",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("30C41C4B-9F84-8E53-1A85-E6557B3E8177"),
                    UserName = "Lucas", Rating = 5, Comment = "Infuriatingly good."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("924C1897-F130-E267-D284-0DB74A89732D"),
            Title = "Reservoir Dogs",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("924C1897-F130-E267-D284-0DB74A89732D"),
                    UserName = "Blonde", Rating = 4, Comment = "Stuck in the middle with you."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("0B9552B3-C538-9650-F795-49322B8E5472"),
            Title = "Full Metal Jacket",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("0B9552B3-C538-9650-F795-49322B8E5472"),
                    UserName = "Hartman", Rating = 5, Comment = "Sir, yes sir!"
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("99E7B0E3-C758-5F81-2926-7924E8504E80"),
            Title = "2001: A Space Odyssey",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("99E7B0E3-C758-5F81-2926-7924E8504E80"), UserName = "HAL",
                    Rating = 5, Comment = "I'm sorry Dave."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("90E94672-B6E3-2115-6620-C71610514074"),
            Title = "Braveheart",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("90E94672-B6E3-2115-6620-C71610514074"),
                    UserName = "Wallace", Rating = 5, Comment = "Freedom!"
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("97883E9E-0F07-E639-981D-5E9DA31047F1"),
            Title = "Memento",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("97883E9E-0F07-E639-981D-5E9DA31047F1"),
                    UserName = "Leonard", Rating = 5, Comment = "Don't believe his lies."
                }
            }
        },
        new () { Id = Guid.Parse("92F6B603-D675-87E2-11E0-C66542A94A99"), Title = "Taxi Driver" },
        new () { Id = Guid.Parse("B7D44903-E7E6-237D-BD8E-2C6A60B04A3C"), Title = "Incendies" },
        new () { Id = Guid.Parse("A8F886C7-C83C-0F5E-7E73-2E9685754775"), Title = "Princess Mononoke" },
        new () { Id = Guid.Parse("A89E70C9-23B2-D70F-F56C-87C1D27141F1"), Title = "Green Book" },
        new () { Id = Guid.Parse("99885923-9D80-B5C1-7C04-A02A23304E15"), Title = "A Clockwork Orange" },
        new () { Id = Guid.Parse("97215079-8C5D-30F4-A3E5-456485C14D6F"), Title = "Good Will Hunting" },
        new () { Id = Guid.Parse("A880934C-7A86-79A9-D1E2-D305B7D34D90"), Title = "Up" },
        new () { Id = Guid.Parse("9B6C7F6F-1F6D-439A-849D-64C130D849A8"), Title = "The Sting" },
        new () { Id = Guid.Parse("BC3D5C94-697F-8799-90C1-A86693114773"), Title = "Gran Torino" },
        new () { Id = Guid.Parse("8E99568C-360C-3E5A-A890-E6D1C9D14E8F"), Title = "Batman Begins" },
        new () { Id = Guid.Parse("A890E6D1-C9D1-4E8F-184B-8E1D4A2B4E9A"), Title = "Monsters, Inc." },
        new () { Id = Guid.Parse("80B972E5-6B64-1B66-D77E-1E8B6E744C5D"), Title = "Blade Runner 2049" },
        new () { Id = Guid.Parse("9F848E53-1A85-E655-7B3E-817730C41C4B"), Title = "Heat" },
        new () { Id = Guid.Parse("F130E267-D284-0DB7-4A89-732D924C1897"), Title = "L.A. Confidential" },
        new () { Id = Guid.Parse("C5389650-F795-4932-2B8E-54720B9552B3"), Title = "Kill Bill: Vol. 1" },
        new () { Id = Guid.Parse("C7585F81-2926-7924-E850-4E8099E7B0E3"), Title = "The Thing" },
        new () { Id = Guid.Parse("B6E32115-6620-C716-1051-407490E94672"), Title = "Raging Bull" },
        new ()
        {
            Id = Guid.Parse("0F07E639-981D-5E9D-A310-47F197883E9E"), Title = "Lock, Stock and Two Smoking Barrels"
        },
        new () { Id = Guid.Parse("D67587E2-11E0-C665-42A9-4A9992F6B603"), Title = "Trainspotting" },
        new () { Id = Guid.Parse("E7E6237D-BD8E-2C6A-60B0-4A3CB7D44903"), Title = "Rush" },
        new () { Id = Guid.Parse("C83C0F5E-7E73-2E96-8575-4775A8F886C7"), Title = "Hacksaw Ridge" },
        new () { Id = Guid.Parse("23B2D70F-F56C-87C1-D271-41F1A89E70C9"), Title = "Shutter Island" },
        new () { Id = Guid.Parse("9D80B5C1-7C04-A02A-2330-4E1599885923"), Title = "Finding Nemo" },
        new () { Id = Guid.Parse("8C5D30F4-A3E5-4564-85C1-4D6F97215079"), Title = "Mad Max: Fury Road" },
        new () { Id = Guid.Parse("7A8679A9-D1E2-D305-B7D3-4D90A880934C"), Title = "Gone Girl" },
        new () { Id = Guid.Parse("1F6D439A-849D-64C1-30D8-49A89B6C7F6F"), Title = "V for Vendetta" },
        new () { Id = Guid.Parse("697F8799-90C1-A866-9311-4773BC3D5C94"), Title = "Logan" },
        new () { Id = Guid.Parse("360C3E5A-A890-E6D1-C9D1-4E8F8E99568C"), Title = "The Grand Budapest Hotel" },
        new () { Id = Guid.Parse("E6D1C9D1-4E8F-184B-8E1D-4A2B4E9AA890"), Title = "Spotlight" },
        new () { Id = Guid.Parse("6B641B66-D77E-1E8B-6E74-4C5D80B972E5"), Title = "1917" },
        new () { Id = Guid.Parse("1A85E655-7B3E-8177-30C4-1C4B9F848E53"), Title = "Dune: Part Two" },
        new () { Id = Guid.Parse("D2840DB7-4A89-732D-924C-1897F130E267"), Title = "Oppenheimer" },
        new () { Id = Guid.Parse("F7954932-2B8E-5472-0B95-52B3C5389650"), Title = "Everything Everywhere All at Once" },
        new () { Id = Guid.Parse("29267924-E850-4E80-99E7-B0E3C7585F81"), Title = "Top Gun: Maverick" },
        new () { Id = Guid.Parse("6620C716-1051-4074-90E9-4672B6E32115"), Title = "Spiderman: No Way Home" },
        new () { Id = Guid.Parse("981D5E9D-A310-47F1-9788-3E9E0F07E639"), Title = "Arrival" },
        new () { Id = Guid.Parse("11E0C665-42A9-4A99-92F6-B603D67587E2"), Title = "The Revenant" },
        new () { Id = Guid.Parse("BD8E2C6A-60B0-4A3C-B7D4-4903E7E6237D"), Title = "Birdman" },
        new () { Id = Guid.Parse("7E732E96-8575-4775-A8F8-86C7C83C0F5E"), Title = "12 Years a Slave" },
        new () { Id = Guid.Parse("F56C87C1-D271-41F1-A89E-70C923B2D70F"), Title = "The Wolf of Wall Street" },
        new () { Id = Guid.Parse("7C04A02A-2330-4E15-9988-59239D80B5C1"), Title = "Her" },
        new () { Id = Guid.Parse("A3E54564-85C1-4D6F-9721-50798C5D30F4"), Title = "The Social Network" },
        new () { Id = Guid.Parse("D1E2D305-B7D3-4D90-A880-934C7A8679A9"), Title = "No Country for Old Men" },
        new () { Id = Guid.Parse("849D64C1-30D8-49A8-9B6C-7F6F1F6D439A"), Title = "There Will Be Blood" },
        new () { Id = Guid.Parse("90C1A866-9311-4773-BC3D-5C94697F8799"), Title = "Pan's Labyrinth" },
        new () { Id = Guid.Parse("A890E6D1-C9D1-4E8F-8E99-568C360C3E5A"), Title = "Casino Royale" },
        new () { Id = Guid.Parse("184B8E1D-4A2B-4E9A-A890-E6D1C9D14E8F"), Title = "Million Dollar Baby" },
        new () { Id = Guid.Parse("D77E1E8B-6E74-4C5D-80B9-72E56B641B66"), Title = "Big Fish" },
        new () { Id = Guid.Parse("7B3E8177-30C4-1C4B-9F84-8E531A85E655"), Title = "Catch Me If You Can" },
        new () { Id = Guid.Parse("4A89732D-924C-1897-F130-E267D2840DB7"), Title = "The Truman Show" },
        new () { Id = Guid.Parse("A5074D02-536D-4573-A365-E5C8D4096054"), Title = "The Sixth Sense" },
        new ()
        {
            Id = Guid.Parse("67807984-F802-4A73-A7D9-BD4B316D997A"),
            Title = "John Wick",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("67807984-F802-4A73-A7D9-BD4B316D997A"),
                    UserName = "ActionLover", Rating = 5, Comment = "Incredible action!"
                },
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("67807984-F802-4A73-A7D9-BD4B316D997A"),
                    UserName = "KeanuFan", Rating = 4, Comment = "Breathtaking."
                }
            }
        },
        new ()
        {
            Id = Guid.Parse("F6470C58-0D12-4C1C-9E2F-A6B53106B222"),
            Title = "Blade Runner",
            Ratings =
            {
                new MovieRating
                {
                    Id = Guid.NewGuid(), MovieId = Guid.Parse("F6470C58-0D12-4C1C-9E2F-A6B53106B222"),
                    UserName = "Cyberpunk82", Rating = 5, Comment = "A masterpiece of sci-fi."
                }
            }
        }
    ];
}
