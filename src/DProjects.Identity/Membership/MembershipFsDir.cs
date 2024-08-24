using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;

using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Utils;
using Microsoft.Extensions.Caching.Memory;

namespace DProjects.Identity.Membership {


    public class MembershipFsDir : IMembership {
        
        //vars
        private readonly IFilesystem mFs;
        private readonly string mPath;

        //ctor
        public MembershipFsDir(IFilesystem filesystem, string path) {
            mFs = filesystem;
            mPath = path;
            if (!filesystem.ExistsDirectory(path)) {
                filesystem.CreateDirectory(path);
            }
        }

        //useres methods
        public async Task<MembershipUser?> GetUserAsync(string id, CancellationToken cancellationToken) {
            var path = PathUtils.Combine(mPath, UrlUtils.UrlEncode(id) + ".user.json");
            return await GetUserFromPathAsync(path, cancellationToken);
        }
        public async Task<bool> ExistUserAsync(string id, CancellationToken cancellationToken) {
            var path = PathUtils.Combine(mPath, UrlUtils.UrlEncode(id) + ".user.json");
            return await mFs.ExistsFileAsync(path, cancellationToken);
        }
        public async IAsyncEnumerable<MembershipUser> ListUsersAsync(string pattern, [EnumeratorCancellation] CancellationToken cancellationToken) {
            await foreach (var entry in mFs.GetEntriesAsync(mPath, GetModes.All, pattern + "*.user.json", cancellationToken)) {
                var user = await GetUserFromPathAsync(entry.Path, cancellationToken);
                if (user != null) {
                    yield return user;
                }
            }
        }
        public async Task AddUserAsync(MembershipUser user, CancellationToken cancellationToken) {
            var path = PathUtils.Combine(mPath, UrlUtils.UrlEncode(user.Identity.Id) + ".user.json");
            if (await mFs.ExistsFileAsync(path, cancellationToken)) throw new System.Exception("Unable to add user: already exists: " + user.Id);
            user.Identity.Created = System.DateTime.Now;
            user.Identity.Modified = System.DateTime.Now;
            var json = System.Text.Json.JsonSerializer.Serialize(user, new System.Text.Json.JsonSerializerOptions() {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await mFs.SaveTextFileAsync(path, json, System.Text.Encoding.UTF8, cancellationToken);
        }
        public async Task RemoveUserAsync(string id, CancellationToken cancellationToken) {
            var path = PathUtils.Combine(mPath, UrlUtils.UrlEncode(id) + ".user.json");
            if (!await mFs.ExistsFileAsync(path, cancellationToken)) throw new System.Exception("Unable to remove user: not found: " + id);
            await mFs.DeleteFileAsync(path, cancellationToken);
        }
        public async Task SaveUserAsync(MembershipUser user, CancellationToken cancellationToken) {
            var path = PathUtils.Combine(mPath, UrlUtils.UrlEncode(user.Identity.Id) + ".user.json");
            if (!await mFs.ExistsFileAsync(path, cancellationToken)) throw new System.Exception("Unable to save user: not found: " + user.Id);
            user.Identity.Modified = System.DateTime.Now;
            var json = System.Text.Json.JsonSerializer.Serialize(user, new System.Text.Json.JsonSerializerOptions() {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await mFs.SaveTextFileAsync(path, json, System.Text.Encoding.UTF8, cancellationToken);
        }


        //roles methods
        public async Task<MembershipRole?> GetRoleAsync(string id, CancellationToken cancellationToken) {
            var path = PathUtils.Combine(mPath, UrlUtils.UrlEncode(id) + ".role.json");
            return await GetRoleFromPathAsync(path, cancellationToken);
        }
        public async Task<bool> ExistRoleAsync(string id, CancellationToken cancellationToken) {
            var path = PathUtils.Combine(mPath, UrlUtils.UrlEncode(id) + ".role.json");
            return await mFs.ExistsFileAsync(path, cancellationToken);
        }
        public async IAsyncEnumerable<MembershipRole> ListRolesAsync(string pattern, [EnumeratorCancellation] CancellationToken cancellationToken) {
            await foreach (var entry in mFs.GetEntriesAsync(mPath, GetModes.All, pattern + "*.role.json", cancellationToken)) {
                var role = await GetRoleFromPathAsync(entry.Path, cancellationToken);
                if (role != null) {
                    yield return role;
                }
            }
        }
        public async Task AddRoleAsync(MembershipRole Role, CancellationToken cancellationToken) {
            var json = System.Text.Json.JsonSerializer.Serialize(Role, new System.Text.Json.JsonSerializerOptions() {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var path = PathUtils.Combine(mPath, UrlUtils.UrlEncode(Role.Id) + ".role.json");
            if (await mFs.ExistsFileAsync(path, cancellationToken)) throw new System.Exception("Unable to add Role: already exists: " + Role.Id);
            await mFs.SaveTextFileAsync(path, json, System.Text.Encoding.UTF8, cancellationToken);
        }
        public async Task RemoveRoleAsync(string id, CancellationToken cancellationToken) {
            var path = PathUtils.Combine(mPath, UrlUtils.UrlEncode(id) + ".role.json");
            if (!await mFs.ExistsFileAsync(path, cancellationToken)) throw new System.Exception("Unable to remove Role: not found: " + id);
            await mFs.DeleteFileAsync(path, cancellationToken);
        }
        public async Task SaveRoleAsync(MembershipRole Role, CancellationToken cancellationToken) {
            var json = System.Text.Json.JsonSerializer.Serialize(Role, new System.Text.Json.JsonSerializerOptions() {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var path = PathUtils.Combine(mPath, UrlUtils.UrlEncode(Role.Id) + ".role.json");
            if (!await mFs.ExistsFileAsync(path, cancellationToken)) throw new System.Exception("Unable to save Role: not found: " + Role.Id);
            await mFs.SaveTextFileAsync(path, json, System.Text.Encoding.UTF8, cancellationToken);
        }


        //private
        private async Task<MembershipUser?> GetUserFromPathAsync(string path, CancellationToken cancellationToken) {
            if (!await mFs.ExistsFileAsync(path, cancellationToken)) return null;
            var json = await mFs.LoadTextFileAsync(path, System.Text.Encoding.UTF8, cancellationToken);
            var user = System.Text.Json.JsonSerializer.Deserialize<MembershipUser>(json, new JsonSerializerOptions() { 
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return user;
        }
        private async Task<MembershipRole?> GetRoleFromPathAsync(string path, CancellationToken cancellationToken) {
            if (!await mFs.ExistsFileAsync(path, cancellationToken)) return null;
            var json = await mFs.LoadTextFileAsync(path, System.Text.Encoding.UTF8, cancellationToken);
            var role = System.Text.Json.JsonSerializer.Deserialize<MembershipRole>(json, new JsonSerializerOptions() {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return role;
        }


    }

}