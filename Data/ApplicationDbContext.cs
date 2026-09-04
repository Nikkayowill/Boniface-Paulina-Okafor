using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data;

public class ApplicationDbContext :
    IdentityDbContext<ApplicationUser>,
    IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AppointmentRequest> AppointmentRequests => Set<AppointmentRequest>();
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<ContactSubmission> ContactSubmissions => Set<ContactSubmission>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    // Patient portal
    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public DbSet<PatientAppointment> PatientAppointments => Set<PatientAppointment>();
    public DbSet<PatientMessage> PatientMessages => Set<PatientMessage>();

    // Scheduling & notifications
    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();
    public DbSet<AppointmentSlot> AppointmentSlots => Set<AppointmentSlot>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<TeleconsultationRequest> TeleconsultationRequests => Set<TeleconsultationRequest>();
    public DbSet<BillPayment> BillPayments => Set<BillPayment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Backs the trigram (gin_trgm_ops) indexes used by free-text ILIKE search
        // (doctor/department/post lookup, admin bill-payment search) so those queries
        // get index scans instead of sequential scans as the tables grow.
        builder.HasPostgresExtension("pg_trgm");

        // Each entity's fluent configuration lives in its own IEntityTypeConfiguration<T> class
        // under Data/Configurations, applied here in one pass. Keeps this method a readable
        // composition point instead of a single ever-growing method.
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
