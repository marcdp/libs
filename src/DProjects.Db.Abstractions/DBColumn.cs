using DProjects.Utils;
using System;
using System.Text.Json.Serialization;

namespace DProjects.Db {


    public class DBColumn {


        //constructor
        public DBColumn() {
            this.Name = "";
            this.Description = "";
            this.Title = "";
            this.DBType = typeof(string);
        }
        public DBColumn(string columnName) {
            this.Name = columnName;
            this.Description = "";
            this.Title = "";
            this.DBType = typeof(string);
        }
        public DBColumn(string columnName, Type dbType) {
            this.Name = columnName;
            this.Description = "";
            this.Title = "";
            this.DBType = dbType;
        }
        public DBColumn(string columnName, Type dbType, DBColumnFormat dbFormat) {
            this.Name = columnName;
            this.Description = "";
            this.Title = "";
            this.DBType = dbType;
            this.Format = dbFormat;
        }
        public DBColumn(DBColumn column) {
            this.Name = column.Name;
            this.Title = column.Title;
            this.Description = column.Description;
            this.DBType = column.DBType;
            this.ReadOnly = column.ReadOnly;
            this.Format = column.Format;
            this.Required = column.Required;
            this.MinLength = column.MinLength;
            this.MaxLength = column.MaxLength;
            this.AutoIncrement = column.AutoIncrement;
            this.DefaultValue = column.DefaultValue;
            this.Unique = column.Unique;
        }


        //properties
        public string Name { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        [JsonIgnore] public Type DBType { get; set; }
        [JsonPropertyName("DBType")]
        public string DBTypeName {
            get {
                return DBType.Name;
            }
            set {
                var type = ConvertUtils.ToSimpleType(value);
                if (type == null) throw new Exception("Unable to set column type: invalid type: " + value);
                DBType = type;
            }
        }
        public bool ReadOnly { get; set; }
        public bool Required { get; set; }
        public int MinLength { get; set; }
        public int MaxLength { get; set; }
        public bool AutoIncrement { get; set; }
        public object? DefaultValue { get; set; }
        public bool Unique { get; set; }
        public DBColumnFormat Format { get; set; }

        //methods
        public DBColumn Clone() {
            return new DBColumn(this);
        }

    }

}
