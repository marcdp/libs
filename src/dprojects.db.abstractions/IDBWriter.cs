using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace DProjects.Db {


    public interface IDBWriter : IDisposable, IAsyncDisposable {

        //props
        DBColumns Columns { get; }

        //sync methods
        void Write(params object?[] values);
        void Write(DBRow row);
        void Write(IDictionary<string, object?> row);
        void Flush();

        //async methods
        Task WriteAsync(params object?[] values);
        Task WriteAsync(DBRow row, CancellationToken cancellationToken);
        Task WriteAsync(IDictionary<string, object?> row, CancellationToken cancellationToken);
        Task FlushAsync();

    }


}
