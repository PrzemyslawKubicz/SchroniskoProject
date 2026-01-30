using Microsoft.AspNetCore.Http; // <--- Potrzebne do HttpContext (symulacja zapytania HTTP)
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schronisko.Api.Controllers;
using Schronisko.Api.Data;
using Schronisko.Shared.Entities;
using System.Security.Claims; // <--- Potrzebne do ClaimsPrincipal (symulacja tożsamości)

namespace Schronisko.Tests
{
    // Klasa zawierająca testy jednostkowe. 
    // Każda metoda oznaczona [Fact] to jeden scenariusz testowy.
    public class SchroniskoTests
    {
        // ====================================================================
        // 1. MECHANIZM FAŁSZYWEJ BAZY DANYCH (In-Memory)
        // ====================================================================
        // Ta metoda tworzy "jednorazową" bazę danych w pamięci RAM.
        // Dzięki temu każdy test startuje z czystą kartą i nie wpływa na inne testy.
        private DataContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                // Guid.NewGuid() zapewnia unikalną nazwę bazy dla każdego wywołania.
                // To kluczowe, żeby testy uruchamiane równolegle nie wchodziły sobie w paradę.
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var databaseContext = new DataContext(options);
            databaseContext.Database.EnsureCreated(); // Upewnij się, że struktura tabel istnieje
            return databaseContext;
        }

        // ====================================================================
        // 2. SYMULACJA ZALOGOWANEGO UŻYTKOWNIKA (Mocking User)
        // ====================================================================
        // Twój kontroler w metodzie Add/Delete używa linii: var userId = User.FindFirst(...).
        // W środowisku testowym "User" jest domyślnie nullem.
        // Ta metoda ręcznie tworzy "fałszywy dowód tożsamości" (ClaimsPrincipal) i wkłada go do kontrolera.
        private void SimulateAdminUser(AnimalsController controller)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "AdminTestowy"),
                new Claim(ClaimTypes.NameIdentifier, "1"), // Udajemy, że to User o ID = 1
                new Claim(ClaimTypes.Role, "Admin")        // Udajemy, że ma rolę Admin
            }, "mock"));

            // Podmieniamy kontekst kontrolera na nasz fałszywy, zawierający tego użytkownika.
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        // ====================================================================
        // TEST 1: CZY GET ZWRACA LISTĘ?
        // ====================================================================
        [Fact]
        public async Task GetAllAnimals_ReturnsAnimalsList()
        {
            // ARRANGE (Przygotowanie)
            // Tworzymy bazę i wrzucamy do niej 2 zwierzaki
            var dbContext = GetDatabaseContext();
            dbContext.Animals.Add(new Animal { Name = "TestDog", Species = "Pies", Status = "Do adopcji" });
            dbContext.Animals.Add(new Animal { Name = "TestCat", Species = "Kot", Status = "Do adopcji" });
            await dbContext.SaveChangesAsync();

            var controller = new AnimalsController(dbContext);

            // ACT (Wykonanie akcji)
            // Uruchamiamy prawdziwą metodę kontrolera
            var result = await controller.GetAllAnimals();

            // ASSERT (Sprawdzenie wyników)
            // Sprawdzamy, czy wynik to ActionResult zawierający Listę Zwierząt
            var actionResult = Assert.IsType<ActionResult<List<Animal>>>(result);
            var model = Assert.IsType<List<Animal>>(actionResult.Value);

            // Czy baza zwróciła dokładnie 2 rekordy?
            Assert.Equal(2, model.Count);
        }

        // ====================================================================
        // TEST 2: CZY POST DODAJE REKORD I LOGUJE OPERACJĘ?
        // ====================================================================
        [Fact]
        public async Task AddAnimal_AddsAnimalToDatabase()
        {
            // Arrange
            var dbContext = GetDatabaseContext();
            var controller = new AnimalsController(dbContext);

            // WAŻNE: Wstrzykujemy Admina, bo metoda AddAnimal ma atrybut [Authorize] 
            // i w środku sprawdza tożsamość do logów. Bez tego test by "wybuchł" (NullReferenceException).
            SimulateAdminUser(controller);

            var newAnimal = new Animal { Name = "Nowy", Species = "Pies", Status = "Do adopcji" };

            // Act
            await controller.AddAnimal(newAnimal);

            // Assert
            // 1. Sprawdzamy czy pies fizycznie trafił do bazy
            var animalInDb = await dbContext.Animals.FirstOrDefaultAsync(a => a.Name == "Nowy");
            Assert.NotNull(animalInDb);

            // 2. Sprawdzamy SIDE-EFFECT (Efekt uboczny):
            // Czy system poprawnie odnotował to zdarzenie w tabeli Logs?
            var logInDb = await dbContext.Logs.FirstOrDefaultAsync();
            Assert.NotNull(logInDb); // Powinien powstać log
            Assert.Equal("AdminTestowy", logInDb.UserEmail); // Sprawdzamy czy log zawiera email naszego symulowanego admina
        }

        // ====================================================================
        // TEST 3: CZY DELETE USUWA REKORD?
        // ====================================================================
        [Fact]
        public async Task DeleteAnimal_RemovesAnimalFromDatabase()
        {
            // Arrange
            var dbContext = GetDatabaseContext();
            var animal = new Animal { Name = "DoUsuniecia", Species = "Chomik", Status = "Do adopcji" };
            dbContext.Animals.Add(animal);
            await dbContext.SaveChangesAsync(); // Zapisujemy, żeby zwierzak dostał ID

            var controller = new AnimalsController(dbContext);
            SimulateAdminUser(controller); // Delete też wymaga logowania akcji

            // Act
            await controller.DeleteAnimal(animal.Id);

            // Assert
            // Próbujemy znaleźć tego zwierzaka. Powinien zwrócić null.
            var deletedAnimal = await dbContext.Animals.FindAsync(animal.Id);
            Assert.Null(deletedAnimal);
        }

        // ====================================================================
        // TEST 4: POBIERANIE PO ID (SCENARIUSZ POZYTYWNY)
        // ====================================================================
        [Fact]
        public async Task GetAnimal_ReturnsAnimal_WhenIdExists()
        {
            // Arrange
            var dbContext = GetDatabaseContext();
            var animal = new Animal { Name = "Szukany", Species = "Kot", Status = "Do adopcji" };
            dbContext.Animals.Add(animal);
            await dbContext.SaveChangesAsync();

            var controller = new AnimalsController(dbContext);

            // Act
            var result = await controller.GetAnimal(animal.Id);

            // Assert
            // Sprawdzamy zagnieżdżone typy zwracane przez API:
            // ActionResult -> OkObjectResult (kod 200) -> Animal
            var actionResult = Assert.IsType<ActionResult<Animal>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedAnimal = Assert.IsType<Animal>(okResult.Value);

            Assert.Equal("Szukany", returnedAnimal.Name);
        }

        // ====================================================================
        // TEST 5: POBIERANIE PO ID (SCENARIUSZ NEGATYWNY - 404)
        // ====================================================================
        [Fact]
        public async Task GetAnimal_ReturnsNotFound_WhenIdDoesNotExist()
        {
            // Arrange
            var dbContext = GetDatabaseContext();
            var controller = new AnimalsController(dbContext);

            // Act
            // Pytamy o ID 999, którego nie ma w pustej bazie
            var result = await controller.GetAnimal(999);

            // Assert
            var actionResult = Assert.IsType<ActionResult<Animal>>(result);
            // Oczekujemy, że kontroler zwróci NotFound ("Nie znaleziono zwierzaka :(")
            Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        }
    }
}