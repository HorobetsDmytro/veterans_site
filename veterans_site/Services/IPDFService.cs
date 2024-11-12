using veterans_site.Models;

namespace veterans_site.Services
{
    public interface IPDFService
    {
        byte[] GenerateConsultationConfirmation(Consultation consultation, ApplicationUser user);
    }
}
