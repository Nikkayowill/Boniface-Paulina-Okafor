namespace Okafor_.NET.Services;

/// <summary>
/// A single published impact figure, sourced from the Nigeria Family Helper Program's
/// reporting rather than from live hospital data. Figures carry the reporting date so
/// the site never presents stale numbers as current.
/// </summary>
public sealed record ImpactFigure(string Value, string Label, string Detail);

/// <summary>
/// Facts about the Nigeria Family Helper Program that the hospital's public pages quote:
/// the charity that funds the hospital, where donations are taken, and the impact figures
/// published in the program's own reporting.
/// </summary>
public sealed class ProgramInfoService
{
    private readonly IConfiguration _configuration;

    public ProgramInfoService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Name => Value("Program:Name", "Nigeria Family Helper Program");

    public string ShortName => Value("Program:ShortName", "NFHP");

    public string Website => Value("Program:Website", "https://www.nigeriafamilyhelperprogram.org");

    public string CharityRegistration => Value("Program:CharityRegistration", "88889 4268 RR0001");

    /// <summary>
    /// External donation page. Donations are processed by CanadaHelps on the program's
    /// behalf so Canadian donors receive a tax receipt; the hospital takes no card details.
    /// </summary>
    public string DonateUrl => Value("Program:DonateUrl", "https://www.canadahelps.org/en/dn/139012");

    /// <summary>
    /// Reporting date for every figure below. Shown wherever figures appear.
    /// </summary>
    public string ImpactAsOf => Value("Program:ImpactAsOf", "May 2026");

    /// <summary>
    /// Headline figures for impact bands. Ordered most-persuasive first.
    /// </summary>
    public IReadOnlyList<ImpactFigure> HeadlineImpact =>
    [
        new(Number("Program:Impact:PatientsTreatedAnnually", 2292),
            "Patients treated each year",
            "Seen at the hospital in Isuochi."),
        new(Number("Program:Impact:OutreachPatients", 1800),
            "Cared for at outreach clinics",
            "Reached in their own communities."),
        new(Number("Program:Impact:ChildrenSupported", 88),
            "Children in school",
            "School fees and materials covered."),
        new(Number("Program:Impact:FamiliesRegistered", 63),
            "Families registered for support",
            "Receiving food, shelter or care help.")
    ];

    public string HospitalBeds => Number("Program:Impact:HospitalBeds", 27);

    public string NursingTrainees => Number("Program:Impact:NursingTrainees", 8);

    public string HomesBuilt => Number("Program:Impact:HomesBuilt", 3);

    private string Value(string key, string fallback)
    {
        var configured = _configuration[key];
        return string.IsNullOrWhiteSpace(configured) ? fallback : configured.Trim();
    }

    /// <summary>
    /// Formats a figure with thousands separators, falling back to the published value
    /// when the configured entry is missing or not a number.
    /// </summary>
    private string Number(string key, int fallback)
    {
        var configured = _configuration[key];
        return int.TryParse(configured, out var parsed)
            ? parsed.ToString("N0")
            : fallback.ToString("N0");
    }
}
