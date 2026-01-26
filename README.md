# DOKUMENTACJA PROJEKTOWA
## System Zarządzania Schroniskiem dla Zwierząt

**Przedmiot:** Bazy Danych w Aplikacjach Internetowych 
**Data:** 25.01.2026  

---

## 1. Opis Systemu

### 1.1. Cel Projektu i Rozwiązywany Problem
Celem projektu było stworzenie nowoczesnego systemu informatycznego typu SPA (Single Page Application) wspierającego codzienną działalność schroniska dla zwierząt. Tradycyjne metody zarządzania oparte na dokumentacji papierowej generują problemy z przepływem informacji, utrudniają aktualizację statusów zwierząt w czasie rzeczywistym oraz wydłużają proces adopcyjny.

**Aplikacja rozwiązuje te problemy poprzez:**
* **Centralizację danych:** Elektroniczna kartoteka zwierząt dostępna dla pracowników i administratorów w jednym miejscu.
* **Transparentność:** Publiczny dostęp do katalogu podopiecznych dla osób zainteresowanych adopcją.
* **Automatyzację:** Elektroniczny obieg wniosków adopcyjnych ze zmianą statusów.
* **Audytowalność:** Rejestrowanie kluczowych akcji w systemie (logi systemowe), co zwiększa bezpieczeństwo danych.

### 1.2. Użytkownicy i Funkcjonalność
System implementuje model kontroli dostępu oparty na rolach (Role-Based Access Control):

| Rola | Opis Uprawnień |
| :--- | :--- |
| **Użytkownik (Klient)** | Przeglądanie katalogu zwierząt, wyszukiwanie i filtrowanie, składanie wniosków adopcyjnych, podgląd statusu własnych wniosków. |
| **Pracownik (Personel)** | Zarządzanie kartoteką zwierząt (dodawanie, edycja), przeglądanie wszystkich wniosków adopcyjnych, podejmowanie decyzji (Zatwierdzanie/Odrzucanie). |
| **Administrator (Szef)** | Pełny dostęp do systemu, dostęp do Panelu Zarządzania (Dashboard) ze statystykami, usuwanie rekordów z bazy danych, wgląd w techniczne logi systemowe. |

---

## 2. Architektura i Technologie

Projekt zrealizowano w nowoczesnej architekturze rozproszonej **Klient-Serwer**.

### 2.1. Stack Technologiczny
* **Backend:** ASP.NET Core Web API (.NET 10).
* **Frontend:** Blazor WebAssembly (WASM).
* **Baza Danych:** Microsoft SQL Server.
* **ORM:** Entity Framework Core (podejście Code First).
* **Uwierzytelnianie:** JWT (JSON Web Token) + `AuthenticationStateProvider`.
* **Testy:** xUnit.

### 2.2. Struktura Rozwiązania
Rozwiązanie zostało podzielone na trzy projekty zgodnie z zasadą *Separation of Concerns*:
* `Schronisko.Api`: Warstwa serwerowa (Kontrolery REST, Migracje, Logika biznesowa, Kontekst Bazy Danych).
* `Schronisko.Client`: Warstwa prezentacji (Komponenty Razor, UI, komunikacja HTTP z API).
* `Schronisko.Shared`: Biblioteka współdzielonych modeli (DTO, Encje), zapewniająca spójność typów danych między backendem a frontendem.

---

## 3. Struktura Bazy Danych

System opiera się na relacyjnej bazie danych SQL Server składającej się z 4 głównych tabel powiązanych relacjami (Klucze Obce).

### 3.1. Schemat ERD
<img width="522" height="644" alt="erd" src="https://github.com/user-attachments/assets/0d1269ed-7615-4e12-bf5e-0aab91831e7d" />



### 3.2. Obiekty Programowalne SQL (Zaawansowane)
W celu optymalizacji i zapewnienia integralności danych, kluczowa logika została przeniesiona do warstwy bazy danych:

1.  **Wyzwalacz (Trigger) `trg_ApproveAdoption`:**
    * **Typ:** `AFTER UPDATE`.
    * **Działanie:** Monitoruje tabelę `AdoptionRequests`. Gdy status wniosku zmieni się na *"Zatwierdzony"*, trigger automatycznie aktualizuje status powiązanego zwierzęcia w tabeli `Animals` na *"Zaadoptowany"*. Działa to jako bezpiecznik spójności danych.

2.  **Procedura Składowana `sp_GetShelterStatistics`:**
    * **Działanie:** Agreguje dane dla Panelu Administratora. W jednym zapytaniu zwraca liczbę dostępnych zwierząt, szczęśliwych adopcji oraz oczekujących wniosków.

3.  **Funkcja Skalarna `fn_DaysInShelter`:**
    * **Działanie:** Oblicza dynamicznie liczbę dni pobytu zwierzęcia w schronisku na podstawie daty przyjęcia i daty bieżącej.

---

## 4. Prezentacja Aplikacji

### 4.1. Strona Główna (Katalog)
Widok dostępny dla każdego użytkownika. Umożliwia filtrowanie i wyszukiwanie.
<img width="1600" height="781" alt="home" src="https://github.com/user-attachments/assets/13402845-87e5-45cd-9542-3f7625153e20" />


### 4.2. Panel Zarządzania (Dashboard)
Widok statystyk i zarządzania wnioskami (dla personelu).
<img width="1557" height="762" alt="dashboard" src="https://github.com/user-attachments/assets/6a06b392-7b23-4732-93a5-9918bf3ca2d0" />


### 4.3. Edycja Zwierzęcia
Formularz zarządzania danymi z walidacją.
<img width="1600" height="783" alt="edit" src="https://github.com/user-attachments/assets/54233f78-663c-4638-bf40-e12e819e7cff" />


### 4.4. Logi Systemowe
Widok audytu dostępny tylko dla Administratora.
<img width="1600" height="782" alt="logs" src="https://github.com/user-attachments/assets/15192902-c516-4f76-8829-08c0222ead16" />

---

## 5. Testy Jednostkowe

W celu zapewnienia wysokiej jakości kodu oraz stabilności logiki biznesowej, zaimplementowano zestaw testów jednostkowych weryfikujących działanie warstwy backendowej (API).

### 5.1. Technologie i Środowisko
* **Framework:** xUnit.
* **Baza danych:** `Microsoft.EntityFrameworkCore.InMemory` – zastosowano bazę danych w pamięci RAM, co pozwala na szybkie wykonywanie testów w izolacji, bez wpływu na produkcyjne dane.
* **Symulacja Tożsamości:** Zaimplementowano mechanizm mockowania `ClaimsPrincipal`, aby weryfikować endpointy chronione atrybutem `[Authorize]` oraz poprawność zapisu logów audytowych (przypisywanie ID użytkownika do akcji).

### 5.2. Scenariusze Testowe
Zrealizowano 5 kluczowych scenariuszy w klasie `ScroniskoTests`:
1.  **`GetAllAnimals`**: Poprawność pobierania listy rekordów.
2.  **`AddAnimal`**: Weryfikacja dodawania rekordu oraz **automatycznego tworzenia logu systemowego**.
3.  **`DeleteAnimal`**: Weryfikacja usuwania rekordu przez Administratora.
4.  **`GetAnimal_ById`**: Poprawność pobierania szczegółów istniejącego rekordu.
5.  **`GetAnimal_NotFound`**: Weryfikacja obsługi błędów (kod 404) dla zapytań o nieistniejące ID.

### 5.3. Wyniki
Wszystkie testy zakończyły się wynikiem pozytywnym.
<img width="792" height="399" alt="testy" src="https://github.com/user-attachments/assets/9e701b32-193c-4d04-97ec-4c627fb7602a" />


---

## 6. Instrukcja Uruchomienia

### Wymagania wstępne
* .NET SDK 10.0.
* SQL Server (LocalDB lub pełna instancja).

### Krok 1: Konfiguracja Bazy Danych
W folderze `Schronisko.Api` wykonaj polecenie, aby utworzyć strukturę bazy, tabele, procedury i triggery:
```bash
dotnet ef database update
```

### Krok 2: Uruchomienie
Należy uruchomić oba projekty równolegle.

### Backend:
```bash
cd Schronisko.Api
dotnet run
```

### Frontend
```bash
cd Schronisko.Client
dotnet run
```

### Krok 3: Dane Logowania (Seed Data)
| Rola | Login (User) | Hasło | Opis Uprawnień |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin` | `admin123` | Pełny dostęp, podgląd logów, usuwanie rekordów |
| **Pracownik** | `pracownik` | `pracownik123` | Edycja zwierząt, zatwierdzanie wniosków |
| **Użytkownik** | `user` | `user123` | Przeglądanie zwierząt, składanie wniosków adopcyjnych |
