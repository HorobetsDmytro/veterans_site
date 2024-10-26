using veterans_site.Models;

namespace veterans_site.Interfaces
{
    public interface IConsultationRepository : IGenericRepository<Consultation>
    {
        Task<IEnumerable<Consultation>> GetFilteredConsultationsAsync(
            ConsultationType? type = null,
            ConsultationFormat? format = null,
            ConsultationStatus? status = null,
            double? minPrice = null,
            double? maxPrice = null,
            string sortOrder = null,
            int page = 1,
            int pageSize = 6
        );

        Task<int> GetTotalPagesAsync(
            ConsultationType? type = null,
            ConsultationFormat? format = null,
            ConsultationStatus? status = null,
            double? minPrice = null,
            double? maxPrice = null,
            int pageSize = 6
        );
    }
}
