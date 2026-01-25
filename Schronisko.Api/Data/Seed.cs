using Schronisko.Shared.Entities;

namespace Schronisko.Api.Data
{
    public class Seed
    {
        public static void SeedData(DataContext context)
        {
            // 1. Seedowanie Użytkowników
            if (!context.Users.Any())
            {
                var users = new List<User>
                {
                    new User
                    {
                        Username = "admin",
                        Email = "admin@schronisko.pl",
                        Role = "Admin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123")
                    },
                    new User
                    {
                        Username = "pracownik",
                        Email = "pracownik@schronisko.pl",
                        Role = "Employee",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("pracownik123")
                        
                    },
                    new User
                    {
                        Username = "user",
                        Email = "user@schronisko.pl",
                        Role = "User",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123")
                    }
                };
                context.Users.AddRange(users);
                context.SaveChanges();
            }

            // 2. Seedowanie Zwierząt
            if (!context.Animals.Any())
            {
                var animals = new List<Animal>
                {
                    new Animal {
                        Name = "Reksio", Species = "Pies", Age = 2,
                        Description = "Bardzo energiczny i wesoły kundelek. Uwielbia spacery.",
                        Status = "Do adopcji",
                        ImageUrl = "images/reksio.jpg"
                    },
                    new Animal {
                        Name = "Mruczek", Species = "Kot", Age = 4,
                        Description = "Spokojny kanapowiec, lubi głaskanie i spanie na słońcu.",
                        Status = "Do adopcji",
                        ImageUrl = "images/mruczek.jpg"
                    },
                    new Animal {
                        Name = "Azor", Species = "Pies", Age = 8,
                        Description = "Starszy pan szukający spokojnego domu. Idealny do domu z ogrodem.",
                        Status = "Do adopcji",
                        ImageUrl = "images/azor.jpg"
                    },
                    new Animal {
                        Name = "Luna", Species = "Pies", Age = 1,
                        Description = "Szczeniak, wymaga nauki czystości, ale jest przekochana.",
                        Status = "Do adopcji",
                        ImageUrl = "images/luna.jpg"
                    },
                    new Animal {
                        Name = "Filemon", Species = "Kot", Age = 0,
                        Description = "Mały psotnik. Wszędzie go pełno.",
                        Status = "Do adopcji",
                        ImageUrl = "images/filemon.jpg"
                    },
                    new Animal {
                        Name = "Bazyli", Species = "Inny", Age = 0,
                        Description = "Mały, ale wariat",
                        Status = "Do adopcji",
                        ImageUrl = "images/bazyli.jpg"
                    }
                };
                context.Animals.AddRange(animals);
                context.SaveChanges();
            }
        }
    }
}