namespace DProjects.Db {

    public class DBSet {


        //variables
        private string mName;
        private DBTables mTables;


        //constructor
        public DBSet() {
            mTables = new DBTables();
            mName = "";
        }


        //properties
        public string Name => mName;
        public DBTables Tables {
            get {
                return mTables;
            }
        }
        public bool HasChanges {
            get {
                foreach (DBTable dbTable in Tables) {
                    if (dbTable.HasChanges) {
                        return true;
                    }
                }
                return false;
            }
        }

        //methods
        public void AcceptChanges() {
            foreach (DBTable dbTable in Tables) {
                dbTable.AcceptChanges();
            }
        }




    }


}
