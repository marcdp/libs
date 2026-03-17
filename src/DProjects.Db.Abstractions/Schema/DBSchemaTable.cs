using System.Collections.Generic;
using System.Xml;

namespace DProjects.Db.Schema {

    public class DBSchemaTable {


        //vars


        //constructor
        public DBSchemaTable() {
        }


        //properties
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public DBSchemaColumns Columns { get; set; } = new DBSchemaColumns();
        public DBSchemaPrimaryKey? PrimaryKey { get; set; }
        public DBSchemaForeignKeys ForeignKeys { get; set; } = new DBSchemaForeignKeys();
        public DBSchemaIndexes Indexes { get; set; } = new DBSchemaIndexes();
        public DBSchemaRecords Records { get; set; } = new DBSchemaRecords();


        //methods
        public DBSchemaColumn? GetColumn(string name) {
            foreach (var aux in Columns) {
                if (aux.Name.Equals(name, System.StringComparison.CurrentCultureIgnoreCase)) return aux;
            }
            return null;
        }
        public DBSchemaColumn[] GetPrimaryKeyColumns() {
            var result = new List<DBSchemaColumn>();
            if (PrimaryKey != null) {
                foreach (var aux in PrimaryKey.Columns) {
                    var aux2 = GetColumn(aux);
                    if (aux2 != null) result.Add(aux2);
                }
            }
            return result.ToArray();
        }
        public DBSchemaColumn[] GetNonPrimaryKeyColumns() {
            var result = new List<DBSchemaColumn>();
            var pkColumnNames = (PrimaryKey == null ? new string[] { } : PrimaryKey.Columns);
            foreach (var aux in Columns) {
                if (System.Array.IndexOf(pkColumnNames, aux.Name) == -1) {
                    result.Add(aux);
                }
            }
            return result.ToArray();
        }
        public DBSchemaIndex? GetIndex(string name) {
            foreach (var aux in Indexes) {
                if (aux.Name.Equals(name)) return aux;
            }
            return null;
        }
        public DBSchemaForeignKey? GetForeignKey(string name) {
            foreach (var aux in ForeignKeys) {
                if (aux.Name.Equals(name)) return aux;
            }
            return null;
        }
        //public XmlDocument ToXmlDocument() {
        //    var settings = new XmlSerializer.Settings {
        //        Unprefixes = new string[] { "DBSchema" }
        //    };
        //    return XmlSerializer.SerializeToXmlDocument(this, settings);
        //}
        //public static DBSchemaDatabase FromXmlDocument(XmlDocument xmlDocument) {
        //    var settings = new XmlDeserializer.Settings {
        //        TypePrefix = "DBSchema"
        //    };
        //    return XmlDeserializer.Deserialize<DBSchemaDatabase>(xmlDocument, settings);
        //}
    }


}
