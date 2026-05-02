using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Okafor_.NET.Data;
using Okafor_.NET.Models;
using System.Web;

namespace Okafor_.NET.Services;

public class LeanNotificationService : INotificationService
{
    private readonly IEmailSender _emailSender;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public LeanNotificationService(
        IEmailSender emailSender,
        ApplicationDbContext context,
        IConfiguration config)
    {
        _emailSender = emailSender;
        _context = context;
        _config = config;
    }

    public async Task<bool> SendConfirmationAsync(NotificationRequest request)
    {
        var subject = $"Your Appointment is Confirmed — BP Okafor Memorial Hospital";
        var body = BuildConfirmationEmailHtml(request);

        return await SendEmailAndLog(request.PatientEmail, subject, body, "Email", request.AppointmentRequestId);
    }

    public async Task<bool> SendAdminAlertAsync(NotificationRequest request)
    {
        var adminEmail = _config["Notifications:AdminEmail"] ?? "admin@okaformemorial.org";
        var subject = $"[New Booking] {request.PatientName} — {request.AppointmentDateTime:MMM d, yyyy h:mm tt}";
        var body = BuildAdminAlertEmailHtml(request);

        return await SendEmailAndLog(adminEmail, subject, body, "Email", request.AppointmentRequestId);
    }

    public async Task<bool> SendReminderAsync(NotificationRequest request)
    {
        var subject = $"Reminder: Your appointment tomorrow at BP Okafor Memorial Hospital";
        var body = BuildReminderEmailHtml(request);

        return await SendEmailAndLog(request.PatientEmail, subject, body, "Email", request.AppointmentRequestId);
    }

    public string BuildWhatsAppUrl(NotificationRequest request)
    {
        var waNumber = _config["Notifications:WhatsAppNumber"]?.Replace("+", "") ?? "2348012345678";
        var message = $"Hello, I have just booked an appointment at BP Okafor Memorial Hospital.\n\n" +
                      $"Name: {request.PatientName}\n" +
                      $"Date: {request.AppointmentDateTime:MMMM d, yyyy}\n" +
                      $"Time: {request.AppointmentDateTime:h:mm tt}\n" +
                      $"Doctor: {request.DoctorName}\n" +
                      $"Department: {request.Department}\n" +
                      $"Ref: {request.ConfirmationRef}";

        return $"https://wa.me/{waNumber}?text={Uri.EscapeDataString(message)}";
    }

    // ──────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────

    private async Task<bool> SendEmailAndLog(string recipient, string subject, string body, string channel, int? appointmentRequestId)
    {
        var log = new NotificationLog
        {
            Channel = channel,
            Recipient = recipient,
            MessageBody = subject,
            AppointmentRequestId = appointmentRequestId,
            SentAt = DateTime.UtcNow
        };

        try
        {
            await _emailSender.SendEmailAsync(recipient, subject, body);
            log.Success = true;
        }
        catch (Exception ex)
        {
            log.Success = false;
            log.ErrorMessage = ex.Message;
        }

        _context.NotificationLogs.Add(log);
        await _context.SaveChangesAsync();

        return log.Success;
    }

    private string BuildConfirmationEmailHtml(NotificationRequest r)
    {
        var hospitalPhone = _config["Notifications:HospitalPhone"] ?? "112";
        return $"""
            <!DOCTYPE html>
            <html>
            <head><meta charset="utf-8"/></head>
            <body style="margin:0;padding:0;background:#f4f4f0;font-family:Georgia,serif;">
              <table width="100%" cellpadding="0" cellspacing="0">
                <tr><td align="center" style="padding:40px 16px;">
                  <table width="600" cellpadding="0" cellspacing="0" style="background:#ffffff;border:1px solid #ddd;">
                    <tr><td style="background:#1e4d6b;padding:28px 36px;">
                      <p style="margin:0;font-size:10px;letter-spacing:3px;text-transform:uppercase;color:#a8c8d8;">Boniface &amp; Paulina Okafor</p>
                      <p style="margin:4px 0 0;font-size:22px;font-weight:bold;color:#ffffff;">Memorial Hospital</p>
                    </td></tr>
                    <tr><td style="padding:36px;">
                      <p style="margin:0;font-size:16px;color:#1c1c1c;">Dear <strong>{r.PatientName}</strong>,</p>
                      <p style="margin:16px 0;font-size:14px;line-height:1.7;color:#4a4a4a;">
                        Your appointment has been confirmed. Please review the details below and arrive at least
                        15 minutes before your scheduled time.
                      </p>
                      <table width="100%" cellpadding="0" cellspacing="0" style="border:1px solid #e0e0e0;margin:24px 0;">
                        <tr style="background:#f9f7f4;">
                          <td style="padding:12px 16px;font-size:11px;font-weight:bold;text-transform:uppercase;letter-spacing:1px;color:#6a6a6a;width:40%;">Date</td>
                          <td style="padding:12px 16px;font-size:14px;color:#1c1c1c;font-weight:bold;">{r.AppointmentDateTime:dddd, MMMM d, yyyy}</td>
                        </tr>
                        <tr>
                          <td style="padding:12px 16px;font-size:11px;font-weight:bold;text-transform:uppercase;letter-spacing:1px;color:#6a6a6a;">Time</td>
                          <td style="padding:12px 16px;font-size:14px;color:#1c1c1c;font-weight:bold;">{r.AppointmentDateTime:h:mm tt}</td>
                        </tr>
                        <tr style="background:#f9f7f4;">
                          <td style="padding:12px 16px;font-size:11px;font-weight:bold;text-transform:uppercase;letter-spacing:1px;color:#6a6a6a;">Doctor</td>
                          <td style="padding:12px 16px;font-size:14px;color:#1c1c1c;">{r.DoctorName}</td>
                        </tr>
                        <tr>
                          <td style="padding:12px 16px;font-size:11px;font-weight:bold;text-transform:uppercase;letter-spacing:1px;color:#6a6a6a;">Department</td>
                          <td style="padding:12px 16px;font-size:14px;color:#1c1c1c;">{r.Department}</td>
                        </tr>
                        <tr style="background:#f9f7f4;">
                          <td style="padding:12px 16px;font-size:11px;font-weight:bold;text-transform:uppercase;letter-spacing:1px;color:#6a6a6a;">Reference</td>
                          <td style="padding:12px 16px;font-size:16px;color:#1e4d6b;font-weight:bold;letter-spacing:2px;">{r.ConfirmationRef}</td>
                        </tr>
                      </table>
                      <p style="margin:0;font-size:13px;line-height:1.7;color:#4a4a4a;">
                        <strong>Please remember to bring:</strong><br/>
                        &bull; Government-issued photo ID<br/>
                        &bull; Any previous test results, scans, or discharge summaries<br/>
                        &bull; Your current medications list
                      </p>
                      <p style="margin:24px 0 0;font-size:13px;color:#4a4a4a;">
                        Need to reschedule? Call us: <strong>{hospitalPhone}</strong> or email 
                        <a href="mailto:info@okaformemorial.org" style="color:#1e4d6b;">info@okaformemorial.org</a>
                      </p>
                    </td></tr>
                    <tr><td style="background:#f4f4f0;padding:20px 36px;border-top:1px solid #ddd;">
                      <p style="margin:0;font-size:11px;color:#8a8a8a;text-align:center;">
                        Boniface &amp; Paulina Okafor Memorial Hospital &nbsp;|&nbsp; {hospitalPhone} &nbsp;|&nbsp; info@okaformemorial.org
                      </p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    private string BuildAdminAlertEmailHtml(NotificationRequest r)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:Arial,sans-serif;padding:24px;color:#1c1c1c;">
              <h2 style="color:#1e4d6b;">New Appointment Booking</h2>
              <table cellpadding="8" cellspacing="0" style="border-collapse:collapse;width:100%;max-width:500px;">
                <tr><td style="font-weight:bold;width:160px;">Patient</td><td>{r.PatientName}</td></tr>
                <tr style="background:#f9f7f4;"><td style="font-weight:bold;">Phone</td><td>{r.PatientPhone}</td></tr>
                <tr><td style="font-weight:bold;">Email</td><td>{r.PatientEmail}</td></tr>
                <tr style="background:#f9f7f4;"><td style="font-weight:bold;">Doctor</td><td>{r.DoctorName}</td></tr>
                <tr><td style="font-weight:bold;">Department</td><td>{r.Department}</td></tr>
                <tr style="background:#f9f7f4;"><td style="font-weight:bold;">Date &amp; Time</td><td>{r.AppointmentDateTime:dddd, MMMM d, yyyy h:mm tt}</td></tr>
                <tr><td style="font-weight:bold;">Reference</td><td style="font-size:16px;font-weight:bold;color:#1e4d6b;letter-spacing:2px;">{r.ConfirmationRef}</td></tr>
              </table>
              <p style="margin-top:24px;">
                <a href="/Admin/Dashboard" style="background:#1e4d6b;color:#fff;padding:10px 20px;text-decoration:none;font-size:12px;font-weight:bold;text-transform:uppercase;letter-spacing:1px;">View in Admin Dashboard</a>
              </p>
            </body>
            </html>
            """;
    }

    private string BuildReminderEmailHtml(NotificationRequest r)
    {
        var hospitalPhone = _config["Notifications:HospitalPhone"] ?? "112";
        return $"""
            <!DOCTYPE html>
            <html>
            <body style="font-family:Georgia,serif;padding:24px;background:#f4f4f0;">
              <table width="600" cellpadding="0" cellspacing="0" style="background:#fff;border:1px solid #ddd;margin:0 auto;">
                <tr><td style="background:#1e4d6b;padding:24px 32px;">
                  <p style="margin:0;font-size:20px;font-weight:bold;color:#fff;">Appointment Reminder</p>
                  <p style="margin:4px 0 0;font-size:11px;color:#a8c8d8;letter-spacing:2px;text-transform:uppercase;">Boniface &amp; Paulina Okafor Memorial Hospital</p>
                </td></tr>
                <tr><td style="padding:32px;">
                  <p style="margin:0;font-size:15px;color:#1c1c1c;">Dear <strong>{r.PatientName}</strong>,</p>
                  <p style="margin:16px 0;font-size:14px;line-height:1.7;color:#4a4a4a;">
                    This is a friendly reminder that you have an appointment <strong>tomorrow</strong>:
                  </p>
                  <p style="font-size:18px;font-weight:bold;color:#1e4d6b;">{r.AppointmentDateTime:dddd, MMMM d, yyyy}</p>
                  <p style="font-size:15px;color:#1c1c1c;margin-top:4px;">{r.AppointmentDateTime:h:mm tt} &nbsp;&mdash;&nbsp; {r.DoctorName} ({r.Department})</p>
                  <p style="font-size:13px;color:#4a4a4a;margin-top:16px;">Reference: <strong style="letter-spacing:2px;">{r.ConfirmationRef}</strong></p>
                  <p style="font-size:13px;color:#4a4a4a;margin-top:16px;">
                    Need to reschedule? Please call us as soon as possible: <strong>{hospitalPhone}</strong>
                  </p>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }
}
