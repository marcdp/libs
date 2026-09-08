using DProjects.Db.Schema;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;


namespace DProjects.Db.Postgresql {

    public class DBConnectionPostgresql : DBConnection {
        
        //constructor
        public DBConnectionPostgresql(string name, string connectionString) : base(name, connectionString, new Npgsql.NpgsqlConnection(connectionString)) {
        }

        // methods
        public override string GetSqlParameterPrefix() {
            return "$";
        }

        // methods (private)
        protected override string GetSqlParameterName(int index) {
            return string.Empty;
        }
        protected override string GetSqlParameterPlaceholder(int index) {
            return "$" + (index + 1);
        }
        protected override bool AreSchemaColumnTypesEquivalent(DBSchemaColumn actual, DBSchemaColumn expected) {
            var actualDataType = GetCanonicalSchemaDataType(actual.DataType);
            var expectedDataType = GetCanonicalSchemaDataType(expected.DataType);
            if (actualDataType != expectedDataType) return false;
            if (actualDataType == DBSchemaDataType.Varbinary) return true;
            if (actualDataType == DBSchemaDataType.Char) return GetPostgresqlCharSize(actual.Size) == GetPostgresqlCharSize(expected.Size);
            if (actualDataType == DBSchemaDataType.Varchar) return actual.Size == expected.Size;
            if (actualDataType == DBSchemaDataType.Numeric) return actual.Precision == expected.Precision && actual.Scale == expected.Scale;
            return true;
        }

        ////DDL 
        #region "DDL table"
        public override string GetSqlQualifierBegin() {
            return "\"";
        }
        public override string GetSqlQualifierEnd() {
            return "\"";
        }
        public override string[] GetTableNames() {
            var result = new List<string>();
            foreach (var datarow in ExecuteTable("SELECT table_name, table_schema FROM information_schema.tables WHERE table_schema='public'").Rows) {
                result.Add(datarow.GetAs<string>("table_name"));
            }
            return result.ToArray();
        }
        public override bool ExistsTable(string table) {
            return GetTableNames().Contains<string>(table);
        }
        public override DBSchemaDataType GetDataTypeFromSqlDataTypeName(string dataTypeName, int length, int precision, int scale) {
            if (dataTypeName.Equals("float", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Double.ToString();
            if (dataTypeName.Equals("real", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Float.ToString();
            if (dataTypeName.Equals("double precision", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Double.ToString();
            if (dataTypeName.Equals("text", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Varchar.ToString();
            if (dataTypeName.Equals("character", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Char.ToString();
            if (dataTypeName.Equals("character varying", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Varchar.ToString();
            if (dataTypeName.Equals("bpchar", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Char.ToString();
            if (dataTypeName.Equals("bytea", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Varbinary.ToString();
            if (dataTypeName.Equals("timestamp", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.DateTime.ToString();
            if (dataTypeName.Equals("timestamp without time zone", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.DateTime.ToString();
            if (dataTypeName.Equals("time without time zone", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Time.ToString();
            if (dataTypeName.Equals("json", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Json.ToString();
            if (dataTypeName.Equals("jsonb", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Jsonb.ToString();
            if (dataTypeName.Equals("integer", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Int.ToString();
            if (dataTypeName.Equals("int2", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Smallint.ToString();
            if (dataTypeName.Equals("int4", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Int.ToString();
            if (dataTypeName.Equals("int8", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Bigint.ToString();
            if (dataTypeName.Equals("float4", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Float.ToString();
            if (dataTypeName.Equals("float8", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Double.ToString();
            if (dataTypeName.Equals("uuid", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.UniqueIdentifier.ToString();
            if (Enum.TryParse<DBSchemaDataType>(dataTypeName, true, out var portableDataType)) dataTypeName = portableDataType.ToString();
            return base.GetDataTypeFromSqlDataTypeName(dataTypeName, length, precision, scale);
        }
        public override DBSchemaTable GetTableSchema(string table) {
            var schema = "public";
            var dbSchemaTable = new DBSchemaTable();
            dbSchemaTable.Name = table;
            dbSchemaTable.Description = "";
            var dbSchemaColumns = new List<DBSchemaColumn>();
            using (var command = CreateCommand("SELECT * FROM " + GetSqlQualifierBegin() + table + GetSqlQualifierEnd() + " WHERE 1=0"))
            using (var reader = command.ExecuteReader(System.Data.CommandBehavior.KeyInfo)) {
                var schemaTable = reader.GetSchemaTable();
                if (schemaTable != null) {
                    foreach (System.Data.DataRow? row in schemaTable.Rows) {
                        if (row == null) continue;
                        var dbSchemaColumn = new DBSchemaColumn((string)row["ColumnName"]);
                        int index = (int)row["ColumnOrdinal"];
                        dbSchemaColumn.Description = "";
                        dbSchemaColumn.Null = !row.IsNull("AllowDBNull") ? (bool)row["AllowDBNull"] : false;
                        dbSchemaColumn.Size = (int)row["ColumnSize"];
                        dbSchemaColumn.Precision = (Int32)row["NumericPrecision"];
                        dbSchemaColumn.Scale = (Int32)row["NumericScale"];
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
            }
            dbSchemaTable.Columns.AddRange(dbSchemaColumns.ToArray());
            //default value
            foreach (var row in ExecuteTable("SELECT column_name, column_default FROM information_schema.columns WHERE (table_schema, table_name) = (?, ?) ORDER BY ordinal_position", [schema, table]).Rows) {
                foreach (var dbSchemaColumn in dbSchemaTable.Columns) {
                    if (dbSchemaColumn.Name.Equals(row["column_name"])) {
                        dbSchemaColumn.Default = row.GetAs<string>("column_default");
                        if (dbSchemaColumn.Default != null) {
                            if (dbSchemaColumn.Default.Equals("CURRENT_TIMESTAMP")) {
                                dbSchemaColumn.Default = "now";
                            } else if (dbSchemaColumn.Default.StartsWith("'")) {
                                dbSchemaColumn.Default = dbSchemaColumn.Default.Split("::")[0].Trim('\'');
                            }
                        }
                    }
                }
            }
            //pk
            var pk = new DBSchemaPrimaryKey();
            pk.Name = ExecuteScalar<string>("SELECT constraint_name FROM information_schema.table_constraints WHERE table_schema = ? AND table_name = ? AND constraint_type = 'PRIMARY KEY'", [schema, table]);
            var pkFound = false;
            var pkColumnNames = new List<string>();
            var sql = @"
                SELECT kcu.column_name
                FROM information_schema.table_constraints tc
                INNER JOIN information_schema.key_column_usage kcu ON tc.constraint_name = kcu.constraint_name AND tc.constraint_schema = kcu.constraint_schema
                WHERE tc.table_schema = ? AND tc.table_name = ? AND tc.constraint_type = 'PRIMARY KEY'
                ORDER BY kcu.ordinal_position;
            ";
            var sSql = new StringBuilder();
            foreach (var row in ExecuteTable(sql, [schema, table]).Rows) {
                foreach (var dbSchemaColumn in dbSchemaTable.Columns) {
                    if (dbSchemaColumn.Name.Equals(row["column_name"])) {
                        pkColumnNames.Add(dbSchemaColumn.Name);
                    }
                }
                pkFound = true;
            }
            pk.Columns = pkColumnNames.ToArray();
            if (pkFound) dbSchemaTable.PrimaryKey = pk;
            ////indexes 
            //var indexes = new List<DBSchemaIndex>();
            //foreach (var row in ExecuteTable("sys.sp_helpindex @objname = ?", [table]).Rows) {
            //    if (row.Table.Columns[0].Name.Equals("RowsAffected")) {
            //        break;
            //    }
            //    var index = new DBSchemaIndex();
            //    index.Name = row.Get("index_name", "");
            //    index.Description = "";
            //    index.Unique = (row.Get("index_description", "").IndexOf("unique") != -1);
            //    var index_column_names = new List<string>();
            //    foreach (var index_key in row.Get("index_keys", "").Replace(" ", "").Split(',')) {
            //        foreach (var dbSchemaColumn in dbSchemaTable.Columns) {
            //            if (dbSchemaColumn.Name.Equals(index_key)) {
            //                index_column_names.Add(dbSchemaColumn.Name);
            //            }
            //        }
            //    }
            //    index.Columns = index_column_names.ToArray();
            //    indexes.Add(index);
            //}
            //dbSchemaTable.Indexes.AddRange(indexes.ToArray());
            //foreign keys
            //var sql = new StringBuilder();
            //sql.AppendLine("SELECT C.TABLE_CATALOG [PKTABLE_QUALIFIER], C.TABLE_SCHEMA [PKTABLE_OWNER], C.TABLE_NAME [PKTABLE_NAME], KCU.COLUMN_NAME [PKCOLUMN_NAME], C2.TABLE_CATALOG [FKTABLE_QUALIFIER], C2.TABLE_SCHEMA [FKTABLE_OWNER], C2.TABLE_NAME [FKTABLE_NAME], KCU2.COLUMN_NAME [FKCOLUMN_NAME], RC.UPDATE_RULE, RC.DELETE_RULE, C.CONSTRAINT_NAME [FK_NAME], C2.CONSTRAINT_NAME [PK_NAME], CAST(7 AS SMALLINT) [DEFERRABILITY] ");
            //sql.AppendLine("FROM   INFORMATION_SCHEMA.TABLE_CONSTRAINTS C ");
            //sql.AppendLine("       INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU ON C.CONSTRAINT_SCHEMA = KCU.CONSTRAINT_SCHEMA AND C.CONSTRAINT_NAME = KCU.CONSTRAINT_NAME ");
            //sql.AppendLine("       INNER JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC ON C.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA AND C.CONSTRAINT_NAME = RC.CONSTRAINT_NAME ");
            //sql.AppendLine("	   INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS C2 ON RC.UNIQUE_CONSTRAINT_SCHEMA = C2.CONSTRAINT_SCHEMA AND RC.UNIQUE_CONSTRAINT_NAME = C2.CONSTRAINT_NAME ");
            //sql.AppendLine("       INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU2 ON C2.CONSTRAINT_SCHEMA = KCU2.CONSTRAINT_SCHEMA AND C2.CONSTRAINT_NAME = KCU2.CONSTRAINT_NAME AND KCU.ORDINAL_POSITION = KCU2.ORDINAL_POSITION ");
            //sql.AppendLine("WHERE C.TABLE_NAME=?");
            //var fkDbTable = ExecuteTable(sql.ToString(), [table]);
            //var fkDbTable = ExecuteTable("sp_fkeys @fktable_name=?", table);
            //var fkNames = new List<string>();
            //foreach (var row in fkDbTable.Rows) {
            //    if (row.Table.Columns[0].Name.Equals("RowsAffected")) {
            //        break;
            //    }
            //    var fkName = row.Get("FK_NAME", "");
            //    if (!fkNames.Contains(fkName)) fkNames.Add(fkName);
            //}
            //var fks = new List<DBSchemaForeignKey>();
            //foreach (var fkName in fkNames) {
            //    //fk
            //    var fk = new DBSchemaForeignKey();
            //    fk.Name = fkName;
            //    fk.Description = "";
            //    var columns = new List<string>();
            //    foreach (var fkDbRow in fkDbTable.Rows) {
            //        if (fkName.Equals(fkDbRow.Get("FK_NAME", ""))) {
            //            foreach (var dbSchemaColumn in dbSchemaTable.Columns) {
            //                if (dbSchemaColumn.Name.Equals(fkDbRow.Get("PKCOLUMN_NAME", ""))) {
            //                    columns.Add(dbSchemaColumn.Name);
            //                }
            //            }
            //        }
            //    }
            //    fk.Columns = columns.ToArray();
            //    foreach (var fkDbRow in fkDbTable.Rows) {
            //        if (fkName.Equals(fkDbRow.Get("FK_NAME", ""))) {
            //            fk.RefTable = fkDbRow.Get("FKTABLE_NAME", "");
            //        }
            //    }
            //    var fkColumnsReferenced = new List<string>();
            //    foreach (var fkDbRow in fkDbTable.Rows) {
            //        if (fkName.Equals(fkDbRow.Get("FK_NAME", ""))) {
            //            fkColumnsReferenced.Add(fkDbRow.Get("FKCOLUMN_NAME", ""));
            //        }
            //    }
            //    fk.RefColumns = fkColumnsReferenced.ToArray();
            //    foreach (var fkDbRow in fkDbTable.Rows) {
            //        if (fkName.Equals(fkDbRow.Get("FK_NAME", ""))) {
            //            switch (fkDbRow.Get("DELETE_RULE", "")) {
            //                case "CASCADE":
            //                    fk.OnDelete = DBSchemaOnDeleteRule.Cascade; break;
            //                case "NO ACTION":
            //                    fk.OnDelete = DBSchemaOnDeleteRule.NoAction; break;
            //                case "SET NULL":
            //                    fk.OnDelete = DBSchemaOnDeleteRule.SetNull; break;
            //                case "SET DEFAULT":
            //                    fk.OnDelete = DBSchemaOnDeleteRule.SetDefault; break;
            //            }
            //            switch (fkDbRow.Get("UPDATE_RULE", "NO ACTION")) {
            //                case "CASCADE":
            //                    fk.OnUpdate = DBSchemaOnUpdateRule.Cascade; break;
            //                case "NO ACTION":
            //                    fk.OnUpdate = DBSchemaOnUpdateRule.NoAction; break;
            //                case "SET NULL":
            //                    fk.OnUpdate = DBSchemaOnUpdateRule.SetNull; break;
            //                case "SET DEFAULT":
            //                    fk.OnUpdate = DBSchemaOnUpdateRule.SetDefault; break;
            //            }
            //        }
            //    }
            //    fks.Add(fk);
            //}
            //dbSchemaTable.ForeignKeys.AddRange(fks.ToArray());
            // rejects indexes whose semantics cannot be represented by the portable schema model
            var unsupportedIndexMetadata = ExecuteTable(@"
                SELECT index_class.relname AS index_name, access_method.amname AS access_method,
                    index_definition.indexprs IS NOT NULL AS has_expressions,
                    index_definition.indpred IS NOT NULL AS is_partial,
                    index_definition.indnkeyatts <> index_definition.indnatts AS has_include_columns,
                    index_definition.indisexclusion AS is_exclusion,
                    NOT index_definition.indisvalid OR NOT index_definition.indisready OR NOT index_definition.indislive AS is_invalid,
                    COALESCE((to_jsonb(index_definition) ->> 'indnullsnotdistinct')::boolean, false) AS nulls_not_distinct,
                    EXISTS (SELECT 1 FROM unnest(index_definition.indoption::smallint[]) AS option(value) WHERE option.value <> 0) AS has_nondefault_ordering,
                    EXISTS (
                        SELECT 1 FROM unnest(index_definition.indclass::oid[]) AS operator_class(oid)
                        INNER JOIN pg_catalog.pg_opclass operator_class_definition ON operator_class_definition.oid = operator_class.oid
                        WHERE NOT operator_class_definition.opcdefault) AS has_nondefault_operator_class,
                    EXISTS (
                        SELECT 1
                        FROM unnest(index_definition.indcollation::oid[]) WITH ORDINALITY AS index_collation(oid, ordinality)
                        INNER JOIN unnest(index_definition.indkey::smallint[]) WITH ORDINALITY AS indexed_key(attnum, ordinality)
                            ON indexed_key.ordinality = index_collation.ordinality
                        INNER JOIN pg_catalog.pg_attribute indexed_attribute
                            ON indexed_attribute.attrelid = table_class.oid AND indexed_attribute.attnum = indexed_key.attnum
                        WHERE index_collation.oid <> 0 AND index_collation.oid <> indexed_attribute.attcollation) AS has_nondefault_collation
                FROM pg_catalog.pg_class table_class
                INNER JOIN pg_catalog.pg_namespace namespace ON namespace.oid = table_class.relnamespace
                INNER JOIN pg_catalog.pg_index index_definition ON index_definition.indrelid = table_class.oid
                INNER JOIN pg_catalog.pg_class index_class ON index_class.oid = index_definition.indexrelid
                INNER JOIN pg_catalog.pg_am access_method ON access_method.oid = index_class.relam
                WHERE namespace.nspname = ? AND table_class.relname = ? AND NOT index_definition.indisprimary
                    AND (access_method.amname <> 'btree' OR index_definition.indexprs IS NOT NULL OR index_definition.indpred IS NOT NULL
                        OR index_definition.indnkeyatts <> index_definition.indnatts
                        OR index_definition.indisexclusion OR NOT index_definition.indisvalid OR NOT index_definition.indisready OR NOT index_definition.indislive
                        OR COALESCE((to_jsonb(index_definition) ->> 'indnullsnotdistinct')::boolean, false)
                        OR EXISTS (SELECT 1 FROM unnest(index_definition.indoption::smallint[]) AS option(value) WHERE option.value <> 0)
                        OR EXISTS (
                            SELECT 1 FROM unnest(index_definition.indclass::oid[]) AS operator_class(oid)
                            INNER JOIN pg_catalog.pg_opclass operator_class_definition ON operator_class_definition.oid = operator_class.oid
                            WHERE NOT operator_class_definition.opcdefault)
                        OR EXISTS (
                            SELECT 1
                            FROM unnest(index_definition.indcollation::oid[]) WITH ORDINALITY AS index_collation(oid, ordinality)
                            INNER JOIN unnest(index_definition.indkey::smallint[]) WITH ORDINALITY AS indexed_key(attnum, ordinality)
                                ON indexed_key.ordinality = index_collation.ordinality
                            INNER JOIN pg_catalog.pg_attribute indexed_attribute
                                ON indexed_attribute.attrelid = table_class.oid AND indexed_attribute.attnum = indexed_key.attnum
                            WHERE index_collation.oid <> 0 AND index_collation.oid <> indexed_attribute.attcollation))
                ORDER BY index_class.relname", [schema, table]);
            ValidateSupportedIndexes(table, unsupportedIndexMetadata);
            // discovers representable indexes without duplicating primary-key backing indexes
            var indexMetadata = ExecuteTable(@"
                SELECT index_class.relname AS index_name, index_definition.indisunique AS is_unique, attribute.attname AS column_name,
                    key_column.ordinality AS ordinal_position
                FROM pg_catalog.pg_class table_class
                INNER JOIN pg_catalog.pg_namespace namespace ON namespace.oid = table_class.relnamespace
                INNER JOIN pg_catalog.pg_index index_definition ON index_definition.indrelid = table_class.oid
                INNER JOIN pg_catalog.pg_class index_class ON index_class.oid = index_definition.indexrelid
                INNER JOIN pg_catalog.pg_am access_method ON access_method.oid = index_class.relam AND access_method.amname = 'btree'
                CROSS JOIN LATERAL unnest(index_definition.indkey) WITH ORDINALITY AS key_column(attnum, ordinality)
                INNER JOIN pg_catalog.pg_attribute attribute ON attribute.attrelid = table_class.oid AND attribute.attnum = key_column.attnum
                WHERE namespace.nspname = ? AND table_class.relname = ? AND NOT index_definition.indisprimary
                    AND index_definition.indisvalid AND index_definition.indisready AND index_definition.indislive
                    AND NOT COALESCE((to_jsonb(index_definition) ->> 'indnullsnotdistinct')::boolean, false)
                    AND index_definition.indexprs IS NULL AND index_definition.indpred IS NULL
                    AND index_definition.indnkeyatts = index_definition.indnatts AND key_column.attnum > 0
                    AND NOT EXISTS (SELECT 1 FROM unnest(index_definition.indoption::smallint[]) AS option(value) WHERE option.value <> 0)
                    AND NOT EXISTS (
                        SELECT 1 FROM unnest(index_definition.indclass::oid[]) AS operator_class(oid)
                        INNER JOIN pg_catalog.pg_opclass operator_class_definition ON operator_class_definition.oid = operator_class.oid
                        WHERE NOT operator_class_definition.opcdefault)
                    AND NOT EXISTS (
                        SELECT 1
                        FROM unnest(index_definition.indcollation::oid[]) WITH ORDINALITY AS index_collation(oid, ordinality)
                        INNER JOIN unnest(index_definition.indkey::smallint[]) WITH ORDINALITY AS indexed_key(attnum, ordinality)
                            ON indexed_key.ordinality = index_collation.ordinality
                        INNER JOIN pg_catalog.pg_attribute indexed_attribute
                            ON indexed_attribute.attrelid = table_class.oid AND indexed_attribute.attnum = indexed_key.attnum
                        WHERE index_collation.oid <> 0 AND index_collation.oid <> indexed_attribute.attcollation)
                ORDER BY index_class.relname, key_column.ordinality", [schema, table]);
            PopulateIndexes(dbSchemaTable, indexMetadata);
            // rejects foreign keys whose semantics cannot be represented by the portable schema model
            var unsupportedForeignKeyMetadata = ExecuteTable(@"
                SELECT constraint_definition.conname AS constraint_name, referenced_namespace.nspname AS referenced_schema,
                    constraint_definition.confmatchtype::text AS match_type, constraint_definition.condeferrable AS is_deferrable,
                    constraint_definition.condeferred AS is_initially_deferred, constraint_definition.confdeltype::text AS delete_action,
                    constraint_definition.confupdtype::text AS update_action,
                    (to_jsonb(constraint_definition) ->> 'confdelsetcols') IS NOT NULL AS has_delete_column_list
                FROM pg_catalog.pg_constraint constraint_definition
                INNER JOIN pg_catalog.pg_class local_table ON local_table.oid = constraint_definition.conrelid
                INNER JOIN pg_catalog.pg_namespace namespace ON namespace.oid = local_table.relnamespace
                INNER JOIN pg_catalog.pg_class referenced_table ON referenced_table.oid = constraint_definition.confrelid
                INNER JOIN pg_catalog.pg_namespace referenced_namespace ON referenced_namespace.oid = referenced_table.relnamespace
                WHERE constraint_definition.contype = 'f' AND namespace.nspname = ? AND local_table.relname = ?
                    AND (referenced_namespace.nspname <> namespace.nspname OR constraint_definition.confmatchtype <> 's'
                        OR constraint_definition.condeferrable OR constraint_definition.condeferred
                        OR constraint_definition.confdeltype = 'r' OR constraint_definition.confupdtype = 'r'
                        OR (to_jsonb(constraint_definition) ->> 'confdelsetcols') IS NOT NULL)
                ORDER BY constraint_definition.conname", [schema, table]);
            ValidateSupportedForeignKeys(table, schema, unsupportedForeignKeyMetadata);
            // discovers foreign keys with local and referenced columns paired by ordinal position
            var foreignKeyMetadata = ExecuteTable(@"
                SELECT constraint_definition.conname AS constraint_name, local_attribute.attname AS local_column,
                    referenced_table.relname AS referenced_table, referenced_attribute.attname AS referenced_column,
                    local_key.ordinality AS ordinal_position, constraint_definition.confdeltype AS delete_action,
                    constraint_definition.confupdtype AS update_action
                FROM pg_catalog.pg_constraint constraint_definition
                INNER JOIN pg_catalog.pg_class local_table ON local_table.oid = constraint_definition.conrelid
                INNER JOIN pg_catalog.pg_namespace namespace ON namespace.oid = local_table.relnamespace
                INNER JOIN pg_catalog.pg_class referenced_table ON referenced_table.oid = constraint_definition.confrelid
                INNER JOIN pg_catalog.pg_namespace referenced_namespace
                    ON referenced_namespace.oid = referenced_table.relnamespace AND referenced_namespace.nspname = namespace.nspname
                CROSS JOIN LATERAL unnest(constraint_definition.conkey) WITH ORDINALITY AS local_key(attnum, ordinality)
                INNER JOIN LATERAL unnest(constraint_definition.confkey) WITH ORDINALITY AS referenced_key(attnum, ordinality)
                    ON referenced_key.ordinality = local_key.ordinality
                INNER JOIN pg_catalog.pg_attribute local_attribute ON local_attribute.attrelid = local_table.oid AND local_attribute.attnum = local_key.attnum
                INNER JOIN pg_catalog.pg_attribute referenced_attribute
                    ON referenced_attribute.attrelid = referenced_table.oid AND referenced_attribute.attnum = referenced_key.attnum
                WHERE constraint_definition.contype = 'f' AND namespace.nspname = ? AND local_table.relname = ?
                ORDER BY constraint_definition.conname, local_key.ordinality", [schema, table]);
            PopulateForeignKeys(dbSchemaTable, foreignKeyMetadata);
            //return
            return dbSchemaTable;
        }
        public override string GetSqlAlterColumn(string table, DBSchemaColumn dBSchemaColumn) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            var sql = new StringBuilder();
            var qualifiedTable = qb + table + qe;
            var qualifiedColumn = qb + dBSchemaColumn.Name + qe;
            // changes the type and nullability with PostgreSQL's separate alter-column forms
            sql.Append("ALTER TABLE ").Append(qualifiedTable).Append(" ALTER COLUMN ").Append(qualifiedColumn).Append(" TYPE ");
            sql.Append(GetSqlTypeDefinition(dBSchemaColumn.DataType, dBSchemaColumn.Size, dBSchemaColumn.Precision, dBSchemaColumn.Scale));
            sql.Append(GetSqlSeparator()).Append(Environment.NewLine);
            sql.Append("ALTER TABLE ").Append(qualifiedTable).Append(" ALTER COLUMN ").Append(qualifiedColumn);
            sql.Append(dBSchemaColumn.Null ? " DROP NOT NULL" : " SET NOT NULL");
            return sql.ToString();
        }
        public override string GetSqlDropDefault(string table, string column) {
            var qualifiedTable = GetSqlQualifierBegin() + table + GetSqlQualifierEnd();
            var qualifiedColumn = GetSqlQualifierBegin() + column + GetSqlQualifierEnd();
            return "ALTER TABLE " + qualifiedTable + " ALTER COLUMN " + qualifiedColumn + " DROP DEFAULT";
        }
        public override string GetSqlDropIndex(string table, string index) {
            return "DROP INDEX " + GetSqlQualifierBegin() + index + GetSqlQualifierEnd();
        }
        public override string GetSqlCreateDefault(string table, string column, string aDefault) {
            var value = "now".Equals(aDefault, StringComparison.OrdinalIgnoreCase) ? GetSqlDefaultNowExpression() : aDefault;
            var qualifiedTable = GetSqlQualifierBegin() + table + GetSqlQualifierEnd();
            var qualifiedColumn = GetSqlQualifierBegin() + column + GetSqlQualifierEnd();
            return "ALTER TABLE " + qualifiedTable + " ALTER COLUMN " + qualifiedColumn + " SET DEFAULT " + value;
        }
        //public override string GetSqlDropDefault(string table, string column) {
        //    var aux = new StringBuilder();
        //    aux.AppendLine("SELECT SchemaName = s.Name,");
        //    aux.AppendLine("	TableName = t.Name,");
        //    aux.AppendLine("    ColumnName = c.Name,");
        //    aux.AppendLine("    DefaultName = dc.Name,");
        //    aux.AppendLine("    DefaultDefinition = dc.Definition");
        //    aux.AppendLine("FROM sys.schemas                s");
        //    aux.AppendLine("    JOIN sys.tables                 t   on  t.schema_id          = s.schema_id");
        //    aux.AppendLine("    JOIN sys.default_constraints    dc  on  dc.parent_object_id  = t.object_id ");
        //    aux.AppendLine("    JOIN sys.columns                c   on  c.object_id          = dc.parent_object_id");
        //    aux.AppendLine("    and c.column_id          = dc.parent_column_id");
        //    aux.AppendLine("WHERE t.Name = ? AND c.Name = ? ");
        //    aux.AppendLine("ORDER BY s.Name, t.Name, c.name");
        //    var sql = new StringBuilder();
        //    foreach (var row in ExecuteTable(aux.ToString(), [table, column]).Rows) {
        //        if (sql.Length > 0) sql.Append(";");
        //        sql.Append("ALTER TABLE " + GetSqlQualifierBegin() + table + GetSqlQualifierEnd() + " DROP CONSTRAINT " + row.Get("DefaultName", ""));
        //    }
        //    return sql.ToString();
        //}
        //public override string GetSqlCreateTempTable(string table, string select) {
        //    var qb = GetSqlQualifierBegin();
        //    var qe = GetSqlQualifierEnd();
        //    return select.Replace(" FROM ", " INTO " + table + " FROM");
        //}
        //public override string GetSqlDropTempTable(string table) {
        //    return GetSqlDropTable(table);
        //}
        public override string[] GetViewNames() {
            var result = new List<string>();
            var schema = "public";
            foreach (var dbRow in ExecuteTable("SELECT table_name FROM information_schema.views WHERE table_schema = ?", [schema]).Rows) {
                var name = dbRow.Get("table_name", "");
                result.Add(name);
            }
            return result.ToArray();
        }
        //public override bool ExistsView(string name) {
        //    return ExecuteScalar<int>("SELECT count(*) FROM sys.views WHERE name=?", [name]) > 0;
        //}
        //public override string GetView(string name) {
        //    var sql = ExecuteScalar<string>("select definition from sys.objects o join sys.sql_modules m on m.object_id = o.object_id where o.object_id = object_id(?) and o.type = 'V'", [name]);
        //    if (sql.IndexOf(" AS ") != -1) {
        //        sql = sql.Substring(sql.IndexOf(" AS ") + 3).Trim();
        //    } else if (sql.IndexOf(" AS\r") != -1) {
        //        sql = sql.Substring(sql.IndexOf(" AS\r") + 3).Trim();
        //    } else if (sql.IndexOf(" AS\n") != -1) {
        //        sql = sql.Substring(sql.IndexOf(" AS\n") + 3).Trim(); 
        //    }
        //    return sql;
        //}
        //public override DBSchemaView GetViewSchema(string table) {
        //    var dbSchemaView = new DBSchemaView();
        //    dbSchemaView.Name = table;
        //    dbSchemaView.Description = "";
        //    var dbSchemaColumns = new List<DBSchemaColumn>();
        //    using (var command = CreateCommand("SELECT * FROM " + GetSqlQualifierBegin() + table + GetSqlQualifierEnd() + " WHERE 1=0"))
        //    using (var reader = command.ExecuteReader(System.Data.CommandBehavior.SchemaOnly)) {
        //        var schemaTable = reader.GetSchemaTable();
        //        foreach (System.Data.DataRow? row in schemaTable.Rows) {
        //            if (row == null) continue;
        //            var dbSchemaColumn = new DBSchemaColumn((string)row["ColumnName"]);
        //            int index = (int)row["ColumnOrdinal"];
        //            dbSchemaColumn.Description = "";
        //            dbSchemaColumn.Null = (bool)row["AllowDBNull"];
        //            dbSchemaColumn.Size = (int)row["ColumnSize"];
        //            dbSchemaColumn.Precision = (Int16)row["NumericPrecision"];
        //            dbSchemaColumn.Scale = (Int16)row["NumericScale"];
        //            dbSchemaColumn.IsAutoincrement = (bool)row["IsAutoIncrement"];
        //            dbSchemaColumn.DataType = GetDataTypeFromSqlDataTypeName((string)row["DataTypeName"], dbSchemaColumn.Size, dbSchemaColumn.Precision, dbSchemaColumn.Scale);
        //            if (dbSchemaColumn.DataType != DBSchemaDataType.Decimal && dbSchemaColumn.DataType != DBSchemaDataType.Numeric) {
        //                dbSchemaColumn.Precision = 0;
        //                dbSchemaColumn.Scale = 0;
        //            }
        //            if (dbSchemaColumn.DataType != DBSchemaDataType.Varchar && dbSchemaColumn.DataType != DBSchemaDataType.Char && dbSchemaColumn.DataType != DBSchemaDataType.Nvarchar && dbSchemaColumn.DataType != DBSchemaDataType.Nchar && dbSchemaColumn.DataType != DBSchemaDataType.Binary && dbSchemaColumn.DataType != DBSchemaDataType.Varbinary) {
        //                dbSchemaColumn.Size = 0;
        //            }
        //            if (dbSchemaColumn.Size == Int32.MaxValue) dbSchemaColumn.Size = 0;
        //            dbSchemaColumns.Add(dbSchemaColumn);
        //        }
        //    }
        //    dbSchemaView.Columns.AddRange(dbSchemaColumns.ToArray());
        //    return dbSchemaView;
        //}
        public override string GetSqlCreateSequence(DBSchemaSequence dbSchemaSequence) {
            var sql = new StringBuilder();
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            sql.AppendLine("CREATE SEQUENCE  " + qb + dbSchemaSequence.Name + qe);
            sql.AppendLine("    START " + dbSchemaSequence.InitValue);
            sql.AppendLine("    INCREMENT BY " + dbSchemaSequence.IncrementBy);
            return sql.ToString();
        }
        public override string GetSqlAlterSequenceIncrement(DBSchemaSequence dbSchemaSequence) {
            var qualifiedSequence = GetSqlQualifierBegin() + dbSchemaSequence.Name + GetSqlQualifierEnd();
            return "ALTER SEQUENCE " + qualifiedSequence + " INCREMENT BY " + dbSchemaSequence.IncrementBy;
        }
        public override string GetSqlDropSequence(string sequence) {
            return "DROP SEQUENCE " + GetSqlQualifierBegin() + sequence + GetSqlQualifierEnd();
        }
        public override string[] GetSequenceNames() {
            var schema = "public";
            var result = new List<string>();
            foreach (var dbRow in ExecuteTable("select sequence_schema, sequence_name from information_schema.sequences WHERE sequence_schema=? ", [schema]).Rows) {
                result.Add(dbRow.Get("sequence_name", ""));
            }
            return result.ToArray();
        }
        public override DBSchemaSequence GetSequenceSchema(string name) {
            var schema = "public";
            var dbTable = ExecuteTable("select sequence_schema, sequence_name, start_value, increment from information_schema.sequences WHERE sequence_schema=? AND sequence_name=?", [schema, name]);
            var dbRow = dbTable.Rows[0];
            var dbSchemaSequence = new DBSchemaSequence();
            dbSchemaSequence.Name = name;
            dbSchemaSequence.Description = "";
            dbSchemaSequence.InitValue = dbRow.Get<int>("start_value", 1);
            dbSchemaSequence.IncrementBy = dbRow.Get<int>("increment", 1);
            return dbSchemaSequence;
        }
        public override bool ExistsSequence(string name) {
            return System.Array.IndexOf(GetSequenceNames(), name) != -1;
        }
        //public override string[] GetProcedureNames() {
        //    return new string[] { };
        //}
        public override string GetSqlTypeDefinition(DBSchemaDataType dataType, int size, int precision, int scale) {
            // translates every supported portable type explicitly into PostgreSQL SQL
            return dataType switch {
                DBSchemaDataType.Char => GetSqlCharacterTypeDefinition("CHAR", size),
                DBSchemaDataType.Varchar => GetSqlVariableCharacterTypeDefinition(size),
                DBSchemaDataType.Nchar => GetSqlCharacterTypeDefinition("CHAR", size),
                DBSchemaDataType.Nvarchar => GetSqlVariableCharacterTypeDefinition(size),
                DBSchemaDataType.Binary => "BYTEA",
                DBSchemaDataType.Varbinary => "BYTEA",
                DBSchemaDataType.Numeric => GetSqlNumericTypeDefinition("NUMERIC", precision, scale),
                DBSchemaDataType.Decimal => GetSqlNumericTypeDefinition("DECIMAL", precision, scale),
                DBSchemaDataType.Smallint => "SMALLINT",
                DBSchemaDataType.TinyInt => "SMALLINT",
                DBSchemaDataType.Int => "INTEGER",
                DBSchemaDataType.Bigint => "BIGINT",
                DBSchemaDataType.Float => "REAL",
                DBSchemaDataType.Real => "DOUBLE PRECISION",
                DBSchemaDataType.Double => "DOUBLE PRECISION",
                DBSchemaDataType.Boolean => "BOOLEAN",
                DBSchemaDataType.Date => "DATE",
                DBSchemaDataType.DateTime => "TIMESTAMP WITHOUT TIME ZONE",
                DBSchemaDataType.Time => "TIME",
                DBSchemaDataType.Timestamp => "TIMESTAMP WITHOUT TIME ZONE",
                DBSchemaDataType.Interval => "INTERVAL",
                DBSchemaDataType.UniqueIdentifier => "UUID",
                DBSchemaDataType.Json => "JSON",
                DBSchemaDataType.Jsonb => "JSONB",
                _ => throw new NotSupportedException($"Portable data type '{dataType}' is not supported by PostgreSQL.")
            };
        }

        //public override string BackupDb() {
        //    var databaseName = StringUtils.GetConnectionStringVariable(ConnectionString, "Initial Catalog", "");
        //    var databaseBackupFolder = ExecuteScalar<string>("DECLARE    @BackupDirectory varchar(1000);EXEC master.dbo.xp_instance_regread N\'HKEY_LOCAL_MACHINE\',N\'Software\\Microsoft\\MSSQLServer\\MSSQLServer\',N\'BackupDirectory\',@BackupDirectory OUTPUT ;select @BackupDirectory"); ;
        //    if (string.IsNullOrEmpty(databaseName)) databaseName = StringUtils.GetConnectionStringVariable(ConnectionString, "Database", "");
        //    if (string.IsNullOrEmpty(databaseName)) databaseName = ExecuteScalar<string>("select DB_NAME()");
        //    string backupfilename = System.IO.Path.Combine(databaseBackupFolder, databaseName + "_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".bak");
        //    ExecuteNonQuery("BACKUP DATABASE " + databaseName + " TO DISK=\'" + backupfilename + "\'");
        //    return backupfilename;
        //}
        //public override void RestoreDb(string filename, string dbname) {
        //    if (string.IsNullOrEmpty(dbname)) dbname = StringUtils.GetConnectionStringVariable(ConnectionString, "Initial Catalog", "");
        //    if (string.IsNullOrEmpty(dbname)) dbname = StringUtils.GetConnectionStringVariable(ConnectionString, "Database", "");
        //    if (string.IsNullOrEmpty(dbname)) dbname = ExecuteScalar<string>("select DB_NAME()");
        //    string finalfilename = filename;
        //    this.CommandTimeout = 99999999;
        //    //restores from backup file format
        //    mCommandTimeout = 60 * 60;
        //    ExecuteNonQuery("use master");
        //    if (!ExistsDb(dbname)) {
        //        //creates new db from backup
        //        string dataPath = this.ExecuteScalar<string>("SELECT SERVERPROPERTY(\'instancedefaultdatapath\') ");
        //        string logPath = this.ExecuteScalar<string>("SELECT SERVERPROPERTY(\'instancedefaultlogpath\') ");
        //        StringBuilder sSql = new StringBuilder();
        //        sSql.AppendLine("RESTORE DATABASE " + dbname);
        //        sSql.AppendLine("  FROM DISK=\'" + finalfilename + "\'");
        //        sSql.AppendLine("  WITH REPLACE ");
        //        foreach (DBRow dbRow in ExecuteTable("RESTORE FILELISTONLY FROM DISK=?", [finalfilename]).Rows) {
        //            string logicalName = dbRow.Get("LogicalName", "");
        //            string physicalName = dbRow.Get("PhysicalName", "");
        //            string type = dbRow.Get("Type", "");
        //            if (type == "D") {
        //                sSql.AppendLine(", MOVE \'" + logicalName + "\' TO \'" + dataPath + dbname + ".mdf\'");
        //            } else if (type == "L") {
        //                sSql.AppendLine(", MOVE \'" + logicalName + "\' TO \'" + dataPath + dbname + "_log.ldf\'");
        //            }
        //        }
        //        ExecuteNonQuery(sSql.ToString());
        //        ExecuteNonQuery("use " + dbname);
        //    } else {
        //        //replaces db
        //        ExecuteNonQuery("ALTER DATABASE [" + dbname + "] SET SINGLE_USER WITH ROLLBACK AFTER 5");
        //        try {
        //            ExecuteNonQuery("RESTORE DATABASE " + dbname + " FROM DISK=\'" + finalfilename + "\' WITH REPLACE");
        //            ExecuteNonQuery("use " + dbname);
        //        } finally {
        //            ExecuteNonQuery("ALTER DATABASE [" + dbname + "] SET MULTI_USER");
        //        }
        //    }
        //}
        //public override void CompactDb() {
        //    string databaseName = StringUtils.GetConnectionStringVariable(ConnectionString, "Initial Catalog", "");
        //    if (string.IsNullOrEmpty(databaseName)) databaseName = DProjects.Utils.StringUtils.GetConnectionStringVariable(ConnectionString, "Database", "");
        //    ExecuteNonQuery("use master");
        //    ExecuteNonQuery("ALTER DATABASE [" + databaseName + "] SET SINGLE_USER WITH ROLLBACK AFTER 5");
        //    try {
        //        //compacta indices
        //        ExecuteNonQuery("DBCC CheckDB(" + databaseName + ", REPAIR_REBUILD )");
        //        //shrinnk database (fit to size)
        //        ExecuteNonQuery("DBCC SHRINKDATABASE ( " + databaseName + ", 10, TRUNCATEONLY)");
        //    } catch (Exception ex) {
        //        throw (new Exception("Error compacting database connection\'" + Name + "\': " + ex.Message, ex));
        //    } finally {
        //        ExecuteNonQuery("ALTER DATABASE [" + databaseName + "] SET MULTI_USER");
        //        this.ExecuteNonQuery("use " + databaseName);
        //    }
        //}
        //public override bool ExistsDb(string dbname) {
        //    return ExecuteTable("select * from sys.databases where name = ?", [dbname]).Rows.Count > 0;
        //}
        //public override void CreateDb(string dbname) {
        //    this.ExecuteNonQuery("CREATE DATABASE " + dbname);
        //}
        //#endregion


        //#region "sql format methods"                         
        public override string GetSqlSelectTop(int number) {
            return " LIMIT " + number;
        }
        public override bool GetSqlSelectTopAtEnd() {
            return true;
        }
        public override string GetSqlSelectOffsetLimit(long offset, int length) {
            return " LIMIT " + length + " OFFSET " + offset;
        }
        public override string GetSqlDefaultNowExpression() {
            return "CURRENT_TIMESTAMP";
        }
        public override string GetSqlGetNextSequenceValue(string sequenceName) {
            return $"SELECT nextval('{sequenceName}')";
        }
        //public override string GetSqlEncodedValue(object? value, Type? type, bool bAllowNull) {
        //    if (type == typeof(DateTime)) {
        //        if (value == null || value == System.DBNull.Value || System.Convert.ToDateTime(value) == default) {
        //            if (bAllowNull) {
        //                return "NULL";
        //            } else {
        //                return "'0000-00-00 00:00:00'";
        //            }
        //        }
        //        if (System.Convert.ToDateTime(value) < new DateTime(1754, 1, 1)) {
        //            value = new DateTime(1754, 1, 1);
        //        }
        //        return "convert(DATETIME,'" + System.Convert.ToDateTime(value).ToString("yyyy-MM-ddTHH:mm:ss.fff") + "',126)";
        //    } else if (type == typeof(bool)) {
        //        if (value == null || value == System.DBNull.Value) {
        //            if (bAllowNull) {
        //                return "NULL";
        //            } else {
        //                return "0";
        //            }
        //        }
        //        return System.Convert.ToBoolean(value) ? "1" : "0";
        //    } else if (type == typeof(string)) {
        //        if (value == null || value == System.DBNull.Value) {
        //            if (bAllowNull) {
        //                return "NULL";
        //            } else {
        //                return "''";
        //            }
        //        }
        //        string result = ((string)value).Replace("'", "''");
        //        if (result.IndexOf(char.ConvertFromUtf32(0)) != -1) {
        //            string nullText = char.ConvertFromUtf32(0);
        //            result = result.Replace(nullText, "' + 0x00 + '");
        //        }
        //        if (EncodingUtils.GetStringContainsUnicodeCharsUpperThan(result, 256)) {
        //            result = "N'" + result + "'";
        //        } else {
        //            result = "'" + result + "'";
        //        }
        //        return result;
        //    } else {
        //        return base.GetSqlEncodedValue(value, type, bAllowNull);
        //    }
        //}
        //public override string GetSqlTempTablePrefix() {
        //    return "#";
        //}
        #endregion

        // methods (private)
        private static string GetSqlCharacterTypeDefinition(string sqlType, int size) {
            return size > 0 ? sqlType + "(" + size + ")" : sqlType;
        }
        private static string GetSqlVariableCharacterTypeDefinition(int size) {
            return size > 0 ? "VARCHAR(" + size + ")" : "TEXT";
        }
        private static string GetSqlNumericTypeDefinition(string sqlType, int precision, int scale) {
            if (precision == 0 && scale > 0) {
                throw new ArgumentOutOfRangeException(nameof(precision), precision, "Precision must be greater than zero when scale is specified.");
            }
            return precision > 0 ? sqlType + "(" + precision + "," + scale + ")" : sqlType;
        }
        private static int GetPostgresqlCharSize(int size) {
            return size == 0 ? 1 : size;
        }
        private static DBSchemaDataType GetCanonicalSchemaDataType(DBSchemaDataType dataType) {
            // canonicalizes only portable types that PostgreSQL persists with indistinguishable storage semantics
            return dataType switch {
                DBSchemaDataType.Timestamp => DBSchemaDataType.DateTime,
                DBSchemaDataType.Nchar => DBSchemaDataType.Char,
                DBSchemaDataType.Nvarchar => DBSchemaDataType.Varchar,
                DBSchemaDataType.Binary => DBSchemaDataType.Varbinary,
                DBSchemaDataType.TinyInt => DBSchemaDataType.Smallint,
                DBSchemaDataType.Real => DBSchemaDataType.Double,
                DBSchemaDataType.Decimal => DBSchemaDataType.Numeric,
                _ => dataType
            };
        }
        protected static void PopulateIndexes(DBSchemaTable table, DBTable metadata) {
            DBSchemaIndex? index = null;
            var columns = new List<string>();
            foreach (var row in metadata.Rows) {
                var name = row.Get("index_name", "");
                if (index == null || !index.Name.Equals(name, StringComparison.Ordinal)) {
                    if (index != null) {
                        index.Columns = columns.ToArray();
                        table.Indexes.Add(index);
                    }
                    index = new DBSchemaIndex() { Name = name, Unique = row.Get("is_unique", false) };
                    columns = new List<string>();
                }
                columns.Add(row.Get("column_name", ""));
            }
            if (index != null) {
                index.Columns = columns.ToArray();
                table.Indexes.Add(index);
            }
        }
        protected static void PopulateForeignKeys(DBSchemaTable table, DBTable metadata) {
            DBSchemaForeignKey? foreignKey = null;
            var columns = new List<string>();
            var referencedColumns = new List<string>();
            foreach (var row in metadata.Rows) {
                var name = row.Get("constraint_name", "");
                if (foreignKey == null || !foreignKey.Name.Equals(name, StringComparison.Ordinal)) {
                    if (foreignKey != null) AddForeignKey(table, foreignKey, columns, referencedColumns);
                    foreignKey = new DBSchemaForeignKey() {
                        Name = name,
                        RefTable = row.Get("referenced_table", ""),
                        OnDelete = GetOnDeleteRule(row.Get("delete_action", "a")),
                        OnUpdate = GetOnUpdateRule(row.Get("update_action", "a"))
                    };
                    columns = new List<string>();
                    referencedColumns = new List<string>();
                }
                columns.Add(row.Get("local_column", ""));
                referencedColumns.Add(row.Get("referenced_column", ""));
            }
            if (foreignKey != null) AddForeignKey(table, foreignKey, columns, referencedColumns);
        }
        protected static void ValidateSupportedIndexes(string table, DBTable metadata) {
            if (metadata.Rows.Count == 0) return;
            var row = metadata.Rows[0];
            var reason = row.Get("access_method", "btree") != "btree" ? "non-btree access method"
                : row.Get("has_expressions", false) ? "expression keys"
                : row.Get("is_partial", false) ? "partial predicate"
                : row.Get("has_include_columns", false) ? "INCLUDE columns"
                : row.Get("is_exclusion", false) ? "exclusion constraint semantics"
                : row.Get("is_invalid", false) ? "invalid or unfinished index state"
                : row.Get("nulls_not_distinct", false) ? "NULLS NOT DISTINCT"
                : row.Get("has_nondefault_ordering", false) ? "non-default ordering"
                : row.Get("has_nondefault_operator_class", false) ? "non-default operator class"
                : row.Get("has_nondefault_collation", false) ? "non-default collation"
                : "unsupported definition";
            throw new NotSupportedException($"PostgreSQL index '{row.Get("index_name", "")}' on table '{table}' cannot be represented: {reason}.");
        }
        protected static void ValidateSupportedForeignKeys(string table, string schema, DBTable metadata) {
            if (metadata.Rows.Count == 0) return;
            var row = metadata.Rows[0];
            var reason = !row.Get("referenced_schema", schema).Equals(schema, StringComparison.Ordinal) ? "cross-schema reference"
                : row.Get("match_type", "s") != "s" ? "MATCH mode"
                : row.Get("is_deferrable", false) || row.Get("is_initially_deferred", false) ? "deferrable semantics"
                : row.Get("delete_action", "a") == "r" || row.Get("update_action", "a") == "r" ? "RESTRICT action"
                : row.Get("has_delete_column_list", false) ? "column-specific SET NULL/DEFAULT action"
                : "unsupported semantics";
            throw new NotSupportedException($"PostgreSQL foreign key '{row.Get("constraint_name", "")}' on table '{table}' cannot be represented: {reason}.");
        }
        private static void AddForeignKey(DBSchemaTable table, DBSchemaForeignKey foreignKey, List<string> columns, List<string> referencedColumns) {
            foreignKey.Columns = columns.ToArray();
            foreignKey.RefColumns = referencedColumns.ToArray();
            table.ForeignKeys.Add(foreignKey);
        }
        private static DBSchemaOnDeleteRule GetOnDeleteRule(string action) {
            return action switch {
                "c" => DBSchemaOnDeleteRule.Cascade,
                "n" => DBSchemaOnDeleteRule.SetNull,
                "d" => DBSchemaOnDeleteRule.SetDefault,
                _ => DBSchemaOnDeleteRule.NoAction
            };
        }
        private static DBSchemaOnUpdateRule GetOnUpdateRule(string action) {
            return action switch {
                "c" => DBSchemaOnUpdateRule.Cascade,
                "n" => DBSchemaOnUpdateRule.SetNull,
                "d" => DBSchemaOnUpdateRule.SetDefault,
                _ => DBSchemaOnUpdateRule.NoAction
            };
        }

    }

}
