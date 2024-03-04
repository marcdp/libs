
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Storage {


    //interface
    public interface ILogStorageEntryReader : IDisposable {

        //methods
        Task<LogEntry?> ReadAsync(CancellationToken cancellationToken);

    }
    


}

