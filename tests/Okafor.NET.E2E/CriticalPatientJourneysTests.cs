using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Okafor_.NET.E2E;

[Collection(E2eCollection.Name)]
[Trait("Category", "E2E")]
public sealed class CriticalPatientJourneysTests
{
    private readonly E2eFixture _fixture;

    public CriticalPatientJourneysTests(E2eFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task MobilePatient_CanFindAndBookARealAvailableAppointment()
    {
        await _fixture.ResetDatabaseAsync();
        var appointment = await _fixture.SeedAppointmentScenarioAsync();
        const string patientEmail = "ada.patient@example.test";

        await _fixture.RunBrowserScenarioAsync(nameof(MobilePatient_CanFindAndBookARealAvailableAppointment), async page =>
        {
            await page.GotoAsync("/AppointmentRequests/Create");
            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Find a Time That Works" })).ToBeVisibleAsync();

            await page.GetByLabel("Department").SelectOptionAsync(new SelectOptionValue { Label = appointment.DepartmentName });
            await Expect(page.GetByLabel("Doctor")).ToBeEnabledAsync();
            await page.GetByLabel("Doctor").SelectOptionAsync(new SelectOptionValue { Label = $"{appointment.DoctorName} — {appointment.DepartmentName}" });
            await page.GetByRole(AriaRole.Button, new() { Name = "Next: Choose Time" }).ClickAsync();

            await page.GetByLabel("Date", new() { Exact = true }).FillAsync(appointment.Date.ToString("yyyy-MM-dd"));
            await page.Locator("[data-slots-grid]").GetByRole(AriaRole.Button, new() { Name = appointment.Time }).ClickAsync();

            await page.GetByLabel("Full Name", new() { Exact = true }).FillAsync("Ada E2E Patient");
            await page.GetByLabel("Phone", new() { Exact = true }).FillAsync("+2348000000001");
            await page.GetByLabel("Email", new() { Exact = true }).FillAsync(patientEmail);
            await page.GetByLabel("Reason for Visit", new() { Exact = true }).FillAsync("Routine automated browser-test appointment.");
            await page.Locator("[data-form-confirmed]").CheckAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Confirm Booking" }).ClickAsync();

            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Booking Confirmed" })).ToBeVisibleAsync();
            await Expect(page.Locator("[data-result-email]")).ToHaveTextAsync(patientEmail);
            await Expect(page.Locator("[data-result-reference]")).ToHaveTextAsync(new System.Text.RegularExpressions.Regex("^\\d{8}$"));
        });

        await _fixture.AssertAppointmentWasPersistedAsync(patientEmail, appointment);
    }

    [Fact]
    public async Task MobileVisitor_CanUseNavigationAndScopedSearch()
    {
        await _fixture.ResetDatabaseAsync();
        var appointment = await _fixture.SeedAppointmentScenarioAsync();

        await _fixture.RunBrowserScenarioAsync(nameof(MobileVisitor_CanUseNavigationAndScopedSearch), async page =>
        {
            await page.GotoAsync("/");
            await page.GetByRole(AriaRole.Button, new() { Name = "Toggle navigation" }).ClickAsync();
            await page.GetByRole(AriaRole.Link, new() { Name = "Search hospital information" }).ClickAsync();

            await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "What can we help you find?" })).ToBeVisibleAsync();
            await page.GetByLabel("Search term").FillAsync("Ada Browser-Test");
            await page.GetByLabel("Search within").SelectOptionAsync("doctors");
            await page.GetByRole(AriaRole.Button, new() { Name = "Search", Exact = true }).ClickAsync();

            await Expect(page.GetByText(appointment.DoctorName, new() { Exact = true })).ToBeVisibleAsync();
            await Expect(page.GetByText("No results", new() { Exact = false })).ToHaveCountAsync(0);
        });
    }

    [Fact]
    public async Task MobileVisitor_CanOpenFatherToochukwuProfileAndStartPreselectedTeleconsultation()
    {
        await _fixture.ResetDatabaseAsync();
        var provider = await _fixture.SeedFatherToochukwuAsync();

        await _fixture.RunBrowserScenarioAsync(
            nameof(MobileVisitor_CanOpenFatherToochukwuProfileAndStartPreselectedTeleconsultation),
            async page =>
            {
                await page.GotoAsync($"/doctors/{provider.Slug}");
                await Expect(page.GetByRole(AriaRole.Heading, new() { Name = provider.FullName })).ToBeVisibleAsync();
                await Expect(page.GetByAltText($"Photo of {provider.FullName}")).ToBeVisibleAsync();
                await page.GetByRole(AriaRole.Link, new() { Name = "Request Psychotherapy Teleconsultation" }).ClickAsync();

                await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Book a Teleconsultation" })).ToBeVisibleAsync();
                await Expect(page.GetByLabel("Department / Specialty")).ToHaveValueAsync(provider.DepartmentId.ToString());
                await Expect(page.GetByLabel("Preferred Doctor", new() { Exact = false })).ToHaveValueAsync(provider.DoctorId.ToString());
            });
    }
}
