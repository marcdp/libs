using DProjects.Db.Schema;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Text;


namespace DProjects.Db.SqlServer {

    public class DBConnectionSqlServer : DBConnection {
        
        //constructor
        public DBConnectionSqlServer(string name, string connectionString) : base(name, connectionString, new Microsoft.Data.SqlClient.SqlConnection(connectionString)) {
        }

        // methods
        protected override bool AreSchemaColumnTypesEquivalent(DBSchemaColumn actual, DBSchemaColumn expected) {
            var actualDataType = GetCanonicalSchemaDataType(actual.DataType);
            var expectedDataType = GetCanonicalSchemaDataType(expected.DataType);
            if (actualDataType != expectedDataType) return false;
            if (actualDataType == DBSchemaDataType.Char || actualDataType == DBSchemaDataType.Nchar || actualDataType == DBSchemaDataType.Binary) {
                return GetSqlServerFixedSize(actual.Size) == GetSqlServerFixedSize(expected.Size);
            }
            if (actualDataType == DBSchemaDataType.Varchar || actualDataType == DBSchemaDataType.Nvarchar || actualDataType == DBSchemaDataType.Varbinary) {
                return GetSqlServerVariableSize(actual.Size) == GetSqlServerVariableSize(expected.Size);
            }
            if (actualDataType == DBSchemaDataType.Decimal) {
                return GetSqlServerNumericPrecision(actual.Precision) == GetSqlServerNumericPrecision(expected.Precision) && actual.Scale == expected.Scale;
            }
            return true;
        }

        //DDL 
        #region "DDL table"
        public override string GetSqlQualifierBegin() {
            return "[";
        }
        public override string GetSqlQualifierEnd() {
            return "]";
        }
        public override string[] GetTableNames() {
            var result = new List<string>();
            foreach (var datarow in ExecuteTable("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME <> 'dtproperties' ORDER BY TABLE_NAME").Rows) {
                result.Add(datarow.GetAs<string>("TABLE_NAME"));
            }
            return result.ToArray();
        }
        public override bool ExistsTable(string table) {
            return (ExecuteScalar<int>("SELECT count(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = ?", [table]) > 0);
        }
        public override DBSchemaDataType GetDataTypeFromSqlDataTypeName(string dataTypeName, int length, int precision, int scale) {
            if (dataTypeName.Equals("float", StringComparison.OrdinalIgnoreCase)) dataTypeName = precision <= 24 ? DBSchemaDataType.Float.ToString() : DBSchemaDataType.Double.ToString();
            if (dataTypeName.Equals("real", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Float.ToString();
            if (dataTypeName.Equals("bit", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Boolean.ToString();
            if (dataTypeName.Equals("text", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Varchar.ToString();
            if (dataTypeName.Equals("ntext", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Nvarchar.ToString();
            if (dataTypeName.Equals("uniqueidentifier", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.UniqueIdentifier.ToString();
            if (dataTypeName.Equals("datetime", StringComparison.OrdinalIgnoreCase) || dataTypeName.Equals("smalldatetime", StringComparison.OrdinalIgnoreCase)) {
                dataTypeName = DBSchemaDataType.DateTime.ToString();
            }
            if (dataTypeName.Equals("datetime2", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Timestamp.ToString();
            else if (dataTypeName.Equals("timestamp", StringComparison.OrdinalIgnoreCase) || dataTypeName.Equals("rowversion", StringComparison.OrdinalIgnoreCase)) {
                dataTypeName = DBSchemaDataType.Varbinary.ToString();
            }
            if (Enum.TryParse<DBSchemaDataType>(dataTypeName, true, out var portableDataType)) dataTypeName = portableDataType.ToString();
            return base.GetDataTypeFromSqlDataTypeName(dataTypeName, length, precision, scale);
        }
        public override DBSchemaTable GetTableSchema(string table) {
            var dbSchemaTable = new DBSchemaTable();
            dbSchemaTable.Name = table;
            dbSchemaTable.Description = "";
            var dbSchemaColumns = new List<DBSchemaColumn>();
            using (var command = CreateCommand("SELECT * FROM " + GetSqlQualifierBegin() + table + GetSqlQualifierEnd() + " WHERE 1=0"))
            using (var reader = command.ExecuteReader(System.Data.CommandBehavior.SchemaOnly)) {
                var schemaTable = reader.GetSchemaTable();
                foreach (System.Data.DataRow? row in schemaTable.Rows) {
                    if (row == null) continue;
                    var dbSchemaColumn = new DBSchemaColumn((string)row["ColumnName"]);
                    int index = (int)row["ColumnOrdinal"];
                    dbSchemaColumn.Description = "";
                    dbSchemaColumn.Null = (bool)row["AllowDBNull"];
                    dbSchemaColumn.Size = (int)row["ColumnSize"];
                    dbSchemaColumn.Precision = (Int16)row["NumericPrecision"];
                    dbSchemaColumn.Scale = (Int16)row["NumericScale"];
                    dbSchemaColumn.IsAutoincrement = (bool)row["IsAutoIncrement"];
                    dbSchemaColumn.DataType = GetDataTypeFromSqlDataTypeName((string)row["DataTypeName"], dbSchemaColumn.Size, dbSchemaColumn.Precision, dbSchemaColumn.Scale);
                    if (dbSchemaColumn.DataType != DBSchemaDataType.Decimal && dbSchemaColumn.DataType != DBSchemaDataType.Numeric) {
                        dbSchemaColumn.Precision = 0;
                        dbSchemaColumn.Scale = 0;
                    }
                    if (dbSchemaColumn.DataType != DBSchemaDataType.Varchar && dbSchemaColumn.DataType != DBSchemaDataType.Char && dbSchemaColumn.DataType != DBSchemaDataType.Nvarchar && dbSchemaColumn.DataType != DBSchemaDataType.Nchar && dbSchemaColumn.DataType != DBSchemaDataType.Binary && dbSchemaColumn.DataType != DBSchemaDataType.Varbinary) {
                        dbSchemaColumn.Size = 0;
                    }
                    if (dbSchemaColumn.Size == Int32.MaxValue) dbSchemaColumn.Size = 0;
                    dbSchemaColumns.Add(dbSchemaColumn);
                }
            }
            dbSchemaTable.Columns.AddRange(dbSchemaColumns.ToArray());
            //default value
            var sSql = new StringBuilder();
            sSql.AppendLine("select sys.all_columns.*, sys.default_constraints.definition as default_value");
            sSql.AppendLine("from sys.all_columns ");
            sSql.AppendLine("	left join sys.all_objects on (sys.all_columns.object_id=sys.all_objects.object_id) ");
            sSql.AppendLine("	left join sys.default_constraints on (sys.all_columns.default_object_id=sys.default_constraints.object_id) ");
            sSql.AppendLine("where sys.all_objects.name = ?");
            sSql.AppendLine("order by column_id");
            foreach (var row in ExecuteTable(sSql.ToString(), [table]).Rows) {
                foreach (var dbSchemaColumn in dbSchemaTable.Columns) {
                    if (dbSchemaColumn.Name.Equals(row["name"])) {
                        dbSchemaColumn.Default = row.GetAs<string>("default_value");
                        while (dbSchemaColumn.Default != null && dbSchemaColumn.Default.StartsWith("(") && dbSchemaColumn.Default.EndsWith(")")) {
                            dbSchemaColumn.Default = dbSchemaColumn.Default.Substring(1, dbSchemaColumn.Default.Length - 2);
                            if (dbSchemaColumn.Default.Equals("getdate()")) dbSchemaColumn.Default = "now";
                        }
                    }
                }
            }
            //pk
            var pk = new DBSchemaPrimaryKey();
            var pkFound = false;
            var pkColumnNames = new List<string>();
            sSql = new StringBuilder();
            sSql.AppendLine("SELECT CONSTRAINT_NAME, COLUMN_NAME");
            sSql.AppendLine("FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE");
            sSql.AppendLine("WHERE OBJECTPROPERTY(OBJECT_ID(CONSTRAINT_SCHEMA + '.' + QUOTENAME(CONSTRAINT_NAME)), 'IsPrimaryKey') = 1 AND TABLE_NAME = ?  ");
            sSql.AppendLine("order by ORDINAL_POSITION");
            foreach (var row in ExecuteTable(sSql.ToString(), [table]).Rows) {
                pk.Name = row.Get("CONSTRAINT_NAME", "");
                foreach (var dbSchemaColumn in dbSchemaTable.Columns) {
                    if (dbSchemaColumn.Name.Equals(row["COLUMN_NAME"])) {
                        pkColumnNames.Add(dbSchemaColumn.Name);
                    }
                }
                pkFound = true;
            }
            pk.Columns = pkColumnNames.ToArray();
            if (pkFound) dbSchemaTable.PrimaryKey = pk;
            // rejects unique constraints whose backing indexes cannot be represented as ordinary portable indexes
            var uniqueConstraintMetadata = ExecuteTable(@"
                SELECT key_constraint.name AS constraint_name, index_definition.name AS index_name
                FROM sys.key_constraints key_constraint
                INNER JOIN sys.tables table_definition ON table_definition.object_id = key_constraint.parent_object_id
                INNER JOIN sys.indexes index_definition ON index_definition.object_id = key_constraint.parent_object_id
                    AND index_definition.index_id = key_constraint.unique_index_id
                WHERE table_definition.name = ? AND key_constraint.type = 'UQ'
                ORDER BY key_constraint.name", [table]);
            ValidateConstraintOwnedUniqueIndexes(table, uniqueConstraintMetadata);
            //indexes 
            PopulateIndexes(dbSchemaTable, ExecuteTable("sys.sp_helpindex @objname = ?", [table]));
            //foreign keys
            var sql = new StringBuilder();
            sql.AppendLine("SELECT C.TABLE_CATALOG [PKTABLE_QUALIFIER], C.TABLE_SCHEMA [PKTABLE_OWNER], C.TABLE_NAME [PKTABLE_NAME], KCU.COLUMN_NAME [PKCOLUMN_NAME], C2.TABLE_CATALOG [FKTABLE_QUALIFIER], C2.TABLE_SCHEMA [FKTABLE_OWNER], C2.TABLE_NAME [FKTABLE_NAME], KCU2.COLUMN_NAME [FKCOLUMN_NAME], RC.UPDATE_RULE, RC.DELETE_RULE, C.CONSTRAINT_NAME [FK_NAME], C2.CONSTRAINT_NAME [PK_NAME], CAST(7 AS SMALLINT) [DEFERRABILITY] ");
            sql.AppendLine("FROM   INFORMATION_SCHEMA.TABLE_CONSTRAINTS C ");
            sql.AppendLine("       INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU ON C.CONSTRAINT_SCHEMA = KCU.CONSTRAINT_SCHEMA AND C.CONSTRAINT_NAME = KCU.CONSTRAINT_NAME ");
            sql.AppendLine("       INNER JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC ON C.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA AND C.CONSTRAINT_NAME = RC.CONSTRAINT_NAME ");
            sql.AppendLine("	   INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS C2 ON RC.UNIQUE_CONSTRAINT_SCHEMA = C2.CONSTRAINT_SCHEMA AND RC.UNIQUE_CONSTRAINT_NAME = C2.CONSTRAINT_NAME ");
            sql.AppendLine("       INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU2 ON C2.CONSTRAINT_SCHEMA = KCU2.CONSTRAINT_SCHEMA AND C2.CONSTRAINT_NAME = KCU2.CONSTRAINT_NAME AND KCU.ORDINAL_POSITION = KCU2.ORDINAL_POSITION ");
            sql.AppendLine("WHERE C.TABLE_NAME=?");
            var fkDbTable = ExecuteTable(sql.ToString(), [table]);
            //var fkDbTable = ExecuteTable("sp_fkeys @fktable_name=?", table);
            var fkNames = new List<string>();
            foreach (var row in fkDbTable.Rows) {
                if (row.Table.Columns[0].Name.Equals("RowsAffected")) {
                    break;
                }
                var fkName = row.Get("FK_NAME", "");
                if (!fkNames.Contains(fkName)) fkNames.Add(fkName);
            }
            var fks = new List<DBSchemaForeignKey>();
            foreach (var fkName in fkNames) {
                //fk
                var fk = new DBSchemaForeignKey();
                fk.Name = fkName;
                fk.Description = "";
                var columns = new List<string>();
                foreach (var fkDbRow in fkDbTable.Rows) {
                    if (fkName.Equals(fkDbRow.Get("FK_NAME", ""))) {
                        foreach (var dbSchemaColumn in dbSchemaTable.Columns) {
                            if (dbSchemaColumn.Name.Equals(fkDbRow.Get("PKCOLUMN_NAME", ""))) {
                                columns.Add(dbSchemaColumn.Name);
                            }
                        }
                    }
                }
                fk.Columns = columns.ToArray();
                foreach (var fkDbRow in fkDbTable.Rows) {
                    if (fkName.Equals(fkDbRow.Get("FK_NAME", ""))) {
                        fk.RefTable = fkDbRow.Get("FKTABLE_NAME", "");
                    }
                }
                var fkColumnsReferenced = new List<string>();
                foreach (var fkDbRow in fkDbTable.Rows) {
                    if (fkName.Equals(fkDbRow.Get("FK_NAME", ""))) {
                        fkColumnsReferenced.Add(fkDbRow.Get("FKCOLUMN_NAME", ""));
                    }
                }
                fk.RefColumns = fkColumnsReferenced.ToArray();
                foreach (var fkDbRow in fkDbTable.Rows) {
                    if (fkName.Equals(fkDbRow.Get("FK_NAME", ""))) {
                        switch (fkDbRow.Get("DELETE_RULE", "")) {
                            case "CASCADE":
                                fk.OnDelete = DBSchemaOnDeleteRule.Cascade; break;
                            case "NO ACTION":
                                fk.OnDelete = DBSchemaOnDeleteRule.NoAction; break;
                            case "SET NULL":
                                fk.OnDelete = DBSchemaOnDeleteRule.SetNull; break;
                            case "SET DEFAULT":
                                fk.OnDelete = DBSchemaOnDeleteRule.SetDefault; break;
                        }
                        switch (fkDbRow.Get("UPDATE_RULE", "NO ACTION")) {
                            case "CASCADE":
                                fk.OnUpdate = DBSchemaOnUpdateRule.Cascade; break;
                            case "NO ACTION":
                                fk.OnUpdate = DBSchemaOnUpdateRule.NoAction; break;
                            case "SET NULL":
                                fk.OnUpdate = DBSchemaOnUpdateRule.SetNull; break;
                            case "SET DEFAULT":
                                fk.OnUpdate = DBSchemaOnUpdateRule.SetDefault; break;
                        }
                    }
                }
                fks.Add(fk);
            }
            dbSchemaTable.ForeignKeys.AddRange(fks.ToArray());
            //return
            return dbSchemaTable;
        }
        public override string GetSqlDropDefault(string table, string column) {
            var aux = new StringBuilder();
            aux.AppendLine("SELECT SchemaName = s.Name,");
            aux.AppendLine("	TableName = t.Name,");
            aux.AppendLine("    ColumnName = c.Name,");
            aux.AppendLine("    DefaultName = dc.Name,");
            aux.AppendLine("    DefaultDefinition = dc.Definition");
            aux.AppendLine("FROM sys.schemas                s");
            aux.AppendLine("    JOIN sys.tables                 t   on  t.schema_id          = s.schema_id");
            aux.AppendLine("    JOIN sys.default_constraints    dc  on  dc.parent_object_id  = t.object_id ");
            aux.AppendLine("    JOIN sys.columns                c   on  c.object_id          = dc.parent_object_id");
            aux.AppendLine("    and c.column_id          = dc.parent_column_id");
            aux.AppendLine("WHERE t.Name = ? AND c.Name = ? ");
            aux.AppendLine("ORDER BY s.Name, t.Name, c.name");
            var sql = new StringBuilder();
            foreach (var row in ExecuteTable(aux.ToString(), [table, column]).Rows) {
                if (sql.Length > 0) sql.Append(";");
                sql.Append("ALTER TABLE " + GetSqlQualifierBegin() + table + GetSqlQualifierEnd() + " DROP CONSTRAINT " + row.Get("DefaultName", ""));
            }
            return sql.ToString();
        }
        public override string GetSqlDropIndex(string table, string index) {
            return "DROP INDEX " + GetSqlQualifierBegin() + table + GetSqlQualifierEnd() + "." + index;
        }
        public override string GetSqlAlterColumn(string table, DBSchemaColumn dBSchemaColumn) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            var sql = new StringBuilder();
            sql.Append("ALTER TABLE " + qb + table + qe + " ALTER COLUMN " + qb + dBSchemaColumn.Name + qe);
            var typeDefinition = GetSqlTypeDefinition(dBSchemaColumn.DataType, dBSchemaColumn.Size, dBSchemaColumn.Precision, dBSchemaColumn.Scale);
            sql.Append(" ").Append(typeDefinition);
            sql.Append(dBSchemaColumn.Null ? " NULL " : " NOT NULL ");
            return sql.ToString();
        }
        public override string GetSqlCreateDefault(string table, string column, string aDefault) {
            var sql = new StringBuilder();
            sql.Append("ALTER TABLE " + GetSqlQualifierBegin() + table + GetSqlQualifierEnd() + " ADD CONSTRAINT DF_" + table + "_" + column);
            if ("now".Equals(aDefault, StringComparison.OrdinalIgnoreCase)) {
                sql.Append(" DEFAULT ").Append(GetSqlDefaultNowExpression());
            } else if (aDefault.Length > 0) {
                sql.Append(" DEFAULT ").Append(aDefault);
            }
            sql.Append(" FOR " + column);
            return sql.ToString();
        }
        public override string GetSqlCreateTempTable(string table, string select) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            return select.Replace(" FROM ", " INTO " + table + " FROM");
        }
        public override string GetSqlDropTempTable(string table) {
            return GetSqlDropTable(table);
        }
        public override string[] GetViewNames() {
            var result = new List<string>();
            foreach (var dbRow in ExecuteTable("SELECT name FROM sys.views ORDER BY name").Rows) {
                var name = dbRow.Get("name", "");
                result.Add(name);
            }
            return result.ToArray();
        }
        public override bool ExistsView(string name) {
            return ExecuteScalar<int>("SELECT count(*) FROM sys.views WHERE name=?", [name]) > 0;
        }
        public override string GetView(string name) {
            var sql = ExecuteScalar<string>("select definition from sys.objects o join sys.sql_modules m on m.object_id = o.object_id where o.object_id = object_id(?) and o.type = 'V'", [name]);
            if (sql.IndexOf(" AS ") != -1) {
                sql = sql.Substring(sql.IndexOf(" AS ") + 3).Trim();
            } else if (sql.IndexOf(" AS\r") != -1) {
                sql = sql.Substring(sql.IndexOf(" AS\r") + 3).Trim();
            } else if (sql.IndexOf(" AS\n") != -1) {
                sql = sql.Substring(sql.IndexOf(" AS\n") + 3).Trim(); 
            }
            return sql;
        }
        public override DBSchemaView GetViewSchema(string table) {
            var dbSchemaView = new DBSchemaView();
            dbSchemaView.Name = table;
            dbSchemaView.Description = "";
            var dbSchemaColumns = new List<DBSchemaColumn>();
            using (var command = CreateCommand("SELECT * FROM " + GetSqlQualifierBegin() + table + GetSqlQualifierEnd() + " WHERE 1=0"))
            using (var reader = command.ExecuteReader(System.Data.CommandBehavior.SchemaOnly)) {
                var schemaTable = reader.GetSchemaTable();
                foreach (System.Data.DataRow? row in schemaTable.Rows) {
                    if (row == null) continue;
                    var dbSchemaColumn = new DBSchemaColumn((string)row["ColumnName"]);
                    int index = (int)row["ColumnOrdinal"];
                    dbSchemaColumn.Description = "";
                    dbSchemaColumn.Null = (bool)row["AllowDBNull"];
                    dbSchemaColumn.Size = (int)row["ColumnSize"];
                    dbSchemaColumn.Precision = (Int16)row["NumericPrecision"];
                    dbSchemaColumn.Scale = (Int16)row["NumericScale"];
                    dbSchemaColumn.IsAutoincrement = (bool)row["IsAutoIncrement"];
                    dbSchemaColumn.DataType = GetDataTypeFromSqlDataTypeName((string)row["DataTypeName"], dbSchemaColumn.Size, dbSchemaColumn.Precision, dbSchemaColumn.Scale);
                    if (dbSchemaColumn.DataType != DBSchemaDataType.Decimal && dbSchemaColumn.DataType != DBSchemaDataType.Numeric) {
                        dbSchemaColumn.Precision = 0;
                        dbSchemaColumn.Scale = 0;
                    }
                    if (dbSchemaColumn.DataType != DBSchemaDataType.Varchar && dbSchemaColumn.DataType != DBSchemaDataType.Char && dbSchemaColumn.DataType != DBSchemaDataType.Nvarchar && dbSchemaColumn.DataType != DBSchemaDataType.Nchar && dbSchemaColumn.DataType != DBSchemaDataType.Binary && dbSchemaColumn.DataType != DBSchemaDataType.Varbinary) {
                        dbSchemaColumn.Size = 0;
                    }
                    if (dbSchemaColumn.Size == Int32.MaxValue) dbSchemaColumn.Size = 0;
                    dbSchemaColumns.Add(dbSchemaColumn);
                }
            }
            dbSchemaView.Columns.AddRange(dbSchemaColumns.ToArray());
            return dbSchemaView;
        }
        public override string[] GetSequenceNames() {
            var result = new List<string>();
            foreach (var dbRow in ExecuteTable("select name from sys.sequences ").Rows) {
                result.Add(dbRow.Get("name",""));
            }
            return result.ToArray();
        }
        public override DBSchemaSequence GetSequenceSchema(string name) {
            var dbTable = ExecuteTable("select name, start_value, increment from sys.sequences where object_id = object_id(?)", [name]);
            var dbRow = dbTable.Rows[0];
            var dbSchemaSequence = new DBSchemaSequence();
            dbSchemaSequence.Name = name;
            dbSchemaSequence.Description = "";
            dbSchemaSequence.InitValue = dbRow.Get<int>("start_value", 0);
            dbSchemaSequence.IncrementBy = dbRow.Get<int>("increment", 0);
            return dbSchemaSequence;
        }
        public override bool ExistsSequence(string name) {
            return (ExecuteScalar<int>("select count(*) from sys.sequences where object_id = object_id(?)", [name]) > 0);
        }
        public override string GetSqlCreateSequence(DBSchemaSequence dbSchemaSequence) {
            var sql = new StringBuilder();
            sql.AppendLine("CREATE SEQUENCE  " + GetSqlQualifierBegin() + dbSchemaSequence.Name + GetSqlQualifierEnd());
            sql.AppendLine("    START WITH " + dbSchemaSequence.InitValue);
            sql.AppendLine("    INCREMENT BY " + dbSchemaSequence.IncrementBy);
            return sql.ToString();
        }
        public override string GetSqlAlterSequenceIncrement(DBSchemaSequence dbSchemaSequence) {
            var sql = new StringBuilder();
            sql.AppendLine("ALTER SEQUENCE  " + GetSqlQualifierBegin() + dbSchemaSequence.Name + GetSqlQualifierEnd());
            sql.AppendLine("    INCREMENT BY " + dbSchemaSequence.IncrementBy);
            return sql.ToString();
        }
        public override string GetSqlDropSequence(string sequence) {
            return "DROP SEQUENCE " + GetSqlQualifierBegin() + sequence + GetSqlQualifierEnd();
        }
        public override string GetSqlTypeDefinition(DBSchemaDataType dataType, int size, int precision, int scale) {
            return dataType switch {
                DBSchemaDataType.Char => GetSqlServerSizedType("CHAR", size, false),
                DBSchemaDataType.Varchar => GetSqlServerSizedType("VARCHAR", size, true),
                DBSchemaDataType.Nchar => GetSqlServerSizedType("NCHAR", size, false),
                DBSchemaDataType.Nvarchar => GetSqlServerSizedType("NVARCHAR", size, true),
                DBSchemaDataType.Binary => GetSqlServerSizedType("BINARY", size, false),
                DBSchemaDataType.Varbinary => GetSqlServerSizedType("VARBINARY", size, true),
                DBSchemaDataType.Numeric => GetSqlServerNumericType("NUMERIC", precision, scale),
                DBSchemaDataType.Decimal => GetSqlServerNumericType("DECIMAL", precision, scale),
                DBSchemaDataType.Smallint => "SMALLINT",
                DBSchemaDataType.TinyInt => "TINYINT",
                DBSchemaDataType.Int => "INT",
                DBSchemaDataType.Bigint => "BIGINT",
                DBSchemaDataType.Float => "REAL",
                DBSchemaDataType.Real => "FLOAT(53)",
                DBSchemaDataType.Double => "FLOAT(53)",
                DBSchemaDataType.Boolean => "BIT",
                DBSchemaDataType.Date => "DATE",
                DBSchemaDataType.DateTime => "DATETIME",
                DBSchemaDataType.Time => "TIME",
                DBSchemaDataType.Timestamp => "DATETIME2",
                DBSchemaDataType.UniqueIdentifier => "UNIQUEIDENTIFIER",
                _ => throw new NotSupportedException($"Portable SQL data type '{dataType}' is not supported by SQL Server.")
            };
        }
        public override string BackupDb() {
            var databaseName = StringUtils.GetConnectionStringVariable(ConnectionString, "Initial Catalog", "");
            var databaseBackupFolder = ExecuteScalar<string>("DECLARE    @BackupDirectory varchar(1000);EXEC master.dbo.xp_instance_regread N\'HKEY_LOCAL_MACHINE\',N\'Software\\Microsoft\\MSSQLServer\\MSSQLServer\',N\'BackupDirectory\',@BackupDirectory OUTPUT ;select @BackupDirectory"); ;
            if (string.IsNullOrEmpty(databaseName)) databaseName = StringUtils.GetConnectionStringVariable(ConnectionString, "Database", "");
            if (string.IsNullOrEmpty(databaseName)) databaseName = ExecuteScalar<string>("select DB_NAME()");
            string backupfilename = System.IO.Path.Combine(databaseBackupFolder, databaseName + "_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".bak");
            ExecuteNonQuery("BACKUP DATABASE " + databaseName + " TO DISK=\'" + backupfilename + "\'");
            return backupfilename;
        }
        public override void RestoreDb(string filename, string dbname) {
            if (string.IsNullOrEmpty(dbname)) dbname = StringUtils.GetConnectionStringVariable(ConnectionString, "Initial Catalog", "");
            if (string.IsNullOrEmpty(dbname)) dbname = StringUtils.GetConnectionStringVariable(ConnectionString, "Database", "");
            if (string.IsNullOrEmpty(dbname)) dbname = ExecuteScalar<string>("select DB_NAME()");
            string finalfilename = filename;
            this.CommandTimeout = 99999999;
            //restores from backup file format
            mCommandTimeout = 60 * 60;
            ExecuteNonQuery("use master");
            if (!ExistsDb(dbname)) {
                //creates new db from backup
                string dataPath = this.ExecuteScalar<string>("SELECT SERVERPROPERTY(\'instancedefaultdatapath\') ");
                string logPath = this.ExecuteScalar<string>("SELECT SERVERPROPERTY(\'instancedefaultlogpath\') ");
                StringBuilder sSql = new StringBuilder();
                sSql.AppendLine("RESTORE DATABASE " + dbname);
                sSql.AppendLine("  FROM DISK=\'" + finalfilename + "\'");
                sSql.AppendLine("  WITH REPLACE ");
                foreach (DBRow dbRow in ExecuteTable("RESTORE FILELISTONLY FROM DISK=?", [finalfilename]).Rows) {
                    string logicalName = dbRow.Get("LogicalName", "");
                    string physicalName = dbRow.Get("PhysicalName", "");
                    string type = dbRow.Get("Type", "");
                    if (type == "D") {
                        sSql.AppendLine(", MOVE \'" + logicalName + "\' TO \'" + dataPath + dbname + ".mdf\'");
                    } else if (type == "L") {
                        sSql.AppendLine(", MOVE \'" + logicalName + "\' TO \'" + dataPath + dbname + "_log.ldf\'");
                    }
                }
                ExecuteNonQuery(sSql.ToString());
                ExecuteNonQuery("use " + dbname);
            } else {
                //replaces db
                ExecuteNonQuery("ALTER DATABASE [" + dbname + "] SET SINGLE_USER WITH ROLLBACK AFTER 5");
                try {
                    ExecuteNonQuery("RESTORE DATABASE " + dbname + " FROM DISK=\'" + finalfilename + "\' WITH REPLACE");
                    ExecuteNonQuery("use " + dbname);
                } finally {
                    ExecuteNonQuery("ALTER DATABASE [" + dbname + "] SET MULTI_USER");
                }
            }
        }
        public override void CompactDb() {
            string databaseName = StringUtils.GetConnectionStringVariable(ConnectionString, "Initial Catalog", "");
            if (string.IsNullOrEmpty(databaseName)) databaseName = DProjects.Utils.StringUtils.GetConnectionStringVariable(ConnectionString, "Database", "");
            ExecuteNonQuery("use master");
            ExecuteNonQuery("ALTER DATABASE [" + databaseName + "] SET SINGLE_USER WITH ROLLBACK AFTER 5");
            try {
                //compacta indices
                ExecuteNonQuery("DBCC CheckDB(" + databaseName + ", REPAIR_REBUILD )");
                //shrinnk database (fit to size)
                ExecuteNonQuery("DBCC SHRINKDATABASE ( " + databaseName + ", 10, TRUNCATEONLY)");
            } catch (Exception ex) {
                throw (new Exception("Error compacting database connection\'" + Name + "\': " + ex.Message, ex));
            } finally {
                ExecuteNonQuery("ALTER DATABASE [" + databaseName + "] SET MULTI_USER");
                this.ExecuteNonQuery("use " + databaseName);
            }
        }
        public override bool ExistsDb(string dbname) {
            return ExecuteTable("select * from sys.databases where name = ?", [dbname]).Rows.Count > 0;
        }
        public override void CreateDb(string dbname) {
            this.ExecuteNonQuery("CREATE DATABASE " + dbname);
        }
        #endregion


        #region "sql format methods"                         
        public override string GetSqlSelectTop(int number) {
            return " TOP " + number + " ";
        }
        public override bool GetSqlSelectTopAtEnd() {
            return true;
        }
        public override string GetSqlSelectOffsetLimit(long offset, int length) {
            return " OFFSET " + offset + " ROWS FETCH NEXT " + length + " ROW ONLY";
        }
        public override string GetSqlSelectAutoincrement() {
            return "SELECT @@IDENTITY";
        }
        public override string GetSqlDefaultNowExpression() {
            return "getDate()";
        }
        public override string GetSqlIdentityDefinition(Type type) {
            if (type == typeof(Guid)) {
                return " uniqueidentifier NOT NULL DEFAULT newId()";
            } else if (type == typeof(long)) {
                return " BIGINT IDENTITY NOT NULL ";
            } else {
                return " INT IDENTITY NOT NULL ";
            }
        }
        public override string GetSqlEncodedLikeValue(string target) {
            string result = target.Trim();
            result = result.Replace("[", "[[]");
            result = result.Replace("á", "a");
            result = result.Replace("à", "a");
            result = result.Replace("Á", "a");
            result = result.Replace("À", "a");
            result = result.Replace("A", "a");
            result = result.Replace("a", "[aáàÀAÁ]");
            result = result.Replace("é", "e");
            result = result.Replace("è", "e");
            result = result.Replace("È", "e");
            result = result.Replace("É", "e");
            result = result.Replace("E", "e");
            result = result.Replace("e", "[eéèÉÈE]");
            result = result.Replace("í", "i");
            result = result.Replace("ï", "i");
            result = result.Replace("Í", "i");
            result = result.Replace("Ï", "i");
            result = result.Replace("I", "i");
            result = result.Replace("i", "[iíïÌÍI]");
            result = result.Replace("ó", "o");
            result = result.Replace("ò", "o");
            result = result.Replace("Ó", "o");
            result = result.Replace("Ò", "o");
            result = result.Replace("O", "o");
            result = result.Replace("o", "[oóòÒÓO]");
            result = result.Replace("ú", "u");
            result = result.Replace("ü", "u");
            result = result.Replace("Ú", "u");
            result = result.Replace("Ú", "u");
            result = result.Replace("Ü", "u");
            result = result.Replace("u", "[uúüÚÙU]");
            return result.Replace("_", "[_]");
        }
        public override string GetSqlGetNextSequenceValue(string sequenceName) {
            return "SELECT NEXT VALUE FOR " + GetSqlQualifierBegin() + sequenceName + GetSqlQualifierEnd();
        }
        public override string GetSqlIfRowCountThrowError(int rowCount, int errorCode, string errorMessage) {
            var sql = new StringBuilder();
            sql.AppendLine("if @@ROWCOUNT = " + rowCount);
            sql.AppendLine("BEGIN");
            sql.AppendLine("    THROW " + errorCode + ", '" + errorMessage.Replace("'", "''") + "', 0;");
            sql.AppendLine("END;");
            return sql.ToString();
        }
        public override string GetSqlEncodedValue(object? value, Type? type, bool bAllowNull) {
            if (type == typeof(DateTime)) {
                if (value == null || value == System.DBNull.Value || System.Convert.ToDateTime(value) == default) {
                    if (bAllowNull) {
                        return "NULL";
                    } else {
                        return "'0000-00-00 00:00:00'";
                    }
                }
                if (System.Convert.ToDateTime(value) < new DateTime(1754, 1, 1)) {
                    value = new DateTime(1754, 1, 1);
                }
                return "convert(DATETIME,'" + System.Convert.ToDateTime(value).ToString("yyyy-MM-ddTHH:mm:ss.fff") + "',126)";
            } else if (type == typeof(bool)) {
                if (value == null || value == System.DBNull.Value) {
                    if (bAllowNull) {
                        return "NULL";
                    } else {
                        return "0";
                    }
                }
                return System.Convert.ToBoolean(value) ? "1" : "0";
            } else if (type == typeof(string)) {
                if (value == null || value == System.DBNull.Value) {
                    if (bAllowNull) {
                        return "NULL";
                    } else {
                        return "''";
                    }
                }
                string result = ((string)value).Replace("'", "''");
                if (result.IndexOf(char.ConvertFromUtf32(0)) != -1) {
                    string nullText = char.ConvertFromUtf32(0);
                    result = result.Replace(nullText, "' + 0x00 + '");
                }
                if (EncodingUtils.GetStringContainsUnicodeCharsUpperThan(result, 256)) {
                    result = "N'" + result + "'";
                } else {
                    result = "'" + result + "'";
                }
                return result;
            } else {
                return base.GetSqlEncodedValue(value, type, bAllowNull);
            }
        }
        public override string GetSqlTempTablePrefix() {
            return "#";
        }
        protected static void PopulateIndexes(DBSchemaTable table, DBTable metadata) {
            foreach (var row in metadata.Rows) {
                if (row.Table.Columns[0].Name.Equals("RowsAffected")) break;
                var description = row.Get("index_description", "");
                if (description.IndexOf("primary key", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                var index = new DBSchemaIndex() {
                    Name = row.Get("index_name", ""),
                    Description = "",
                    Unique = description.IndexOf("unique", StringComparison.OrdinalIgnoreCase) >= 0
                };
                var columns = new List<string>();
                foreach (var indexKey in row.Get("index_keys", "").Replace(" ", "").Split(',')) {
                    foreach (var column in table.Columns) {
                        if (column.Name.Equals(indexKey)) columns.Add(column.Name);
                    }
                }
                index.Columns = columns.ToArray();
                table.Indexes.Add(index);
            }
        }
        protected static void ValidateConstraintOwnedUniqueIndexes(string table, DBTable metadata) {
            if (metadata.Rows.Count == 0) return;
            var row = metadata.Rows[0];
            throw new NotSupportedException($"SQL Server unique constraint '{row.Get("constraint_name", "")}' on table '{table}' cannot be represented by the portable schema model (backing index '{row.Get("index_name", "")}').");
        }

        // methods (private)
        private static DBSchemaDataType GetCanonicalSchemaDataType(DBSchemaDataType dataType) {
            return dataType switch {
                DBSchemaDataType.Real => DBSchemaDataType.Double,
                DBSchemaDataType.Numeric => DBSchemaDataType.Decimal,
                _ => dataType
            };
        }
        private static int GetSqlServerFixedSize(int size) {
            return size == 0 ? 1 : size;
        }
        private static int GetSqlServerVariableSize(int size) {
            return size == int.MaxValue ? 0 : size;
        }
        private static string GetSqlServerSizedType(string sqlType, int size, bool supportsMax) {
            if (supportsMax && (size == 0 || size == int.MaxValue)) return sqlType + "(MAX)";
            return size > 0 ? sqlType + "(" + size + ")" : sqlType;
        }
        private static string GetSqlServerNumericType(string sqlType, int precision, int scale) {
            if (precision == 0 && scale > 0) throw new ArgumentOutOfRangeException(nameof(precision), precision, "Precision must be greater than zero when scale is specified.");
            return precision > 0 ? sqlType + "(" + precision + "," + scale + ")" : sqlType;
        }
        private static int GetSqlServerNumericPrecision(int precision) {
            return precision == 0 ? 18 : precision;
        }
        #endregion


    }

}
