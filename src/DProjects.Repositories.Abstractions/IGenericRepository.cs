using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace DProjects.Repositories {

    public interface IGenericRepository<TEntity, TKey> where TEntity : IGenericRepositoryElement<TKey> {

        //methods
        Task<TEntity?> GetAsync(string id, CancellationToken cancellationToken);
        IAsyncEnumerable<TEntity> ListAsync(string pattern, CancellationToken cancellationToken);
        Task AddAsync(TEntity element, CancellationToken cancellationToken);
        Task SaveAsync(TEntity element, CancellationToken cancellationToken);
        Task RemoveAsync(string id, CancellationToken cancellationToken);

    }

}