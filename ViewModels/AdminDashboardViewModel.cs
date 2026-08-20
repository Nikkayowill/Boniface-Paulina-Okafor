namespace Okafor_.NET.ViewModels;

public class AdminDashboardViewModel
{
    public int DoctorsCount { get; set; }
    public int DepartmentsCount { get; set; }
    public int AppointmentsCount { get; set; }
    public int PostsCount { get; set; }
    public int ContactSubmissionsCount { get; set; }
    public int UnreadPatientMessagesCount { get; set; }

    public int PendingAppointmentsCount { get; set; }
    public int ApprovedAppointmentsCount { get; set; }
    public int RejectedAppointmentsCount { get; set; }

    public int PendingTeleconsultationsCount { get; set; }
    public int ConfirmedTeleconsultationsCount { get; set; }
    public int RescheduledTeleconsultationsCount { get; set; }

    public int PendingBillPaymentsCount { get; set; }
    public int PaidBillPaymentsCount { get; set; }
    public decimal TotalPaidRevenue { get; set; }

    /// <summary>
    /// The request that has been waiting longest for a member of staff, across
    /// every queue where a person is waiting on a reply. Null when nothing is
    /// outstanding.
    /// </summary>
    public AdminOutstandingItemViewModel? LongestWaiting { get; set; }

    public List<AdminDashboardActivityViewModel> RecentActivity { get; set; } = new();
}

/// <summary>
/// One outstanding request, named the way a member of staff would name it:
/// who is waiting, what they asked for, and since when.
/// </summary>
public class AdminOutstandingItemViewModel
{
    public string Queue { get; set; } = string.Empty;
    public string Who { get; set; } = string.Empty;
    public string What { get; set; } = string.Empty;
    public DateTime WaitingSince { get; set; }
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = "Index";
    public int? RecordId { get; set; }
}

public class AdminDashboardActivityViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
