
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;

using Microsoft.Extensions.Logging;


namespace DProjects.Queues {

    [Protocol("fs-dir", "")]
    [ProtocolExample("fs-dir:/path/to/dir", "")]
    public class QueueFsDirFactory(IFilesystem filesystem, ILogger<IFilesystem> logger) : IFactoryByUrl<IQueue> {
        public IQueue Create(string src) {
            var url = new System.Uri(src);  
            return new QueueFsDir(filesystem, url.AbsolutePath, logger);
        }

    }

}
