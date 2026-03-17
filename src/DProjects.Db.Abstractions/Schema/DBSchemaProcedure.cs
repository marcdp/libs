namespace DProjects.Db.Schema {

    public class DBSchemaProcedure {


        //variables


        //constructor
        public DBSchemaProcedure() {
        }


        //properties
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public DBSchemaProcedureArgument[] Arguments { get; set; } = new DBSchemaProcedureArgument[] { };
        public string Content { get; set; } = "";
    }


}
