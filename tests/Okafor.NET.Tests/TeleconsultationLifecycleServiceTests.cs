using Okafor_.NET.Models;
using Okafor_.NET.Services;

namespace Okafor_.NET.Tests;

public sealed class TeleconsultationLifecycleServiceTests
{
    private readonly TeleconsultationLifecycleService _service = new();

    [Fact]
    public void ValidateAdminUpdate_RequiresNextStepsBeforeConfirmation()
    {
        var request = CreateRequest(TeleconsultationStatus.Pending);
        var update = new TeleconsultationUpdateInput(
            TeleconsultationStatus.Confirmed,
            DateTime.Today.AddDays(1),
            "10:00",
            null,
            null);

        var errors = _service.ValidateAdminUpdate(request, update);

        Assert.Contains(errors, e =>
            e.FieldName == nameof(TeleconsultationUpdateInput.AdminNotes) &&
            e.Message.Contains("meeting link", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateAdminUpdate_RequiresSaferNotesBeforeRejection()
    {
        var request = CreateRequest(TeleconsultationStatus.Pending);
        var update = new TeleconsultationUpdateInput(
            TeleconsultationStatus.Rejected,
            DateTime.Today.AddDays(1),
            "10:00",
            null,
            null);

        var errors = _service.ValidateAdminUpdate(request, update);

        Assert.Contains(errors, e =>
            e.FieldName == nameof(TeleconsultationUpdateInput.AdminNotes) &&
            e.Message.Contains("safer next-step", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(TeleconsultationStatus.Completed)]
    [InlineData(TeleconsultationStatus.Rejected)]
    [InlineData(TeleconsultationStatus.Cancelled)]
    public void ValidateAdminUpdate_DoesNotReopenTerminalTeleconsultations(TeleconsultationStatus terminalStatus)
    {
        var request = CreateRequest(terminalStatus);
        var update = new TeleconsultationUpdateInput(
            TeleconsultationStatus.Confirmed,
            DateTime.Today.AddDays(1),
            "10:00",
            "https://example.test/consult",
            null);

        var errors = _service.ValidateAdminUpdate(request, update);

        Assert.Contains(errors, e =>
            e.FieldName == nameof(TeleconsultationUpdateInput.Status) &&
            e.Message.Contains("cannot be reopened", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ApplyAdminUpdate_NormalizesTextAndSetsUpdatedAtWhenChanged()
    {
        var request = CreateRequest(TeleconsultationStatus.Pending);
        var update = new TeleconsultationUpdateInput(
            TeleconsultationStatus.Confirmed,
            DateTime.Today.AddDays(2),
            " 14:30 ",
            " https://example.test/consult ",
            " Join five minutes early. ");

        var changed = _service.ApplyAdminUpdate(request, update);

        Assert.True(changed);
        Assert.Equal(TeleconsultationStatus.Confirmed, request.Status);
        Assert.Equal(DateTime.Today.AddDays(2), request.PreferredDate);
        Assert.Equal("14:30", request.PreferredTime);
        Assert.Equal("https://example.test/consult", request.MeetingLink);
        Assert.Equal("Join five minutes early.", request.AdminNotes);
        Assert.NotNull(request.UpdatedAt);
    }

    private static TeleconsultationRequest CreateRequest(TeleconsultationStatus status) => new()
    {
        PatientName = "Test Patient",
        Email = "patient@example.com",
        Phone = "+2348012345678",
        DepartmentId = 1,
        ConsultationType = TeleconsultationType.Video,
        PreferredDate = DateTime.Today.AddDays(1),
        PreferredTime = "10:00",
        Reason = "Follow-up care",
        ConsentAccepted = true,
        Status = status,
        CreatedAt = DateTime.UtcNow
    };
}
