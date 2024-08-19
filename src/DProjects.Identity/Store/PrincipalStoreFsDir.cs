using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using DProjects.Fs;

namespace DProjects.Identity.Store {

    public class PrincipalStoreFsDir(IFilesystem filesystem, string path) : IPrincipalStore {



        //method
        public Task AddAsync(ClaimsPrincipal element, CancellationToken cancellationToken) {

            //foreach (var iii in element.Identities) { 
            //}
            throw new System.NotImplementedException();
        }
        public Task<ClaimsPrincipal?> GetAsync(string id, CancellationToken cancellationToken) {
            throw new System.NotImplementedException();
        }
        public IAsyncEnumerable<ClaimsPrincipal> ListAsync(string pattern, CancellationToken cancellationToken) {
            throw new System.NotImplementedException();
        }
        public Task RemoveAsync(string id, CancellationToken cancellationToken) {
            throw new System.NotImplementedException();
        }
        public Task SaveAsync(ClaimsPrincipal element, CancellationToken cancellationToken) {
            throw new System.NotImplementedException();
        }

    }

}