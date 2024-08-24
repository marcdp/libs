using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace DProjects.Identity.Membership {

    public interface IMembershipUserAccessor {
        Task<MembershipUser?> GetUserAsync(string id, CancellationToken cancellationToken);
    }

    public interface IMembership : IMembershipUserAccessor {

        //users
        Task<bool> ExistUserAsync(string id, CancellationToken cancellationToken);
        IAsyncEnumerable<MembershipUser> ListUsersAsync(string pattern, CancellationToken cancellationToken);
        Task AddUserAsync(MembershipUser user, CancellationToken cancellationToken);
        Task SaveUserAsync(MembershipUser user, CancellationToken cancellationToken);
        Task RemoveUserAsync(string id, CancellationToken cancellationToken);

        //roles
        Task<MembershipRole?> GetRoleAsync(string id, CancellationToken cancellationToken);
        Task<bool> ExistRoleAsync(string id, CancellationToken cancellationToken);
        IAsyncEnumerable<MembershipRole> ListRolesAsync(string pattern, CancellationToken cancellationToken);
        Task AddRoleAsync(MembershipRole role, CancellationToken cancellationToken);
        Task SaveRoleAsync(MembershipRole role, CancellationToken cancellationToken);
        Task RemoveRoleAsync(string id, CancellationToken cancellationToken);
    }

}