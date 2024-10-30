namespace veterans_site.Services
{
    public interface IEmailService
    {
        Task SendConsultationConfirmationAsync(string toEmail, string recipientName, string consultationTitle, DateTime consultationDateTime);
        Task SendRoleChangedEmailAsync(string toEmail, string userName, string newRole);
        Task SendRoleChangeConfirmationEmailAsync(string toEmail, string userName, string newRole, string confirmationLink, string rejectLink);

    }
}
