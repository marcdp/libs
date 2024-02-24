
namespace DProjects.Db.Extensions {


    public static class DBTableToDataTable {


        //methods
        public static System.Data.DataTable ToDatatable(this DBTable dbTable) {
            var result = new System.Data.DataTable();
            foreach(var dbColumn in dbTable.Columns) {
                var column = new System.Data.DataColumn ();
                column.ColumnName = dbColumn.Name;
                column.DataType = dbColumn.DBType;
                result.Columns.Add(column);
            }
            foreach(var dbRow in dbTable.Rows) {
                var dataRow = result.NewRow();
                foreach (var dbColumn in dbTable.Columns) {
                    dataRow[dbColumn.Name] = dbRow[dbColumn.Name];
                }
                result.Rows.Add(dataRow);

            }
            return result;
        }

    }


}