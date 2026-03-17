using System;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Db {


    public interface IDBReader : IDisposable {

        //methods
        int GetColumnsCount();
        DBColumns GetColumns();
        DBRow? Read();
        bool Read(object?[] values);
        bool NextResult();

        //async methods
        Task<DBColumns> GetColumnsAsync(CancellationToken cancellationToken = default);
        Task<DBRow?> ReadAsync(CancellationToken cancellationToken = default);
        Task<bool> ReadAsync(object?[] values, CancellationToken cancellationToken = default);
        Task<bool> NextResultAsync(CancellationToken cancellationToken = default);
    }


}
