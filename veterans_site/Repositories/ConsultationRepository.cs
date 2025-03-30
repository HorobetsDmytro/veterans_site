using Microsoft.EntityFrameworkCore;
using veterans_site.Data;
using veterans_site.Interfaces;
using veterans_site.Models;

namespace veterans_site.Repositories
{
    public class ConsultationRepository : GenericRepository<Consultation>, IConsultationRepository
    {
        private readonly ILogger<ConsultationRepository> _logger;
        public ConsultationRepository(VeteranSupportDbContext context, ILogger<ConsultationRepository> logger) : base(context)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<Consultation>> GetFilteredConsultationsAsync(
        ConsultationType? type = null,
        ConsultationFormat? format = null,
        ConsultationStatus? status = null,
        double? minPrice = null,
        double? maxPrice = null,
        string sortOrder = null,
        int page = 1,
        int pageSize = 10,
        string specialistName = null,
        bool parentOnly = false)
        {
            var query = _context.Consultations
                .Include(c => c.Slots)
                .Include(c => c.Bookings)
                .AsQueryable();

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

            if (!string.IsNullOrEmpty(specialistName))
                query = query.Where(c => c.SpecialistName == specialistName);

            if (parentOnly)
                query = query.Where(c => c.IsParent);

            query = sortOrder switch
            {
                "date_desc" => query.OrderByDescending(c => c.DateTime),
                "price_asc" => query.OrderBy(c => c.Price),
                "price_desc" => query.OrderByDescending(c => c.Price),
                _ => query.OrderBy(c => c.DateTime)
            };

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
            int pageSize = 10,
            string specialistName = null,
            bool parentOnly = false)
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

            if (!string.IsNullOrEmpty(specialistName))
                query = query.Where(c => c.SpecialistName == specialistName);

            if (parentOnly)
                query = query.Where(c => c.IsParent);

            var total = await query.CountAsync();
            return (int)Math.Ceiling(total / (double)pageSize);
        }

        public async Task<Consultation> GetByIdWithSlotsAsync(int id)
        {
            return await _context.Consultations
                .Include(c => c.Slots)
                    .ThenInclude(s => s.User)
                .Include(c => c.Bookings)
                    .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Consultation>> GetUserConsultationsAsync(string userId)
        {
            var consultations = await _context.Consultations
                .Include(c => c.Slots)
                .Include(c => c.Bookings)
                .Where(c =>
                    (c.Format == ConsultationFormat.Group && c.Bookings.Any(b => b.UserId == userId)) ||
                    (c.Format == ConsultationFormat.Individual && c.Slots.Any(s => s.UserId == userId && s.IsBooked)))
                .OrderByDescending(c => c.DateTime)
                .ToListAsync();

            return consultations;
        }

        public async Task<bool> IsUserRegisteredForConsultationAsync(string userId, int consultationId)
        {
            return await _context.Consultations
                .AnyAsync(c => c.Id == consultationId && c.UserId == userId);
        }

        public async Task<int> GetUserBookingsCountAsync(string userId)
        {
            return await _context.Consultations
                .CountAsync(c => c.UserId == userId && c.Status != ConsultationStatus.Cancelled);
        }

        public async Task<bool> CanBookConsultationAsync(int consultationId)
        {
            var consultation = await _context.Consultations
                .FirstOrDefaultAsync(c => c.Id == consultationId);

            if (consultation == null)
                return false;

            if (consultation.Format == ConsultationFormat.Individual)
                return !consultation.IsBooked;

            return consultation.BookedParticipants < consultation.MaxParticipants;
        }

        public async Task<IEnumerable<Consultation>> GetAvailableConsultationsAsync(
            ConsultationType? type = null,
            ConsultationFormat? format = null,
            double? minPrice = null,
            double? maxPrice = null,
            string sortOrder = null,
            int page = 1,
            int pageSize = 6)
        {
            var query = _context.Consultations
                .Include(c => c.Slots)
                .Where(c => c.Status == ConsultationStatus.Planned);

            if (type.HasValue)
                query = query.Where(c => c.Type == type.Value);

            if (format.HasValue)
                query = query.Where(c => c.Format == format.Value);

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

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> BookConsultationAsync(int consultationId, string userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == consultationId);

                if (consultation == null)
                    return false;

                // Перевіряємо можливість бронювання
                if (consultation.Format == ConsultationFormat.Individual)
                {
                    if (consultation.IsBooked)
                        return false;

                    consultation.IsBooked = true;
                    consultation.UserId = userId;
                }
                else // Group
                {
                    if (consultation.BookedParticipants >= consultation.MaxParticipants)
                        return false;

                    consultation.BookedParticipants++;

                    var booking = new ConsultationBooking
                    {
                        ConsultationId = consultationId,
                        UserId = userId,
                        BookingTime = DateTime.Now
                    };

                    await _context.ConsultationBookings.AddAsync(booking);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> IsUserBookedForConsultationAsync(int consultationId, string userId)
        {
            var consultation = await _context.Consultations
                .Include(c => c.Slots)
                .FirstOrDefaultAsync(c => c.Id == consultationId);

            if (consultation == null)
                return false;

            if (consultation.Format == ConsultationFormat.Individual)
            {
                return await _context.ConsultationSlots
                    .AnyAsync(s => s.ConsultationId == consultationId &&
                                  s.UserId == userId &&
                                  s.IsBooked);
            }
            else
            {
                return await _context.ConsultationBookings
                    .AnyAsync(b => b.ConsultationId == consultationId &&
                                  b.UserId == userId);
            }
        }

        public async Task RemoveBookingAsync(int consultationId, string userId)
        {
            var booking = await _context.ConsultationBookings
                .FirstOrDefaultAsync(b => b.ConsultationId == consultationId && b.UserId == userId);

            if (booking != null)
            {
                _context.ConsultationBookings.Remove(booking);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetUserConsultationsCount(string userId)
        {
            return await _context.Consultations
                .Where(c => c.UserId == userId || c.Bookings.Any(b => b.UserId == userId))
                .CountAsync();
        }

        public async Task RemoveUserBookingsAsync(string userId)
        {
            // Видаляємо записи з групових консультацій
            var bookings = await _context.ConsultationBookings
                .Where(b => b.UserId == userId)
                .ToListAsync();

            if (bookings.Any())
            {
                _context.ConsultationBookings.RemoveRange(bookings);
            }

            var groupConsultations = await _context.Consultations
                .Where(c => c.Format == ConsultationFormat.Group && c.Bookings.Any(b => b.UserId == userId))
                .ToListAsync();

            foreach (var consultation in groupConsultations)
            {
                consultation.BookedParticipants--;
            }

            var individualConsultations = await _context.Consultations
                .Where(c => c.Format == ConsultationFormat.Individual && c.UserId == userId)
                .ToListAsync();

            foreach (var consultation in individualConsultations)
            {
                consultation.UserId = null;
                consultation.IsBooked = false;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CancelConsultationAsync(int consultationId, string userId)
        {
            var consultation = await _context.Consultations
                .Include(c => c.Bookings)
                .FirstOrDefaultAsync(c => c.Id == consultationId);

            if (consultation == null)
                return;

            if (consultation.Format == ConsultationFormat.Individual && consultation.UserId == userId)
            {
                consultation.UserId = null;
                consultation.IsBooked = false;
            }
            else if (consultation.Format == ConsultationFormat.Group)
            {
                var booking = consultation.Bookings.FirstOrDefault(b => b.UserId == userId);
                if (booking != null)
                {
                    _context.ConsultationBookings.Remove(booking);
                    consultation.BookedParticipants--;
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasActiveConsultationsAsync(string userId)
        {
            return await _context.Consultations
                .AnyAsync(c =>
                    (c.UserId == userId || c.Bookings.Any(b => b.UserId == userId)) &&
                    c.DateTime > DateTime.Now &&
                    c.Status != ConsultationStatus.Cancelled);
        }

        public async Task<bool> BookConsultationSlotAsync(int consultationId, int slotId, string userId)
        {
            try
            {
                var slot = await _context.ConsultationSlots
                    .Include(s => s.Consultation)
                    .FirstOrDefaultAsync(s => s.Id == slotId && s.ConsultationId == consultationId);

                if (slot == null || slot.IsBooked)
                {
                    return false;
                }

                slot.IsBooked = true;
                slot.UserId = userId;

                var booking = new ConsultationBooking
                {
                    ConsultationId = consultationId,
                    UserId = userId,
                    BookingTime = DateTime.Now
                };

                _context.ConsultationBookings.Add(booking);

                var consultation = await _context.Consultations
                    .FindAsync(consultationId);

                if (consultation != null)
                {
                    consultation.BookedParticipants++;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error booking consultation slot: {ex.Message}");
                return false;
            }
        }

        public async Task<IEnumerable<Consultation>> GetUserConsultationsWithSlotsAsync(string userId)
        {
            return await _context.Consultations
                .Include(c => c.Slots)
                .Where(c => c.UserId == userId ||
                            c.Slots.Any(s => s.UserId == userId) ||
                            c.Bookings.Any(b => b.UserId == userId))
                .ToListAsync();
        }

        public async Task UpdateConsultationStatusesAsync()
        {
            var currentTime = DateTime.Now;
            var consultations = await _context.Consultations
                .Where(c => c.Status != ConsultationStatus.Cancelled &&
                           c.Status != ConsultationStatus.Completed)
                .ToListAsync();

            foreach (var consultation in consultations)
            {
                var consultationEndTime = consultation.Format == ConsultationFormat.Individual
                    ? consultation.EndDateTime
                    : consultation.DateTime.AddMinutes(consultation.Duration);

                if (consultationEndTime <= currentTime)
                {
                    consultation.Status = ConsultationStatus.Completed;
                }
                else if (consultation.DateTime <= currentTime && currentTime < consultationEndTime)
                {
                    consultation.Status = ConsultationStatus.InProgress;
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
