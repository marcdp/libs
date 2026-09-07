namespace DProjects.Db.Schema {

    public class DBSchemaFunction {

        // ctor
        public DBSchemaFunction() {
        }

        // props
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public DBSchemaFunctionArgument[] Arguments { get; set; } = new DBSchemaFunctionArgument[] { };
        public string Content { get; set; } = "";
    }
}
