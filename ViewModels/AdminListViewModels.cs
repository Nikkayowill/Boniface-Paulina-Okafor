using Okafor_.NET.Models;

namespace Okafor_.NET.ViewModels;

/// <summary>
/// A single page of admin register results, plus the paging facts a list view
/// needs to render a pager and report an accurate total, while the controller
/// only ever fetches one page of rows from the database.
///
/// Shared across every paginated admin Index action (Fix 2 of the query-quality
/// cleanup pass) — check here before inventing another paging shape.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

/// <summary>
/// The two facts the shared <c>_Pager</c> partial (Areas/Admin/Views/Shared)
/// needs to render prev/next links. Kept separate from <see cref="PagedResult{T}"/>
/// so the partial's @model isn't generic — a view builds one of these from its
/// own PagedResult&lt;T&gt; to pass in.
/// </summary>
public class PagerViewModel
{
    public int Page { get; init; }
    public int TotalPages { get; init; }
}

/// <summary>Projection for AppointmentRequestsController.Index — only the fields the register row prints.</summary>
public class AppointmentRequestListItemViewModel
{
    public int Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime PreferredDate { get; set; }
    public string PreferredTime { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? DoctorName { get; set; }
    public AppointmentStatus Status { get; set; }
    public bool ContactConfirmed { get; set; }
    public string? ContactMethod { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Projection for PatientAppointmentsController.Index.</summary>
public class PatientAppointmentListItemViewModel
{
    public int Id { get; set; }
    public int PatientProfileId { get; set; }
    public string? PatientName { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string? DepartmentName { get; set; }
    public string? DoctorName { get; set; }
    public string? Notes { get; set; }
    public PatientAppointmentStatus Status { get; set; }
}

/// <summary>Projection for PatientMessagesController.Index.</summary>
public class PatientMessageListItemViewModel
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
    public string? PatientName { get; set; }
    public string? PatientEmail { get; set; }
}

/// <summary>Projection for TeleconsultationsController.Index.</summary>
public class TeleconsultationListItemViewModel
{
    public int Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime PreferredDate { get; set; }
    public string PreferredTime { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public TeleconsultationType ConsultationType { get; set; }
    public TeleconsultationStatus Status { get; set; }
    public string? MeetingLink { get; set; }
}
