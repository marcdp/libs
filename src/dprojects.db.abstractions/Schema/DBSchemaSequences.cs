using System.Collections.Generic;

namespace DProjects.Db.Schema {

    public class DBSchemaSequences : List<DBSchemaSequence> {

        //constructor
        public DBSchemaSequences() {
        }

        //methods
        public DBSchemaSequence? this[string name] {
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
