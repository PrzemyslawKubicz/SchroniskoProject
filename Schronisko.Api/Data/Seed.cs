using Schronisko.Shared.Entities;

namespace Schronisko.Api.Data
{
    // Klasa "Seed" służy do wstępnego wypełnienia bazy danych.
    // Jest uruchamiana przy starcie aplikacji (w Program.cs), aby na świeżej bazie 
    // administrator miał się jak zalogować, a strona nie świeciła pustkami.
    public class Seed
    {
        // Metoda statyczna przyjmująca kontekst bazy danych, żeby móc wykonywać operacje INSERT.
        public static void SeedData(DataContext context)
        {
            // =============================================================
            // 1. SEEDOWANIE UŻYTKOWNIKÓW
            // =============================================================

            // ZABEZPIECZENIE PRZED DUPLIKATAMI:
            // Sprawdzamy, czy w tabeli Users cokolwiek już jest.
            // Jeśli (!Any()) zwraca true (tabela pusta), to wchodzimy do środka.
            // Jeśli dane już są, pomijamy ten krok (dzięki temu nie dodajemy admina 100 razy).
            if (!context.Users.Any())
            {
                var users = new List<User>
                {
                    // Konto Administratora (pełne uprawnienia)
                    new User
                    {
                        Username = "admin",
                        Email = "admin@schronisko.pl",
                        Role = "Admin", 
                        // WAŻNE: Hashowanie hasła!
                        // Nigdy nie zapisujemy hasła otwartym tekstem. 
                        // BCrypt tworzy bezpieczny ciąg znaków, którego nie da się łatwo odwrócić.
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123")
                    },
                    // Konto Pracownika (edycja zwierząt, obsługa wniosków)
                    new User
                    {
                        Username = "pracownik",
                        Email = "pracownik@schronisko.pl",
                        Role = "Employee",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("pracownik123")

                    },
                    // Konto Zwykłego Użytkownika (składanie wniosków)
                    new User
                    {
                        Username = "user",
                        Email = "user@schronisko.pl",
                        Role = "User",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123")
                    }
                };

                // Dodajemy listę do lokalnego kontekstu (pamięci RAM)...
                context.Users.AddRange(users);

                // ...i wysyłamy komendę INSERT do bazy danych (zatwierdzenie zmian).
                context.SaveChanges();
            }

            // =============================================================
            // 2. SEEDOWANIE ZWIERZĄT
            // =============================================================

            // Ponownie sprawdzamy, czy tabela Animals jest pusta.
            if (!context.Animals.Any())
            {
                var animals = new List<Animal>
                {
                    new Animal {
                        Name = "Reksio", Species = "Pies", Age = 2,
                        Description = "Bardzo energiczny i wesoły kundelek. Uwielbia spacery.",
                        Status = "Do adopcji",
                        ImageUrl = "images/reksio.jpg"
                        // Uwaga: Pole DateAdded przyjmie tutaj wartość domyślną (np. 0001-01-01),
                        // chyba że w konstruktorze klasy Animal ustawiłeś = DateTime.Now.
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

                // AddRange jest szybsze niż dodawanie w pętli pojedynczo (Add)
                context.Animals.AddRange(animals);

                // Zapisujemy zwierzęta w bazie.
                context.SaveChanges();
            }
        }
    }
}