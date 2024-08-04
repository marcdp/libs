
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("csv", "")]
    [ProtocolExample("csv:", "")]
    [ProtocolExample("csv:?delimiter=;&lineTerminator=&quoteChar='&doubleQuote=false&header=true&skipInitialRows=10&skipInitialSpace=true&nullSequence=\\\\N&comment=#&ignoreComments=true&ignoreEmptyLines=false&InferDataTypes=false&escapeChar=", "")]
    public class DBWriterCsvFactory : IFactoryByUrl<IDBWriter, TextWriter> {

        public IDBWriter Create(string src, TextWriter writer) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterCsv.Settings>(src);
            return new DBWriterCsv(writer, leaveOpen, settings);
        }
    }

}
