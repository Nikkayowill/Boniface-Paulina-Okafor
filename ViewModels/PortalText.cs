namespace Okafor_.NET.ViewModels;

/// <summary>
/// One place for "print this, or say plainly that it is missing".
///
/// Several records store an absent email or telephone number as an empty string
/// rather than as null — an appointment request submitted with only a phone
/// number is the common case. A view written as
/// <c>@(request.Email ?? request.Phone)</c> therefore printed nothing at all
/// for those records, leaving the separator beside it dangling against a blank.
/// </summary>
public static class PortalText
{
    /// <summary>The value, or the fallback when it is null, empty or blank.</summary>
    public static string Or(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    /// <summary>The first value that is actually written, or the fallback.</summary>
    public static string FirstOr(string? first, string? second, string fallback) =>
        string.IsNullOrWhiteSpace(first) ? Or(second, fallback) : first;
}
