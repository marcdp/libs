//using DProjects.Db.Readers;
//using DProjects.Db.Writers;
//using System.IO;

//namespace DProjects.Db.Extensions {


//    public static class DBTableToHtml {


//        //methods
//        public static void ToHtml(this DBTable dbTable, TextWriter writer, DBWriterHtml.Settings? settings = null) {
//            if (settings == null) settings = new DBWriterHtml.Settings();
//            using (var dbWriter = new Writers.DBWriterHtml(writer, true, settings)) {
//                dbWriter.Columns.Add(dbTable.Columns);
//                foreach (var dbRow in dbTable.Rows) {
//                    dbWriter.Write(dbRow);
//                }
//            }
//        }
//        public static string ToHtml(this DBTable dbTable, DBWriterHtml.Settings? settings = null) {
//            var sw = new StringWriter();
//            dbTable.ToHtml(sw, settings);
//            return sw.ToString();
//        }

//    }


//}