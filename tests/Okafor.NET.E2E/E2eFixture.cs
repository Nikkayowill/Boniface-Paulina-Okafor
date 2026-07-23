using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Playwright;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using Okafor_.NET.Seed;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;

namespace Okafor_.NET.E2E;

public sealed class E2eFixture : IAsyncLifetime
{
    private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";
    private readonly MsSqlContainer _database = new MsSqlBuilder(SqlServerImage)
        .WithCleanUp(true)
        .Build();
    private readonly SemaphoreSlim _scenarioLock = new(1, 1);

    private SqlConnection? _databaseConnection;
    private Respawner? _respawner;
    private E2eWebApplicationFactory? _application;
    private HttpClient? _serverClient;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public Uri BaseAddress { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        await using (var context = CreateDbContext())
        {
            await context.Database.MigrateAsync();
        }

        _databaseConnection = new SqlConnection(_database.GetConnectionString());
        await _databaseConnection.OpenAsync();
        _respawner = await Respawner.CreateAsync(_databaseConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            SchemasToInclude = ["dbo"],
            TablesToIgnore = [new Table("dbo", "__EFMigrationsHistory")]
        });

        var privateStorage = Path.Combine(Path.GetTempPath(), $"okafor-e2e-{Guid.NewGuid():N}");
        _application = new E2eWebApplicationFactory(_database.GetConnectionString(), privateStorage);
        _application.UseKestrel(0);
        _serverClient = _application.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var server = _application.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        var publishedAddress = addresses?.SingleOrDefault()
            ?? throw new InvalidOperationException("The E2E Kestrel server did not publish exactly one address.");
        BaseAddress = new Uri(publishedAddress);
        _serverClient.BaseAddress = BaseAddress;

        using var readinessResponse = await _serverClient.GetAsync("/health/ready");
        readinessResponse.EnsureSuccessStatusCode();

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !string.Equals(Environment.GetEnvironmentVariable("HEADED"), "1", StringComparison.Ordinal)
        });
    }

    public async Task ResetDatabaseAsync()
    {
        if (_respawner is null || _databaseConnection is null)
        {
            throw new InvalidOperationException("The E2E database has not been initialized.");
        }

        await _scenarioLock.WaitAsync();
        try
        {
            await _respawner.ResetAsync(_databaseConnection);
        }
        finally
        {
            _scenarioLock.Release();
        }
    }

    public async Task<AppointmentScenario> SeedAppointmentScenarioAsync()
    {
        var appointmentDate = DateTime.Today.AddDays(7);
        await using var context = CreateDbContext();
        var department = new Department
        {
            Name = "Family Medicine",
            Description = "Fictional department used only by automated E2E tests."
        };
        var doctor = new Doctor
        {
            FullName = "Dr. Ada Browser-Test",
            Slug = "dr-ada-browser-test",
            Specialty = "Family Medicine",
            Bio = "Fictional clinician used only by automated E2E tests.",
            Qualifications = "E2E Test Qualification",
            Department = department
        };
        context.DoctorAvailabilities.Add(new DoctorAvailability
        {
            Doctor = doctor,
            DayOfWeek = appointmentDate.DayOfWeek,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            SlotDurationMinutes = 30,
            IsActive = true
        });
        await context.SaveChangesAsync();

        return new AppointmentScenario(department.Name, doctor.FullName, appointmentDate, "09:00");
    }

    public async Task<ProviderScenario> SeedFatherToochukwuAsync()
    {
        await using var context = CreateDbContext();
        await ClinicalDataSeed.SeedAsync(context);
        var provider = await context.Doctors
            .AsNoTracking()
            .Include(doctor => doctor.Department)
            .SingleAsync(doctor => doctor.Slug == "rev-fr-dr-toochukwu-bartholomew-okafor");

        return new ProviderScenario(
            provider.Id,
            provider.DepartmentId,
            provider.FullName,
            provider.Department!.Name,
            provider.Slug!);
    }

    public async Task<ProviderScenario> SeedPublicWebsiteContentAsync()
    {
        await using var context = CreateDbContext();
        await ClinicalDataSeed.SeedAsync(context);

        context.Posts.Add(new Post
        {
            Title = "A short guide to preparing for your hospital visit",
            Slug = "prepare-for-your-hospital-visit",
            Summary = "A concise automated test article used to verify the public website across mobile routes.",
            Content = """
                Bring the medicines you currently take and any referral information you have.

                ## Before you leave home

                Write down the questions you want to ask the care team.
                """,
            Published = true,
            IsFeatured = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var provider = await context.Doctors
            .AsNoTracking()
            .Include(doctor => doctor.Department)
            .SingleAsync(doctor => doctor.Slug == "rev-fr-dr-toochukwu-bartholomew-okafor");

        return new ProviderScenario(
            provider.Id,
            provider.DepartmentId,
            provider.FullName,
            provider.Department!.Name,
            provider.Slug!);
    }

    public async Task AssertAppointmentWasPersistedAsync(string email, AppointmentScenario scenario)
    {
        await using var context = CreateDbContext();
        var request = await context.AppointmentRequests
            .AsNoTracking()
            .Include(item => item.Doctor)
            .SingleAsync(item => item.Email == email);
        var slot = await context.AppointmentSlots
            .AsNoTracking()
            .SingleAsync(item => item.AppointmentRequestId == request.Id);

        request.Status.Should().Be(AppointmentStatus.Pending);
        request.Doctor!.FullName.Should().Be(scenario.DoctorName);
        request.PreferredDate.Should().Be(scenario.Date.Date);
        request.PreferredTime.Should().Be(scenario.Time);
        slot.IsBooked.Should().BeTrue();
        slot.SlotDateTime.Should().Be(scenario.Date.Date.AddHours(9));
    }

    public async Task RunBrowserScenarioAsync(
        string scenarioName,
        Func<IPage, Task> scenario,
        ViewportSize? viewport = null)
    {
        if (_browser is null)
        {
            throw new InvalidOperationException("The E2E browser has not been initialized.");
        }

        await using var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseAddress.ToString(),
            Locale = "en-NG",
            TimezoneId = "Africa/Lagos",
            ViewportSize = viewport ?? new ViewportSize { Width = 390, Height = 844 }
        });
        await context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });

        var page = await context.NewPageAsync();
        var pageErrors = new List<string>();
        var serverErrors = new List<string>();
        page.PageError += (_, error) => pageErrors.Add(error);
        page.Response += (_, response) =>
        {
            if (response.Url.StartsWith(BaseAddress.ToString(), StringComparison.OrdinalIgnoreCase) &&
                response.Status >= 500)
            {
                serverErrors.Add($"{response.Status} {response.Url}");
            }
        };

        try
        {
            await scenario(page);
            pageErrors.Should().BeEmpty("the page should not raise uncaught JavaScript errors");
            serverErrors.Should().BeEmpty("the critical journey should not receive server errors");
            await context.Tracing.StopAsync();
        }
        catch
        {
            var artifactDirectory = GetArtifactDirectory();
            Directory.CreateDirectory(artifactDirectory);
            var safeName = Regex.Replace(scenarioName, "[^a-zA-Z0-9_-]+", "-").Trim('-').ToLowerInvariant();
            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(artifactDirectory, $"{safeName}.png"),
                FullPage = true
            });
            await context.Tracing.StopAsync(new TracingStopOptions
            {
                Path = Path.Combine(artifactDirectory, $"{safeName}-trace.zip")
            });
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();
        _serverClient?.Dispose();
        if (_application is not null)
        {
            await _application.DisposeAsync();
        }

        if (_databaseConnection is not null)
        {
            await _databaseConnection.DisposeAsync();
        }

        _scenarioLock.Dispose();
        await _database.DisposeAsync();
    }

    private ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_database.GetConnectionString())
            .EnableDetailedErrors()
            .Options;
        return new ApplicationDbContext(options);
    }

    private static string GetArtifactDirectory()
    {
        var configured = Environment.GetEnvironmentVariable("E2E_ARTIFACTS_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Okafor-.NET.sln")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(directory?.FullName ?? AppContext.BaseDirectory, "artifacts", "e2e");
    }

    private sealed class E2eWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;
        private readonly string _privateStorage;

        public E2eWebApplicationFactory(string connectionString, string privateStorage)
        {
            _connectionString = connectionString;
            _privateStorage = privateStorage;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("E2E");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString,
                    ["AllowedHosts"] = "*",
                    ["Authentication:RequireConfirmedAccount"] = "false",
                    ["Payments:Provider"] = "Mock",
                    ["Payments:Mock:ReferencePrefix"] = "E2E",
                    ["Notifications:Provider"] = "Lean",
                    ["Notifications:WhatsAppNumber"] = "+2348000000000",
                    ["BackgroundTasks:AppointmentRemindersEnabled"] = "false",
                    ["BackgroundTasks:PushSubscriptionCleanupEnabled"] = "false",
                    ["PatientDocuments:StorageRoot"] = Path.Combine(_privateStorage, "patient-documents"),
                    ["DataProtection:KeysPath"] = Path.Combine(_privateStorage, "data-protection-keys")
                });
            });
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(_connectionString, sqlOptions =>
                        sqlOptions.EnableRetryOnFailure()));
            });
        }
    }
}

public sealed record AppointmentScenario(string DepartmentName, string DoctorName, DateTime Date, string Time);

public sealed record ProviderScenario(
    int DoctorId,
    int DepartmentId,
    string FullName,
    string DepartmentName,
    string Slug);
