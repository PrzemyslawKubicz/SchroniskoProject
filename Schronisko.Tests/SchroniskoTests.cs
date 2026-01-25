using Microsoft.AspNetCore.Http; // <--- Potrzebne do HttpContext
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schronisko.Api.Controllers;
using Schronisko.Api.Data;
using Schronisko.Shared.Entities;
using System.Security.Claims; // <--- Potrzebne do ClaimsPrincipal

namespace Schronisko.Tests
{
    public class SchroniskoTests
    {
        // 1. Pomocnicza baza danych (InMemory)
        private DataContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var databaseContext = new DataContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
        }

        // 2. Metoda udająca zalogowanego Admina
        // Bez tego metody Add/Delete wyrzucą błąd, bo próbują czytać User.Identity
        private void SimulateAdminUser(AnimalsController controller)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "AdminTestowy"),
                new Claim(ClaimTypes.NameIdentifier, "1"), // ID admina
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }

        [Fact] // Test nr 1: GET (Dla każdego)
        public async Task GetAllAnimals_ReturnsAnimalsList()
        {
            // Arrange
            var dbContext = GetDatabaseContext();
            dbContext.Animals.Add(new Animal { Name = "TestDog", Species = "Pies", Status = "Do adopcji" });
            dbContext.Animals.Add(new Animal { Name = "TestCat", Species = "Kot", Status = "Do adopcji" });
            await dbContext.SaveChangesAsync();

            var controller = new AnimalsController(dbContext);

            // Act
            var result = await controller.GetAllAnimals();

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<Animal>>>(result);
            var model = Assert.IsType<List<Animal>>(actionResult.Value);
            Assert.Equal(2, model.Count);
        }

        [Fact] // Test nr 2: POST (Wymaga Admina)
        public async Task AddAnimal_AddsAnimalToDatabase()
        {
            // Arrange
            var dbContext = GetDatabaseContext();
            var controller = new AnimalsController(dbContext);

            // Wstrzykujemy Admina, żeby logowanie zadziałało
            SimulateAdminUser(controller);

            var newAnimal = new Animal { Name = "Nowy", Species = "Pies", Status = "Do adopcji" };

            // Act
            await controller.AddAnimal(newAnimal);

            // Assert
            var animalInDb = await dbContext.Animals.FirstOrDefaultAsync(a => a.Name == "Nowy");
            Assert.NotNull(animalInDb);

            // Sprawdzamy czy dodał się LOG!
            var logInDb = await dbContext.Logs.FirstOrDefaultAsync();
            Assert.NotNull(logInDb); // Powinien powstać log
            Assert.Equal("AdminTestowy", logInDb.UserEmail);
        }

        [Fact] // Test nr 3: DELETE (Wymaga Admina)
        public async Task DeleteAnimal_RemovesAnimalFromDatabase()
        {
            // Arrange
            var dbContext = GetDatabaseContext();
            var animal = new Animal { Name = "DoUsuniecia", Species = "Chomik", Status = "Do adopcji" };
            dbContext.Animals.Add(animal);
            await dbContext.SaveChangesAsync();

            var controller = new AnimalsController(dbContext);

            // Wstrzykujemy Admina
            SimulateAdminUser(controller);

            // Act
            await controller.DeleteAnimal(animal.Id);

            // Assert
            var deletedAnimal = await dbContext.Animals.FindAsync(animal.Id);
            Assert.Null(deletedAnimal);
        }

        [Fact] // Test nr 4: GET ID (Dla każdego)
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
            var actionResult = Assert.IsType<ActionResult<Animal>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedAnimal = Assert.IsType<Animal>(okResult.Value);

            Assert.Equal("Szukany", returnedAnimal.Name);
        }

        [Fact] // Test nr 5: GET ID (Nie znaleziono)
        public async Task GetAnimal_ReturnsNotFound_WhenIdDoesNotExist()
        {
            // Arrange
            var dbContext = GetDatabaseContext();
            var controller = new AnimalsController(dbContext);

            // Act
            var result = await controller.GetAnimal(999);

            // Assert
            var actionResult = Assert.IsType<ActionResult<Animal>>(result);
            Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        }
    }
}