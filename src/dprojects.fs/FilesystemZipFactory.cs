
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System;
using System.Runtime.InteropServices;

namespace DProjects.Fs {

    [Protocol("zip", "")]
    [ProtocolUsage("zip:FSURL")]
    [ProtocolExample("zip:/path/to/file.zip", "")]
    [ProtocolExample("zip:/path/to/file.zip?isReadonly=true", "")]
    [ProtocolExample("zip:file:///native/path/to/file.zip", "")]
    [ProtocolExample("zip:smb://server/path/to/file.zip", "")]
    public class FilesystemZipFactory(IFactoryByUrl<IFilesystem> fsFactory) : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) {
            var (outerUrl, innerUrl) = UrlUtils.UnwrapUrl(src);
            var filesystem = fsFactory.Create(innerUrl);
            var isReadonly = UrlUtils.GetQueryValue<bool>(new Uri(outerUrl).Query, "isReadonly");
            return new FilesystemZip(filesystem, "/", null, isReadonly);
        }

    }

}
