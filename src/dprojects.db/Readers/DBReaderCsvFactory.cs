
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Readers {

    [Protocol("csv", "")]
    [ProtocolExample("csv:", "")]
    [ProtocolExample("csv:?delimiter=;&lineTerminator=&quoteChar='&doubleQuote=false&header=true&skipInitialRows=10&skipInitialSpace=true&nullSequence=\\\\N&comment=#&ignoreComments=true&ignoreEmptyLines=false&InferDataTypes=false&escapeChar=", "")]
    public class DBReaderCsvFactory : IFactoryByUrlAndArgument<IDBReader, TextReader> {

        public IDBReader Create(string src, TextReader reader) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBReaderCsv.Settings>(src);
            return new DBReaderCsv(reader, leaveOpen, settings);
        }
    }

}
