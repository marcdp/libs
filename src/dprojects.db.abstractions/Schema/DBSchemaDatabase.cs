using System.Xml;

using DProjects.Serialization;
using DProjects.Text.Xml;

namespace DProjects.Db.Schema {

    public class DBSchemaDatabase {


        //constructor
        public DBSchemaDatabase() {
        }


        //properties
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public DBSchemaTables Tables { get; set; } = new DBSchemaTables();
        public DBSchemaViews Views { get; set; } = new DBSchemaViews();
        public DBSchemaProcedures Procedures { get; set; } = new DBSchemaProcedures();
        public DBSchemaSequences Sequences { get; set; } = new DBSchemaSequences();
        public DBSchemaScripts Scripts { get; set; } = new DBSchemaScripts { };


        //methods
        public DBSchemaTable? GetTable(string name) {
            foreach (var aux in Tables) if (aux.Name.Equals(name, System.StringComparison.CurrentCultureIgnoreCase)) return aux;
            return null;
        }
        public DBSchemaView? GetView(string name) {
            foreach (var aux in Views) if (aux.Name.Equals(name, System.StringComparison.CurrentCultureIgnoreCase)) return aux;
            return null;
        }
        public DBSchemaProcedure? GetProcedure(string name) {
            foreach (var aux in Procedures) if (aux.Name.Equals(name, System.StringComparison.CurrentCultureIgnoreCase)) return aux;
            return null;
        }
        public DBSchemaSequence? GetSequence(string name) {
            foreach (var aux in Sequences) if (aux.Name.Equals(name, System.StringComparison.CurrentCultureIgnoreCase)) return aux;
            return null;
        }
        public XmlDocument ToXmlDocument() {
            var settings = new XmlSerializerSettings {
                Unprefixes = new string[] { "DBSchema" }
            };
            var serializer = new XmlSerializer(settings);
            return serializer.SerializeToXmlDocument(this);
        }
        public static DBSchemaDatabase FromXmlDocument(string xml) {
            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml.Trim());
            return FromXmlDocument(xmlDocument);
        }
        public static DBSchemaDatabase FromXmlDocument(XmlDocument xmlDocument) {
            var settings = new XmlDeserializerSettings {
                TypePrefix = "DBSchema"
            };
            var deserializer = new XmlDeserializer(settings);
            return deserializer.Deserialize<DBSchemaDatabase>(xmlDocument);
        }

    }


}
