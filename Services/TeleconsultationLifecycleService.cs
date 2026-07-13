using Okafor_.NET.Models;

namespace Okafor_.NET.Services;

public sealed record TeleconsultationUpdateInput(
    TeleconsultationStatus Status,
    DateTime PreferredDate,
    string PreferredTime,
    string? MeetingLink,
    string? AdminNotes);

public sealed record TeleconsultationValidationError(string FieldName, string Message);

public interface ITeleconsultationLifecycleService
{
    IReadOnlyList<TeleconsultationValidationError> ValidateAdminUpdate(
        TeleconsultationRequest request,
        TeleconsultationUpdateInput update);

    bool ApplyAdminUpdate(TeleconsultationRequest request, TeleconsultationUpdateInput update);
}

public sealed class TeleconsultationLifecycleService : ITeleconsultationLifecycleService
{
    public IReadOnlyList<TeleconsultationValidationError> ValidateAdminUpdate(
        TeleconsultationRequest request,
        TeleconsultationUpdateInput update)
    {
        var errors = new List<TeleconsultationValidationError>();

        if (update.PreferredDate.Date < DateTime.Today)
        {
            errors.Add(new TeleconsultationValidationError(
                nameof(update.PreferredDate),
                "Teleconsultation date cannot be in the past."));
        }

        if (string.IsNullOrWhiteSpace(update.PreferredTime))
        {
            errors.Add(new TeleconsultationValidationError(
                nameof(update.PreferredTime),
                "Teleconsultation time is required."));
        }

        if (!string.IsNullOrWhiteSpace(update.MeetingLink) &&
            !IsSafeMeetingLink(update.MeetingLink))
        {
            errors.Add(new TeleconsultationValidationError(
                nameof(update.MeetingLink),
                "Meeting link must be a valid http or https URL."));
        }

        if (request.Status is TeleconsultationStatus.Completed or TeleconsultationStatus.Rejected or TeleconsultationStatus.Cancelled &&
            update.Status != request.Status)
        {
            errors.Add(new TeleconsultationValidationError(
                nameof(update.Status),
                "Completed, rejected, or cancelled teleconsultations cannot be reopened from this screen."));
        }

        if (update.Status is TeleconsultationStatus.Confirmed or TeleconsultationStatus.Rescheduled &&
            string.IsNullOrWhiteSpace(update.MeetingLink) &&
            string.IsNullOrWhiteSpace(update.AdminNotes))
        {
            errors.Add(new TeleconsultationValidationError(
                nameof(update.AdminNotes),
                "Add a meeting link or clear next-step notes before confirming or rescheduling."));
        }

        if (update.Status == TeleconsultationStatus.Rejected &&
            string.IsNullOrWhiteSpace(update.AdminNotes))
        {
            errors.Add(new TeleconsultationValidationError(
                nameof(update.AdminNotes),
                "Add safer next-step notes before rejecting a teleconsultation."));
        }

        return errors;
    }

    public bool ApplyAdminUpdate(TeleconsultationRequest request, TeleconsultationUpdateInput update)
    {
        var preferredTime = update.PreferredTime.Trim();
        var meetingLink = string.IsNullOrWhiteSpace(update.MeetingLink) ? null : update.MeetingLink.Trim();
        var adminNotes = string.IsNullOrWhiteSpace(update.AdminNotes) ? null : update.AdminNotes.Trim();

        var changed =
            request.Status != update.Status ||
            request.PreferredDate.Date != update.PreferredDate.Date ||
            !string.Equals(request.PreferredTime, preferredTime, StringComparison.Ordinal) ||
            !string.Equals(request.MeetingLink, meetingLink, StringComparison.Ordinal) ||
            !string.Equals(request.AdminNotes, adminNotes, StringComparison.Ordinal);

        request.Status = update.Status;
        request.PreferredDate = update.PreferredDate.Date;
        request.PreferredTime = preferredTime;
        request.MeetingLink = meetingLink;
        request.AdminNotes = adminNotes;

        if (changed)
            request.UpdatedAt = DateTime.UtcNow;

        return changed;
    }

    private static bool IsSafeMeetingLink(string meetingLink)
    {
        if (!Uri.TryCreate(meetingLink.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        return uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp;
    }
}
