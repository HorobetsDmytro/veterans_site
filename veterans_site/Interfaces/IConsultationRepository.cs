﻿using veterans_site.Models;

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
        int pageSize = 10,
        string specialistName = null,
        bool parentOnly = false);

        Task<int> GetTotalPagesAsync(
            ConsultationType? type = null,
            ConsultationFormat? format = null,
            ConsultationStatus? status = null,
            double? minPrice = null,
            double? maxPrice = null,
            int pageSize = 10,
            string specialistName = null,
            bool parentOnly = false);

        Task<IEnumerable<Consultation>> GetUserConsultationsAsync(string userId);
        Task<bool> IsUserRegisteredForConsultationAsync(string userId, int consultationId);
        Task<int> GetUserBookingsCountAsync(string userId);
        Task<bool> CanBookConsultationAsync(int consultationId);

        Task<IEnumerable<Consultation>> GetAvailableConsultationsAsync(
            ConsultationType? type = null,
            ConsultationFormat? format = null,
            double? minPrice = null,
            double? maxPrice = null,
            string sortOrder = null,
            int page = 1,
            int pageSize = 6);

        Task<bool> BookConsultationAsync(int consultationId, string userId);
        Task<bool> IsUserBookedForConsultationAsync(int consultationId, string userId);
        Task RemoveBookingAsync(int consultationId, string userId);
        Task<int> GetUserConsultationsCount(string userId);
        Task RemoveUserBookingsAsync(string userId);
        Task CancelConsultationAsync(int consultationId, string userId);
        Task<bool> HasActiveConsultationsAsync(string userId);
        Task<Consultation> GetByIdWithSlotsAsync(int id);
        Task<bool> BookConsultationSlotAsync(int consultationId, int slotId, string userId);
        Task<IEnumerable<Consultation>> GetUserConsultationsWithSlotsAsync(string userId);
        Task UpdateConsultationStatusesAsync();
    }
}
