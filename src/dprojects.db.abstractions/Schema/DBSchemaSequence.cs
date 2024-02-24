namespace DProjects.Db.Schema {

    public class DBSchemaSequence {


        //variables


        //constructor
        public DBSchemaSequence() {
        }


        //properties
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public long InitValue { get; set; } = 0;
        public long IncrementBy { get; set; } = 1;


    }


}
