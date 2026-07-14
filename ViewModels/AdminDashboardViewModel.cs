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
    public int PendingDonationsCount { get; set; }
    public int ConfirmedDonationsCount { get; set; }

    public List<AdminDashboardActivityViewModel> RecentActivity { get; set; } = new();
}

public class AdminDashboardActivityViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
