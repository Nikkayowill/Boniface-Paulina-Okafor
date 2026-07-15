using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Models;

namespace Okafor_.NET.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
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

    // Patient portal
    public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public DbSet<PatientAppointment> PatientAppointments => Set<PatientAppointment>();
    public DbSet<PatientMessage> PatientMessages => Set<PatientMessage>();

    // Scheduling & notifications
    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();
    public DbSet<AppointmentSlot> AppointmentSlots => Set<AppointmentSlot>();
    public DbSet<WhatsAppSchedulingSession> WhatsAppSchedulingSessions => Set<WhatsAppSchedulingSession>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<TeleconsultationRequest> TeleconsultationRequests => Set<TeleconsultationRequest>();
    public DbSet<BillPayment> BillPayments => Set<BillPayment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.Property(login => login.LoginProvider).HasMaxLength(128);
            entity.Property(login => login.ProviderKey).HasMaxLength(128);
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.Property(token => token.LoginProvider).HasMaxLength(128);
            entity.Property(token => token.Name).HasMaxLength(128);
        });

        builder.Entity<Post>()
            .HasIndex(p => p.Slug)
            .IsUnique();

        builder.Entity<Doctor>()
            .HasIndex(d => d.Slug)
            .IsUnique()
            .HasFilter("[Slug] IS NOT NULL");

        builder.Entity<Donation>()
            .Property(d => d.Amount)
            .HasPrecision(18, 2);

        builder.Entity<Donation>()
            .HasIndex(d => d.PaymentReference)
            .IsUnique();

        builder.Entity<Donation>()
            .Property(d => d.Status)
            .HasConversion<string>();

        builder.Entity<Donation>()
            .HasIndex(d => d.ProviderReference)
            .IsUnique()
            .HasFilter("[ProviderReference] IS NOT NULL");

        builder.Entity<Donation>()
            .HasIndex(d => new { d.Status, d.CreatedAt });

        builder.Entity<AppointmentRequest>()
            .Property(a => a.Status)
            .HasConversion<string>();

        builder.Entity<TeleconsultationRequest>()
            .Property(t => t.Status)
            .HasConversion<string>();

        builder.Entity<TeleconsultationRequest>()
            .Property(t => t.ConsultationType)
            .HasConversion<string>();

        builder.Entity<TeleconsultationRequest>()
            .HasIndex(t => new { t.Status, t.CreatedAt });

        builder.Entity<TeleconsultationRequest>()
            .HasOne(t => t.Department)
            .WithMany()
            .HasForeignKey(t => t.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TeleconsultationRequest>()
            .HasOne(t => t.Doctor)
            .WithMany()
            .HasForeignKey(t => t.DoctorId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<TeleconsultationRequest>()
            .HasOne(t => t.ApplicationUser)
            .WithMany()
            .HasForeignKey(t => t.ApplicationUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<TeleconsultationRequest>()
            .HasOne(t => t.PatientProfile)
            .WithMany()
            .HasForeignKey(t => t.PatientProfileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<BillPayment>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);

        builder.Entity<BillPayment>()
            .Property(p => p.Status)
            .HasConversion<string>();

        builder.Entity<BillPayment>()
            .HasIndex(p => p.InvoiceNumber)
            .IsUnique();

        builder.Entity<BillPayment>()
            .HasIndex(p => p.ProviderReference)
            .IsUnique()
            .HasFilter("[ProviderReference] IS NOT NULL");

        builder.Entity<BillPayment>()
            .HasIndex(p => new { p.Status, p.CreatedAt });

        builder.Entity<BillPayment>()
            .HasOne(p => p.ApplicationUser)
            .WithMany()
            .HasForeignKey(p => p.ApplicationUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<PatientAppointment>()
            .Property(a => a.Status)
            .HasConversion<string>();

        builder.Entity<PatientProfile>()
            .HasOne(p => p.ApplicationUser)
            .WithMany()
            .HasForeignKey(p => p.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PatientProfile>()
            .HasIndex(p => p.ApplicationUserId)
            .IsUnique();

        builder.Entity<PatientAppointment>()
            .HasOne(p => p.AppointmentRequest)
            .WithMany()
            .HasForeignKey(p => p.AppointmentRequestId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<PatientAppointment>()
            .HasIndex(p => p.AppointmentRequestId)
            .IsUnique()
            .HasFilter("[AppointmentRequestId] IS NOT NULL");

        builder.Entity<AppointmentSlot>()
            .HasOne(s => s.Doctor)
            .WithMany()
            .HasForeignKey(s => s.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AppointmentSlot>()
            .HasIndex(s => new { s.DoctorId, s.SlotDateTime })
            .IsUnique();

        builder.Entity<AppointmentSlot>()
            .HasOne(s => s.AppointmentRequest)
            .WithMany()
            .HasForeignKey(s => s.AppointmentRequestId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<WhatsAppSchedulingSession>()
            .HasIndex(s => new { s.PatientPhone, s.Status, s.ExpiresAt });

        builder.Entity<WhatsAppSchedulingSession>()
            .HasOne(s => s.AppointmentRequest)
            .WithMany()
            .HasForeignKey(s => s.AppointmentRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<NotificationLog>()
            .HasOne(n => n.TeleconsultationRequest)
            .WithMany()
            .HasForeignKey(n => n.TeleconsultationRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<NotificationLog>()
            .HasIndex(n => n.TeleconsultationRequestId);

        builder.Entity<NotificationLog>()
            .HasIndex(n => n.ExternalMessageId)
            .HasFilter("[ExternalMessageId] IS NOT NULL");

        builder.Entity<PushSubscription>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PushSubscription>()
            .Property(s => s.Endpoint)
            .HasMaxLength(2048);

        builder.Entity<PushSubscription>()
            .HasIndex(s => s.EndpointHash)
            .IsUnique();

        builder.Entity<PushSubscription>()
            .HasIndex(s => s.UserId);

        builder.Entity<DoctorAvailability>()
            .HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
