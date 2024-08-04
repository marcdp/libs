
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Web;

namespace DProjects.Fs {

    [Protocol("xml", "")]
    [ProtocolUsage("xml:FSURL")]
    [ProtocolExample("xml:file:///path/to/file.xml", "")]
    [ProtocolExample("xml:fs:///path/to/file.xml", "")]
    public class FilesystemXmlFactory(IFactoryByUrl<IFilesystem> fsFactory) : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) {
            var url = new Uri(src);
            var (outerUrl, innerUrl) = UrlUtils.UnwrapUrl(src);
            var aInnerUrl = new Uri(innerUrl);
            var filesystem = fsFactory.Create(innerUrl);
            var isReadonly = UrlUtils.GetQueryValue<bool>(new Uri(outerUrl).Query, "isReadonly");
            var init = UrlUtils.GetQueryValue<bool>(new Uri(outerUrl).Query, "init");
            var autoFlush = UrlUtils.GetQueryValue<bool>(new Uri(outerUrl).Query, "autoFlush");
            var gzip = UrlUtils.GetQueryValue<bool>(new Uri(outerUrl).Query, "gzip");
            var password = UrlUtils.GetQueryValue<string?>(new Uri(outerUrl).Query, "password", null);
            var indent = UrlUtils.GetQueryValue<bool>(new Uri(outerUrl).Query, "indent");
            //create
            return new FilesystemXml(filesystem, "/", isReadonly, init, autoFlush, gzip, password, indent);
        }

    }

}
