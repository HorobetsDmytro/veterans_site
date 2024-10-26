using veterans_site.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace veterans_site.Interfaces
{
    public interface INewsRepository : IGenericRepository<News>
    {
        Task<IEnumerable<News>> GetLatestNewsAsync(int count);
        Task<IEnumerable<News>> SearchNewsByTitleAsync(string title);
    }
}
