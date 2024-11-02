using veterans_site.Models;

namespace veterans_site.Services
{
    public interface IEmailService
    {
        Task SendConsultationConfirmationAsync(string toEmail, string recipientName, string consultationTitle, DateTime consultationDateTime);
        Task SendRoleChangedEmailAsync(string toEmail, string userName, string newRole);
        Task SendRoleChangeConfirmationEmailAsync(string toEmail, string userName, string newRole, string confirmationLink, string rejectLink);
        Task SendNewConsultationNotificationAsync(string toEmail, string veteranName, Consultation consultation);
        Task SendBookingRequestToSpecialistAsync(string specialistEmail, string specialistName,
        string userFullName, string consultationTitle, DateTime consultationTime,
        string confirmLink, string rejectLink);
        Task SendBookingRejectedEmailAsync(string userEmail, string userName, string consultationTitle);
    }
}
