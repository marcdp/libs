namespace DProjects.Db.Schema {

    public class DBSchemaView {


        //variables


        //constructor
        public DBSchemaView() {
        }


        //properties
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public DBSchemaColumns Columns { get; set; } = new DBSchemaColumns();
        public string Content { get; set; } = "";


        //methods
        public DBSchemaColumn? GetColumn(string name) {
            foreach (var aux in Columns) {
                if (aux.Name.Equals(name)) return aux;
            }
            return null;
        } 

    }


}
