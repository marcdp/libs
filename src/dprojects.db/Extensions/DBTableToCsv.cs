using DProjects.Db.Readers;
using DProjects.Db.Writers;
using System.IO;

namespace DProjects.Db.Extensions {


    public static class DBTableToCsv {


        //methods
        public static void ToCsv(this DBTable dbTable, TextWriter writer, DBWriterCsv.Settings? settings = null) {
            if (settings == null) settings = new DBWriterCsv.Settings();
            using (var dbWriter = new DBWriterCsv(writer, true, settings)) {
                dbWriter.Columns.Add(dbTable.Columns);
                foreach (var dbRow in dbTable.Rows) {
                    dbWriter.Write(dbRow);
                }
            }
        }
        public static string ToCsv(this DBTable dbTable, DBWriterCsv.Settings? settings = null) {
            var sw = new StringWriter();
            dbTable.ToCsv(sw, settings);
            return sw.ToString();
        }

    }


}