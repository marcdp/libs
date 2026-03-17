using System.Collections.Generic;

namespace DProjects.Db.Schema {

    public class DBSchemaColumns : List<DBSchemaColumn> {

        //constructor
        public DBSchemaColumns() {
        }

        //methods
        public DBSchemaColumn? this[string name] {
            get {
                if (int.TryParse(name, out int index)) return this[index];
                foreach (var item in this) {
                    if (item.Name.Equals(name)) return item;
                }
                return null;
            }
        }
    }
}
