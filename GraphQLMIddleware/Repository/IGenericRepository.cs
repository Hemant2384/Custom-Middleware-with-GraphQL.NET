using System.Linq;
using System.Threading.Tasks;

namespace GraphQLMIddleware.Repository
{
    public interface IGenericRepository<TEntity>
    {
        IQueryable<TEntity> GetAll();
        Task<TEntity> GetById(int id);
    }
}