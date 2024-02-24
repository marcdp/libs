using DProjects.Utils;

namespace DProjects.Db.Schema {

    public class DBSchemaPrimaryKey {


        //variables


        //constructor
        public DBSchemaPrimaryKey() {
        }


        //properties
        public string Name { get; set; } = "";
        public string[] Columns { get; set; } = [];

        //methods
        public string GetHash() {
            return HashUtils.ToHashMD5Base64((string.Join(",", Columns)).ToLower());
        }

    }


}
