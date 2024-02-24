using System;

namespace DProjects.Db.Schema {

    public class DBSchemaColumn {


        //variables


        //constructor
        public DBSchemaColumn() {
        }
        public DBSchemaColumn(string name) {
            Name = name;
        }


        //properties
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool Null { get; set; }
        public string? Default { get; set; } = null;
        public DBSchemaDataType DataType { get; set; }
        public int Size { get; set; }
        public int Precision { get; set; }
        public int Scale { get; set; }
        public bool IsAutoincrement { get; set; }
        public string? Collation { get; set; } = null;


        //methods
        public Type GetNetDataType() {
            return DataType.GetNetDataType();
        }
        public DBSchemaColumn Clone() {
            var result = new DBSchemaColumn(Name);
            result.Description = Description;
            result.Null = Null;
            result.Default = Default;
            result.DataType = DataType;
            result.Size = Size;
            result.Precision = Precision;
            result.Scale = Scale;
            result.IsAutoincrement = IsAutoincrement;
            result.Collation = Collation;
            return result;
        }


    }

}
