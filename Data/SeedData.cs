using FlockForge.Data.Local;
using FlockForge.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlockForge.Data;

/// <summary>
/// Provides seed data for the FlockForge application
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Seeds the database with default data
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="logger">Logger instance</param>
    public static async Task SeedAsync(FlockForgeDbContext context, ILogger logger)
    {
        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Check if data already exists
            if (await context.Farmers.AnyAsync())
            {
                logger.LogInformation("Database already contains data, skipping seed");
                return;
            }

            logger.LogInformation("Seeding database with default data");

            // Create sample farmer
            var sampleFarmer = new Farmer
            {
                FirebaseUid = "sample-farmer-uid",
                FirstName = "John",
                Surname = "Smith",
                Email = "john.smith@example.com",
                MobileNumber = "+27821234567",
                Address = "123 Farm Road, Rural Area, Western Cape",
                PreferredLanguage = "en",
                TimeZone = "Africa/Johannesburg",
                IsProfileComplete = true,
                IsEmailVerified = true,
                IsMobileVerified = true
            };

            context.Farmers.Add(sampleFarmer);
            await context.SaveChangesAsync();

            // Create sample farms with different breeds and production systems
            var farms = new List<Farm>
            {
                new Farm
                {
                    FarmerId = sampleFarmer.Id,
                    FarmName = "Springbok Farm",
                    CompanyName = "Smith Farming Enterprises",
                    Breed = "Dorper",
                    TotalProductionEwes = 500,
                    Size = 1200.5m,
                    GPSLocation = "-33.9249,18.4241", // Cape Town area
                    ProductionSystem = "Extensive",
                    Address = "Farm 123, Springbok Road, Western Cape",
                    Province = "Western Cape",
                    Country = "South Africa",
                    VeterinarianName = "Dr. Sarah Johnson",
                    VeterinarianPhone = "+27821111111",
                    AgentName = "Cape Livestock Agents",
                    AgentPhone = "+27822222222",
                    DefaultGestationDays = 150,
                    TargetWeaningAge = 90,
                    IsActive = true
                },
                new Farm
                {
                    FarmerId = sampleFarmer.Id,
                    FarmName = "Merino Valley",
                    Breed = "South African Merino",
                    TotalProductionEwes = 800,
                    Size = 2000.0m,
                    GPSLocation = "-25.7479,28.2293", // Pretoria area
                    ProductionSystem = "Semi-intensive",
                    Address = "Farm 456, Merino Valley, Gauteng",
                    Province = "Gauteng",
                    Country = "South Africa",
                    VeterinarianName = "Dr. Michael Brown",
                    VeterinarianPhone = "+27823333333",
                    DefaultGestationDays = 148,
                    TargetWeaningAge = 100,
                    IsActive = true
                },
                new Farm
                {
                    FarmerId = sampleFarmer.Id,
                    FarmName = "Karoo Sheep Station",
                    Breed = "Damara",
                    TotalProductionEwes = 300,
                    Size = 5000.0m,
                    GPSLocation = "-32.2968,22.4569", // Karoo area
                    ProductionSystem = "Extensive",
                    Address = "Farm 789, Karoo Plains, Northern Cape",
                    Province = "Northern Cape",
                    Country = "South Africa",
                    DefaultGestationDays = 152,
                    TargetWeaningAge = 85,
                    IsActive = true
                }
            };

            context.Farms.AddRange(farms);
            await context.SaveChangesAsync();

            // Create sample lambing seasons for the first farm
            var springbokFarm = farms[0];
            var lambingSeasons = new List<LambingSeason>
            {
                new LambingSeason
                {
                    FarmId = springbokFarm.Id,
                    Code = "2024-SPRING",
                    GroupName = "Spring Lambing 2024",
                    MatingStart = new DateOnly(2024, 3, 1),
                    MatingEnd = new DateOnly(2024, 3, 31),
                    LambingStart = new DateOnly(2024, 7, 29), // 150 days later
                    LambingEnd = new DateOnly(2024, 8, 29),
                    TargetEwes = 400,
                    ExpectedConceptionRate = 85m,
                    ExpectedLambingPercentage = 120m,
                    GestationDays = 150,
                    Active = false,
                    Status = LambingSeasonStatus.Completed
                },
                new LambingSeason
                {
                    FarmId = springbokFarm.Id,
                    Code = "2024-AUTUMN",
                    GroupName = "Autumn Lambing 2024",
                    MatingStart = new DateOnly(2024, 9, 1),
                    MatingEnd = new DateOnly(2024, 9, 30),
                    LambingStart = new DateOnly(2025, 1, 29),
                    LambingEnd = new DateOnly(2025, 2, 28),
                    TargetEwes = 450,
                    ExpectedConceptionRate = 88m,
                    ExpectedLambingPercentage = 125m,
                    GestationDays = 150,
                    Active = true,
                    Status = LambingSeasonStatus.Lambing
                }
            };

            context.LambingSeasons.AddRange(lambingSeasons);
            await context.SaveChangesAsync();

            // Create sample production records for the completed season
            var completedSeason = lambingSeasons[0];

            // Breeding record
            var breedingRecord = new BreedingRecord
            {
                LambingSeasonId = completedSeason.Id,
                MatingType = MatingType.NaturalMating,
                EwesMated = 400,
                NaturalMatingStart = completedSeason.MatingStart,
                NaturalMatingEnd = completedSeason.MatingEnd,
                RamId = "RAM001",
                RamBreed = "Dorper",
                Cost = 5000m,
                Notes = "Used 8 rams for natural mating. Good weather conditions."
            };

            context.BreedingRecords.Add(breedingRecord);

            // Scanning record
            var scanningRecord = new ScanningRecord
            {
                LambingSeasonId = completedSeason.Id,
                ScanDate = new DateOnly(2024, 5, 15),
                EwesScanned = 400,
                EwesPregnant = 340,
                EwesSingles = 200,
                EwesTwins = 130,
                EwesMultiples = 10,
                EwesEmpty = 60,
                Method = ScanningMethod.Ultrasound,
                ScannerName = "Western Cape Scanning Services",
                Cost = 2000m,
                Notes = "Good conception rate achieved. Twins higher than expected."
            };

            context.ScanningRecords.Add(scanningRecord);

            // Lambing record
            var lambingRecord = new LambingRecord
            {
                LambingSeasonId = completedSeason.Id,
                LambingDate = new DateOnly(2024, 8, 15),
                EwesLambed = 335,
                TotalLambsBorn = 475,
                LambsBornAlive = 460,
                LambsBornDead = 15,
                MaleLambs = 235,
                FemaleLambs = 225,
                EwesSingles = 195,
                EwesTwins = 125,
                EwesMultiples = 15,
                EwesAssisted = 25,
                EwesMortality = 5,
                AverageBirthWeight = 4.2m,
                WeatherConditions = "Mild, dry conditions",
                Notes = "Excellent lambing season with minimal losses."
            };

            context.LambingRecords.Add(lambingRecord);

            // Weaning record
            var weaningRecord = new WeaningRecord
            {
                LambingSeasonId = completedSeason.Id,
                WeaningDate = new DateOnly(2024, 11, 15),
                LambsWeaned = 445,
                MaleLambsWeaned = 225,
                FemaleLambsWeaned = 220,
                TotalWeaningWeight = 13350m, // 30kg average
                AverageWeaningWeight = 30.0m,
                AverageWeaningAge = 92,
                LambMortality = 15,
                EweMortality = 2,
                LambsSold = 300,
                LambsRetained = 145,
                SalesIncome = 450000m, // R1500 per lamb
                AveragePricePerKg = 50m,
                WeaningCost = 8000m,
                FeedCost = 25000m,
                HealthCost = 3000m,
                EweBodyConditionScore = 3.2m,
                Notes = "Excellent weaning weights achieved. Strong market prices."
            };

            context.WeaningRecords.Add(weaningRecord);

            await context.SaveChangesAsync();

            logger.LogInformation("Database seeded successfully with sample data");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    /// <summary>
    /// Gets the list of common sheep breeds in South Africa
    /// </summary>
    public static List<string> GetCommonBreeds()
    {
        return new List<string>
        {
            "Dorper",
            "South African Merino",
            "Damara",
            "Blackhead Persian",
            "Karakul",
            "Afrino",
            "Dohne Merino",
            "South African Mutton Merino",
            "Van Rooy",
            "Ronderib Afrikaner",
            "Pedi",
            "Nguni",
            "Boer Goat", // Sometimes mixed with sheep operations
            "Ile de France",
            "Suffolk",
            "Texel"
        };
    }

    /// <summary>
    /// Gets the list of production systems
    /// </summary>
    public static List<string> GetProductionSystems()
    {
        return new List<string>
        {
            "Extensive",
            "Semi-intensive", 
            "Intensive",
            "Feedlot",
            "Pasture-based",
            "Mixed farming"
        };
    }

    /// <summary>
    /// Gets typical gestation periods by breed (in days)
    /// </summary>
    public static Dictionary<string, int> GetGestationPeriodsByBreed()
    {
        return new Dictionary<string, int>
        {
            { "Dorper", 150 },
            { "South African Merino", 148 },
            { "Damara", 152 },
            { "Blackhead Persian", 150 },
            { "Karakul", 150 },
            { "Afrino", 148 },
            { "Dohne Merino", 148 },
            { "South African Mutton Merino", 148 },
            { "Van Rooy", 150 },
            { "Ronderib Afrikaner", 150 },
            { "Pedi", 150 },
            { "Nguni", 150 },
            { "Ile de France", 148 },
            { "Suffolk", 147 },
            { "Texel", 147 }
        };
    }
}