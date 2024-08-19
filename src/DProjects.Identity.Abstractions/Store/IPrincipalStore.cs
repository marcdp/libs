using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Identity.Store {

    public interface IPrincipalStore {

        Task<ClaimsPrincipal?> GetAsync(string id, CancellationToken cancellationToken);
        IAsyncEnumerable<ClaimsPrincipal> ListAsync(string pattern, CancellationToken cancellationToken);
        Task AddAsync(ClaimsPrincipal element, CancellationToken cancellationToken);
        Task SaveAsync(ClaimsPrincipal element, CancellationToken cancellationToken);
        Task RemoveAsync(string id, CancellationToken cancellationToken);
    }
     

}