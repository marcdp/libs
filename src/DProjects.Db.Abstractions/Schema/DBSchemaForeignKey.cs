using DProjects.Utils;

namespace DProjects.Db.Schema {

    public class DBSchemaForeignKey {


        //constructor
        public DBSchemaForeignKey() {
        }


        //properties
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public DBSchemaOnDeleteRule OnDelete { get; set; } = DBSchemaOnDeleteRule.NoAction;
        public DBSchemaOnUpdateRule OnUpdate { get; set; } = DBSchemaOnUpdateRule.NoAction;
        public string[] Columns { get; set; } = [];
        public string RefTable { get; set; } = "";
        public string[] RefColumns { get; set; } = [];

        //methods
        public string GetHash() {
            return HashUtils.ToHashMD5Base64((Name + string.Join(",", Columns) + RefTable + string.Join(",", RefColumns) + OnDelete.ToString() + OnUpdate.ToString()).ToLower());
        }

    }


}
