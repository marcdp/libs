using System;

namespace DProjects.Db.Schema {

    public enum DBSchemaDataType {
        None,
        Char,
        Varchar,
        Nchar,
        Nvarchar,
        Binary,
        Varbinary,
        Numeric,
        Decimal,
        Smallint,
        TinyInt,
        Int,
        Bigint,
        Float,
        Real,
        Double,
        Boolean,
        Date,
        DateTime,
        Time,
        Timestamp,
        Interval,
        UniqueIdentifier,
        Json
    }
    public static class DBSchemaDataTypeModule {
        public static Type GetNetDataType(this DBSchemaDataType dbSchemaDataType) {
            if (dbSchemaDataType == DBSchemaDataType.Char) {
                return typeof(char);
            } else if (dbSchemaDataType == DBSchemaDataType.Varchar) {
                return typeof(string);
            } else if (dbSchemaDataType == DBSchemaDataType.Nchar) {
                return typeof(string);
            } else if (dbSchemaDataType == DBSchemaDataType.Nvarchar) {
                return typeof(string);
            } else if (dbSchemaDataType == DBSchemaDataType.Binary) {
                return typeof(byte[]);
            } else if (dbSchemaDataType == DBSchemaDataType.Varbinary) {
                return typeof(byte[]);
            } else if (dbSchemaDataType == DBSchemaDataType.Numeric) {
                return typeof(decimal);
            } else if (dbSchemaDataType == DBSchemaDataType.Decimal) {
                return typeof(decimal);
            } else if (dbSchemaDataType == DBSchemaDataType.Smallint) {
                return typeof(short);
            } else if (dbSchemaDataType == DBSchemaDataType.TinyInt ) {
                return typeof(byte);
            } else if (dbSchemaDataType == DBSchemaDataType.Int) {
                return typeof(int);
            } else if (dbSchemaDataType == DBSchemaDataType.Bigint) {
                return typeof(long);
            } else if (dbSchemaDataType == DBSchemaDataType.Float) {
                return typeof(float);
            } else if (dbSchemaDataType == DBSchemaDataType.Real) {
                return typeof(double);
            } else if (dbSchemaDataType == DBSchemaDataType.Double) {
                return typeof(double);
            } else if (dbSchemaDataType == DBSchemaDataType.Boolean) {
                return typeof(bool);
            } else if (dbSchemaDataType == DBSchemaDataType.Date) {
                return typeof(System.DateTime);
            } else if (dbSchemaDataType == DBSchemaDataType.DateTime) {
                return typeof(System.DateTime);
            } else if (dbSchemaDataType == DBSchemaDataType.Time) {
                return typeof(System.TimeSpan);
            } else if (dbSchemaDataType == DBSchemaDataType.Timestamp) {
                return typeof(System.TimeSpan);
            } else if (dbSchemaDataType == DBSchemaDataType.Interval) {
                return typeof(string);
            } else if (dbSchemaDataType == DBSchemaDataType.UniqueIdentifier) {
                return typeof(System.Guid);
            } else if (dbSchemaDataType == DBSchemaDataType.Json) {
                return typeof(System.String);
            }
            throw new NotImplementedException();
        }
        public static System.Data.DbType GetDbType(this DBSchemaDataType dbSchemaDataType) {
            if (dbSchemaDataType == DBSchemaDataType.Char) {
                return System.Data.DbType.String;
            } else if (dbSchemaDataType == DBSchemaDataType.Varchar) {
                return System.Data.DbType.String;
            } else if (dbSchemaDataType == DBSchemaDataType.Nchar) {
                return System.Data.DbType.String;
            } else if (dbSchemaDataType == DBSchemaDataType.Nvarchar) {
                return System.Data.DbType.String;
            } else if (dbSchemaDataType == DBSchemaDataType.Binary) {
                return System.Data.DbType.Binary;
            } else if (dbSchemaDataType == DBSchemaDataType.Varbinary) {
                return System.Data.DbType.Binary;
            } else if (dbSchemaDataType == DBSchemaDataType.Numeric) {
                return System.Data.DbType.VarNumeric;
            } else if (dbSchemaDataType == DBSchemaDataType.Decimal) {
                return System.Data.DbType.Decimal;
            } else if (dbSchemaDataType == DBSchemaDataType.Smallint) {
                return System.Data.DbType.Int16;
            } else if (dbSchemaDataType == DBSchemaDataType.TinyInt) {
                return System.Data.DbType.Byte;
            } else if (dbSchemaDataType == DBSchemaDataType.Int) {
                return System.Data.DbType.Int32;
            } else if (dbSchemaDataType == DBSchemaDataType.Bigint) {
                return System.Data.DbType.Int64;
            } else if (dbSchemaDataType == DBSchemaDataType.Float) {
                return System.Data.DbType.Single;
            } else if (dbSchemaDataType == DBSchemaDataType.Real) {
                return System.Data.DbType.Double;
            } else if (dbSchemaDataType == DBSchemaDataType.Double) {
                return System.Data.DbType.Double;
            } else if (dbSchemaDataType == DBSchemaDataType.Boolean) {
                return System.Data.DbType.Boolean; 
            } else if (dbSchemaDataType == DBSchemaDataType.Date) {
                return System.Data.DbType.Date;
            } else if (dbSchemaDataType == DBSchemaDataType.DateTime) {
                return System.Data.DbType.DateTime;
            } else if (dbSchemaDataType == DBSchemaDataType.Time) {
                return System.Data.DbType.Time;
            } else if (dbSchemaDataType == DBSchemaDataType.Timestamp) {
                return System.Data.DbType.DateTime;
            } else if (dbSchemaDataType == DBSchemaDataType.Interval) {
                return System.Data.DbType.String;
            } else if (dbSchemaDataType == DBSchemaDataType.UniqueIdentifier) {
                return System.Data.DbType.Guid;
            } else if (dbSchemaDataType == DBSchemaDataType.Json) {
                return System.Data.DbType.String;
            }
            throw new NotImplementedException();
        }
    }

}
