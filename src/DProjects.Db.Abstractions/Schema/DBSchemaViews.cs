using System.Collections.Generic;

namespace DProjects.Db.Schema {

    public class DBSchemaViews : List<DBSchemaView> {

        //constructor
        public DBSchemaViews() {
        }

        //methods
        public DBSchemaView? this[string name] {
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
