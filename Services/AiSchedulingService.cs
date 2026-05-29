using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Okafor_.NET.Services;

public sealed partial class AiSchedulingService : IAiSchedulingService
{
    private const string SystemPrompt = """
        You parse WhatsApp appointment requests for Boniface and Paulina Okafor Memorial Hospital.
        Return strict JSON only. No markdown.
        Schema:
        {
          "AppointmentType": "General|Pediatrics|Maternity|Diagnostics|Surgery|Teleconsultation|Unknown",
          "PreferredDate": "YYYY-MM-DD or null",
          "PreferredTimeWindow": "Morning|Afternoon|Evening|Any or null",
          "MissingFields": ["AppointmentType","PreferredDate","PreferredTimeWindow"]
        }
        Use today's date supplied in the user message to resolve relative dates such as tomorrow.
        If you cannot confidently infer a field, set it to null or Unknown and include it in MissingFields.
        """;

    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiSchedulingService> _logger;

    public AiSchedulingService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<AiSchedulingService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<SchedulingIntent> ParseAppointmentRequestAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        var endpoint = _configuration["SchedulingAi:Endpoint"];
        var apiKey = _configuration["SchedulingAi:ApiKey"];
        var model = _configuration["SchedulingAi:Model"] ?? "gpt-4o-mini";

        if (!IntegrationConfiguration.HasRealValue(endpoint) ||
            !IntegrationConfiguration.HasRealValue(apiKey))
        {
            return ParseWithRules(message);
        }

        try
        {
            var payload = new
            {
                model,
                temperature = 0,
                response_format = new { type = "json_object" },
                messages = new[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = $"Today is {DateTime.Today:yyyy-MM-dd}. Message: {message}" }
                }
            };

            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Scheduling AI parse failed with HTTP {StatusCode}. Falling back to local rules.", (int)response.StatusCode);
                return ParseWithRules(message);
            }

            var content = TryReadAssistantContent(body);
            return ParseIntentJson(content) ?? ParseWithRules(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Scheduling AI parse failed. Falling back to local rules.");
            return ParseWithRules(message);
        }
    }

    private static string? TryReadAssistantContent(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.TryGetProperty("choices", out var choices) &&
                choices.ValueKind == JsonValueKind.Array)
            {
                var first = choices.EnumerateArray().FirstOrDefault();
                if (first.ValueKind == JsonValueKind.Object &&
                    first.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content))
                {
                    return content.GetString();
                }
            }

            if (root.TryGetProperty("output_text", out var outputText))
            {
                return outputText.GetString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static SchedulingIntent? ParseIntentJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var appointmentType = GetString(root, "AppointmentType");
            var preferredTimeWindow = GetString(root, "PreferredTimeWindow");
            var preferredDateRaw = GetString(root, "PreferredDate");
            var missing = ReadMissingFields(root);

            var intent = new SchedulingIntent
            {
                AppointmentType = NormalizeAppointmentType(appointmentType),
                PreferredTimeWindow = NormalizeTimeWindow(preferredTimeWindow),
                MissingFields = missing
            };

            if (DateTime.TryParse(preferredDateRaw, out var preferredDate))
            {
                intent.PreferredDate = preferredDate.Date;
            }

            AddMissingFields(intent);
            return intent;
        }
        catch
        {
            return null;
        }
    }

    private static SchedulingIntent ParseWithRules(string message)
    {
        var normalized = message.ToLowerInvariant();
        var intent = new SchedulingIntent
        {
            AppointmentType = InferAppointmentType(normalized),
            PreferredDate = InferDate(normalized),
            PreferredTimeWindow = InferTimeWindow(normalized)
        };

        AddMissingFields(intent);
        return intent;
    }

    private static string InferAppointmentType(string normalized)
    {
        if (normalized.Contains("child") || normalized.Contains("children") || normalized.Contains("baby") ||
            normalized.Contains("pediatric") || normalized.Contains("paediatric"))
        {
            return "Pediatrics";
        }

        if (normalized.Contains("pregnan") || normalized.Contains("antenatal") || normalized.Contains("maternity"))
            return "Maternity";

        if (normalized.Contains("lab") || normalized.Contains("test") || normalized.Contains("scan") || normalized.Contains("diagnostic"))
            return "Diagnostics";

        if (normalized.Contains("surgery") || normalized.Contains("surgical"))
            return "Surgery";

        if (normalized.Contains("phone") || normalized.Contains("video") || normalized.Contains("teleconsult"))
            return "Teleconsultation";

        if (normalized.Contains("doctor") || normalized.Contains("appointment") || normalized.Contains("clinic") || normalized.Contains("see"))
            return "General";

        return string.Empty;
    }

    private static DateTime? InferDate(string normalized)
    {
        if (normalized.Contains("tomorrow"))
            return DateTime.Today.AddDays(1);

        if (normalized.Contains("today"))
            return DateTime.Today;

        var match = DateRegex().Match(normalized);
        if (match.Success && DateTime.TryParse(match.Value, out var parsed))
            return parsed.Date;

        return null;
    }

    private static string InferTimeWindow(string normalized)
    {
        if (normalized.Contains("morning"))
            return "Morning";

        if (normalized.Contains("afternoon"))
            return "Afternoon";

        if (normalized.Contains("evening"))
            return "Evening";

        if (normalized.Contains("any time") || normalized.Contains("anytime") || normalized.Contains("any"))
            return "Any";

        return string.Empty;
    }

    private static void AddMissingFields(SchedulingIntent intent)
    {
        intent.MissingFields.Clear();

        if (string.IsNullOrWhiteSpace(intent.AppointmentType) ||
            intent.AppointmentType.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
        {
            intent.MissingFields.Add("AppointmentType");
        }

        if (!intent.PreferredDate.HasValue)
            intent.MissingFields.Add("PreferredDate");

        if (string.IsNullOrWhiteSpace(intent.PreferredTimeWindow))
            intent.MissingFields.Add("PreferredTimeWindow");
    }

    private static string NormalizeAppointmentType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        return value.Trim();
    }

    private static string NormalizeTimeWindow(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        return value.Trim();
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static List<string> ReadMissingFields(JsonElement root)
    {
        if (!root.TryGetProperty("MissingFields", out var missingFields) ||
            missingFields.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return missingFields
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    [GeneratedRegex(@"\b\d{4}-\d{2}-\d{2}\b|\b\d{1,2}/\d{1,2}/\d{2,4}\b")]
    private static partial Regex DateRegex();
}
