using System.Threading.Tasks;
using System.Threading;


namespace DProjects.Secrets {

    public class SecretManagerNull() : ISecretManager {

        //methods
        public Task<bool> IsSealed(CancellationToken cancellationToken) {
            return Task.FromResult(true);
        }
        public Task<bool> Unseal(string pass, CancellationToken cancellationToken) {
            return Task.FromResult(false);
        }
        public Task Seal(CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
        public Task Seal(string password, CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }


        //methods
        public Task<Secret[]> ListAsync(string? pattern, CancellationToken cancellationToken) {
            return Task.FromResult(new Secret[] { });
        }
        public Task SetAsync(Secret secret, CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }
        public Task<bool> DelAsync(string path, CancellationToken cancellationToken) {
            return Task.FromResult(false);
        }
        public Task<Secret?> GetAsync(string name, CancellationToken cancellationToken) {
            return Task.FromResult<Secret?>(null);
        }

    }

}