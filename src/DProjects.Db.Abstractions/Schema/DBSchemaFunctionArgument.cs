namespace DProjects.Db.Schema {

    public class DBSchemaFunctionArgument {

        // ctor
        public DBSchemaFunctionArgument() {
            Name = "";
        }
        public DBSchemaFunctionArgument(string name) : this() {
            Name = name;
        }

        // props
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
