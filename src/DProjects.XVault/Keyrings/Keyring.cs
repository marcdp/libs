
namespace DProjects.XVault.Keyrings {
    public abstract class Keyring {
        public abstract bool TryReadBytes(string key, out byte[] value);
        public abstract bool TryReadText(string key, out string value);
    }
}