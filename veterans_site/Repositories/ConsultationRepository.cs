using Microsoft.EntityFrameworkCore;
using veterans_site.Data;
using veterans_site.Interfaces;
using veterans_site.Models;

namespace veterans_site.Repositories
{
    public class ConsultationRepository : GenericRepository<Consultation>, IConsultationRepository
    {
        public ConsultationRepository(VeteranSupportDBContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Consultation>> GetFilteredConsultationsAsync(
            ConsultationType? type = null,
            ConsultationFormat? format = null,
            ConsultationStatus? status = null,
            double? minPrice = null,
            double? maxPrice = null,
            string sortOrder = null,
            int page = 1,
            int pageSize = 6)
        {
            var query = _context.Consultations.AsQueryable();

            // Застосовуємо фільтри
            if (type.HasValue)
                query = query.Where(c => c.Type == type.Value);

            if (format.HasValue)
                query = query.Where(c => c.Format == format.Value);

            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);

            if (minPrice.HasValue)
                query = query.Where(c => c.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(c => c.Price <= maxPrice.Value);

            // Сортування
            query = sortOrder switch
            {
                "date_desc" => query.OrderByDescending(c => c.DateTime),
                "date_asc" => query.OrderBy(c => c.DateTime),
                "price_desc" => query.OrderByDescending(c => c.Price),
                "price_asc" => query.OrderBy(c => c.Price),
                _ => query.OrderBy(c => c.DateTime)
            };

            // Пагінація
            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalPagesAsync(
            ConsultationType? type = null,
            ConsultationFormat? format = null,
            ConsultationStatus? status = null,
            double? minPrice = null,
            double? maxPrice = null,
            int pageSize = 6)
        {
            var query = _context.Consultations.AsQueryable();

            if (type.HasValue)
                query = query.Where(c => c.Type == type.Value);

            if (format.HasValue)
                query = query.Where(c => c.Format == format.Value);

            if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);

            if (minPrice.HasValue)
                query = query.Where(c => c.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(c => c.Price <= maxPrice.Value);

            var total = await query.CountAsync();
            return (int)Math.Ceiling(total / (double)pageSize);
        }
    }
}
