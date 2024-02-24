namespace DProjects.Db.Schema {

    public class DBSchemaProcedureArgument {


        //variables


        //constructor
        public DBSchemaProcedureArgument() {
            Name = "";
        }
        public DBSchemaProcedureArgument(string name) : this() {
            Name = name;
        }


        //properties
        public string Name { get; set; }
        public string Description { get; set; } = "";
        public bool Null { get; set; } = false;
        public string Default { get; set; } = "";
        public string Direction { get; set; } = "";
        public DBSchemaDataType DataType { get; set; } = DBSchemaDataType.Varchar;
        public int Length { get; set; } = 0;
        public int Precision { get; set; } = 0;
        public int Scale { get; set; } = 0;

    }


}
