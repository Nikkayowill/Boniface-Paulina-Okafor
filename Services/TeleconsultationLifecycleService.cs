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

        if (request.Status is TeleconsultationStatus.Completed or TeleconsultationStatus.Rejected &&
            update.Status != request.Status)
        {
            errors.Add(new TeleconsultationValidationError(
                nameof(update.Status),
                "Completed or rejected teleconsultations cannot be reopened from this screen."));
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
        var changed =
            request.Status != update.Status ||
            request.PreferredDate.Date != update.PreferredDate.Date ||
            !string.Equals(request.PreferredTime, update.PreferredTime, StringComparison.Ordinal) ||
            !string.Equals(request.MeetingLink, update.MeetingLink, StringComparison.Ordinal) ||
            !string.Equals(request.AdminNotes, update.AdminNotes, StringComparison.Ordinal);

        request.Status = update.Status;
        request.PreferredDate = update.PreferredDate.Date;
        request.PreferredTime = update.PreferredTime.Trim();
        request.MeetingLink = string.IsNullOrWhiteSpace(update.MeetingLink) ? null : update.MeetingLink.Trim();
        request.AdminNotes = string.IsNullOrWhiteSpace(update.AdminNotes) ? null : update.AdminNotes.Trim();

        if (changed)
            request.UpdatedAt = DateTime.UtcNow;

        return changed;
    }
}
