using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using veterans_site.Data;
using veterans_site.Interfaces;
using veterans_site.Models;

namespace veterans_site.Repositories
{
    public class NewsRepository : GenericRepository<News>, INewsRepository
    {
        private readonly VeteranSupportDBContext _context;

        public NewsRepository(VeteranSupportDBContext context) : base(context)
        {
            _context = context;
        }

        public override async Task UpdateAsync(News entity)
        {
            var existingNews = await _context.News.FindAsync(entity.Id);
            if (existingNews != null)
            {
                existingNews.Title = entity.Title;
                existingNews.Content = entity.Content;
                existingNews.PublishDate = entity.PublishDate;

                if (entity.ImagePath != null)
                {
                    existingNews.ImagePath = entity.ImagePath;
                }

                _context.News.Update(existingNews);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<News>> GetLatestNewsAsync(int count)
        {
            return await _dbSet
                .OrderByDescending(n => n.PublishDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<News>> SearchNewsByTitleAsync(string title)
        {
            return await FindAsync(n => n.Title.Contains(title));
        }
    }
}
    