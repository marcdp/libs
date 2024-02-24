//using DProjects.Db.Readers;
//using DProjects.Db.Writers;
//using System.IO;

//namespace DProjects.Db.Extensions {


//    public static class DBTableToView {


//        //methods
//        public static DBTable ToView(this DBTable dbTable, string[] columns, string where, object[] values, string orderBy) {
//            using (var dbReader = new DBReaderView(dbTable, columns, where, values, orderBy, true)) {
//                return DBTable.FromDBReader(dbReader);
//            }
//        }

//    }


//}