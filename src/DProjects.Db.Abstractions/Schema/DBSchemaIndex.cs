using DProjects.Utils;

namespace DProjects.Db.Schema {

    public class DBSchemaIndex {


        //variables


        //constructor
        public DBSchemaIndex() {
        }


        //properties
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool Unique { get; set; } = false;
        public string[] Columns { get; set; } = [];

        //methods
        public string GetHash() {
            return HashUtils.ToHashMD5Base64((Name + Unique + string.Join(",", Columns)).ToLower());
        }
    }


}
