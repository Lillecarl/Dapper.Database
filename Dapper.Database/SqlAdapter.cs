﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection.Emit;

using Dapper;
using Dapper.Database;

#if NETSTANDARD1_3
using DataException = System.InvalidOperationException;
#else
using System.Threading;
#endif

#if NET451
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
#endif


/// <summary>
/// The interface for all Dapper.Contrib database operations
/// Implementing this is each provider's model.
/// </summary>
public partial interface ISqlAdapter
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <param name="tableInfo"></param>
    /// <param name="entityToInsert"></param>
    /// <returns></returns>
    bool Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, TableInfo tableInfo, object entityToInsert);

    /// <summary>
    /// Adds the name of a column.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    void AppendColumnName(StringBuilder sb, string columnName);

    /// <summary>
    /// Adds a column equality to a parameter.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    void AppendColumnNameEqualsValue(StringBuilder sb, string columnName);

    /// <summary>
    /// Returns the table name for the database type
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="schema"></param>
    /// <returns></returns>
    string FormatSchemaTable(string tableName, string schema);

    /// <summary>
    /// Returns the sql for testing for existence
    /// </summary>
    /// <returns>sql string</returns>
    string GetExistsSql(string tableName, string whereClause);

    /// <summary>
    /// 
    /// </summary>
    string EscapeTableName { get; }

    /// <summary>
    /// 
    /// </summary>
    string EscapeSqlIdentifier { get; }

    /// <summary>
    /// 
    /// </summary>
    string EscapeSqlAssignment { get; }

    /// <summary>
    /// 
    /// </summary>
    bool SupportsSchemas { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tableInfo"></param>
    /// <param name="whereClause"></param>
    /// <returns></returns>
    string CountQuery(TableInfo tableInfo, string whereClause);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tableInfo"></param>
    /// <param name="whereClause"></param>
    /// <returns></returns>
    string DeleteQuery(TableInfo tableInfo, string whereClause);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tableInfo"></param>
    /// <param name="whereClause"></param>
    /// <returns></returns>
    string ExistsQuery(TableInfo tableInfo, string whereClause);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tableInfo"></param>
    /// <param name="whereClause"></param>
    /// <returns></returns>
    string GetQuery(TableInfo tableInfo, string whereClause);
}

/// <summary>
/// Base class for SqlAdapter handlers - provides default/common handling for different database engines
/// </summary>
public abstract class SqlAdapter
{

    ///// <summary>
    ///// Returns the format for table name
    ///// </summary>
    //public virtual string EscapeTableName => "[{0}]";

    ///// <summary>
    ///// Returns the sql identifier format
    ///// </summary>
    //public virtual string EscapeSqlIdentifier => "[{0}]";

    ///// <summary>
    ///// Returns the escaped assignment format
    ///// </summary>
    //public virtual string EscapeSqlAssignment => "[{0}] = @{1}";

    ///// <summary>
    ///// Returns true if schemas are supported in database
    ///// </summary>
    //public virtual bool SupportsSchemas => true;

    ///// <summary>
    ///// Returns the escaped assignment format
    ///// </summary>
    //public virtual string EscapeParameter => "@{1}";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <param name="tableInfo"></param>
    /// <param name="entityToInsert"></param>
    /// <returns></returns>
    public virtual bool Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, TableInfo tableInfo, object entityToInsert)
    {
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tableInfo"></param>   
    /// <param name="whereClause"></param>
    /// <returns></returns>
    public virtual string CountQuery(TableInfo tableInfo, string whereClause)
    {
        if (string.IsNullOrWhiteSpace(whereClause))
        {
            return $"select count(*) from {EscapeTableNamee(tableInfo)}";
        }
        else
        {
            return $"select count(*) from {EscapeTableNamee(tableInfo)} where {whereClause}";
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tableInfo"></param>
    /// <param name="whereClause"></param>
    /// <returns></returns>
    public virtual string DeleteQuery(TableInfo tableInfo, string whereClause)
    {
        var wc = string.IsNullOrWhiteSpace(whereClause) ? EscapeWhereList(tableInfo.KeyColumns) : whereClause;
        return $"delete from {EscapeTableNamee(tableInfo)} where {wc}";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tableInfo"></param>
    /// <param name="whereClause"></param>
    /// <returns></returns>
    public virtual string ExistsQuery(TableInfo tableInfo, string whereClause)
    {
        var wc = string.IsNullOrWhiteSpace(whereClause) ? EscapeWhereList(tableInfo.KeyColumns) : whereClause;
        return $"select exists (select 1 from {EscapeTableNamee(tableInfo)} where {wc})";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tableInfo"></param>
    /// <param name="whereClause"></param>
    /// <returns></returns>
    public virtual string GetQuery(TableInfo tableInfo, string whereClause)
    {
        var wc = string.IsNullOrWhiteSpace(whereClause) ? EscapeWhereList(tableInfo.KeyColumns) : whereClause;
        return $"select {EscapeColumnList(tableInfo.SelectColumns)} from {EscapeTableNamee(tableInfo)} where {wc}";
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="tableInfo"></param>
    /// <returns></returns>
    public virtual string EscapeTableNamee(TableInfo tableInfo) => EscapeTableNamee(tableInfo.TableName);

    /// <summary>
    /// Returns the format for table name
    /// </summary>
    public virtual string EscapeTableNamee(string value) => $"[{value}]";

    /// <summary>
    /// Returns the format for table name
    /// </summary>
    public virtual string EscapeColumnn(string value) => $"[{value}]";

    /// <summary>
    /// Returns the format for table name
    /// </summary>
    public virtual string EscapeParameter(string value) => $"@{value}";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="columns"></param>
    /// <returns></returns>
    public virtual string EscapeColumnList(IEnumerable<ColumnInfo> columns) => string.Join(",", columns.Select(ci => EscapeColumnn(ci.ColumnName)));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="columns"></param>
    /// <returns></returns>
    public virtual string EscapeWhereList(IEnumerable<ColumnInfo> columns) => string.Join(" and ", columns.Select(ci => $"{EscapeColumnn(ci.ColumnName)} = {EscapeParameter(ci.PropertyName)}"));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="columns"></param>
    /// <returns></returns>
    public virtual string EscapeParameters(IEnumerable<ColumnInfo> columns) => string.Join(",", columns.Select(ci => EscapeParameter(ci.PropertyName)));

}


/// <summary>
/// The SQL Server database adapter.
/// </summary>
public partial class SqlServerAdapter : SqlAdapter, ISqlAdapter
{

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tableInfo"></param>
    /// <returns></returns>
    public override string EscapeTableNamee(TableInfo tableInfo) =>
        !string.IsNullOrEmpty(tableInfo.SchemaName) ? EscapeTableNamee(tableInfo.SchemaName) + "." : null + EscapeTableNamee(tableInfo.TableName);



    //private object EscapeColumnList(Func<IEnumerable<ColumnInfo>> insertColumns)
    //{
    //    throw new NotImplementedException();
    //}


    /// <summary>
    /// 
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="transaction"></param>
    /// <param name="commandTimeout"></param>
    /// <param name="tableInfo"></param>
    /// <param name="entityToInsert"></param>
    /// <returns></returns>
    public override bool Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, TableInfo tableInfo, object entityToInsert)
    {
        var cmd = new StringBuilder($"insert into { EscapeTableNamee(tableInfo)} ({EscapeColumnList(tableInfo.InsertColumns)}) values ({EscapeParameters(tableInfo.InsertColumns)}); ");

        if (tableInfo.GeneratedColumns.Any() && tableInfo.KeyColumns.Any())
        {
            cmd.Append($"select {EscapeColumnList(tableInfo.GeneratedColumns)} from {EscapeTableNamee(tableInfo)} ");

            if (tableInfo.KeyColumns.Any(k => k.IsIdentity))
            {
                cmd.Append($"where {EscapeColumnn(tableInfo.KeyColumns.First(k => k.IsIdentity).ColumnName)} = SCOPE_IDENTITY();");
            }
            else
            {
                cmd.Append($"where {EscapeWhereList(tableInfo.KeyColumns)};");
            }

            var multi = connection.QueryMultiple(cmd.ToString(), entityToInsert, transaction, commandTimeout);

            var vals = multi.Read().ToList();

            if (!vals.Any()) return false;

            var rvals = ((IDictionary<string, object>)vals[0]);

            foreach (var key in rvals.Keys)
            {
                var rval = rvals[key];
                var p = tableInfo.GeneratedColumns.Single(gp => gp.ColumnName == key).Property;
                p.SetValue(entityToInsert, Convert.ChangeType(rval, p.PropertyType), null);
            }

            return true;
        }
        else
        {
            return connection.Execute(cmd.ToString(), entityToInsert, transaction, commandTimeout) > 0;
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tableInfo"></param>
    /// <param name="whereClause"></param>
    /// <returns></returns>
    public override string ExistsQuery(TableInfo tableInfo, string whereClause)
    {
        var wc = string.IsNullOrWhiteSpace(whereClause) ? EscapeWhereList(tableInfo.KeyColumns) : whereClause;
        return $"if exists (select 1 from {EscapeTableNamee(tableInfo)} where {wc}) select 1 else select 0";
    }

    /// <summary>
    /// Adds the name of a column.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnName(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("[{0}]", columnName);
    }

    /// <summary>
    /// Adds a column equality to a parameter.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnNameEqualsValue(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("[{0}] = @{1}", columnName, columnName);
    }

    /// <summary>
    /// Returns the schema and table name
    /// </summary>
    /// <param name="tableName">The table</param>
    /// <param name="schema">The Schema if it was specified</param>
    /// <returns>schema + table</returns>
    public string FormatSchemaTable(string tableName, string schema)
    {
        return string.IsNullOrEmpty(schema) ? $"[{tableName}]" : $"[{schema}].[{ tableName}]";
    }

    /// <summary>
    /// Returns sql existence statement
    /// </summary>
    /// <returns>sql</returns>
    public string GetExistsSql(string tableName, string whereClause) => $"IF EXISTS (SELECT 1 FROM {tableName} WHERE {whereClause}) SELECT 1 ELSE SELECT 0";

    /// <summary>
    /// Returns the format for table name
    /// </summary>
    public string EscapeTableName => "[{0}]";

    /// <summary>
    /// Returns the sql identifier format
    /// </summary>
    public string EscapeSqlIdentifier => "[{0}]";

    /// <summary>
    /// Returns the escaped assignment format
    /// </summary>
    public string EscapeSqlAssignment => "[{0}] = @{1}";

    /// <summary>
    /// Returns true if schemas are supported in database
    /// </summary>
    public bool SupportsSchemas => true;
}

/// <summary>
/// The SQL Server Compact Edition database adapter.
/// </summary>
public partial class SqlCeServerAdapter : SqlAdapter, ISqlAdapter
{
    /// <summary>
    /// Inserts <paramref name="entityToInsert"/> into the database, returning the Id of the row created.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout to use.</param>
    /// <param name="tableName">The table to insert into.</param>
    /// <param name="columnList">The columns to set with this insert.</param>
    /// <param name="parameterList">The parameters to set for this insert.</param>
    /// <param name="keyProperties">The key columns in this table.</param>
    /// <param name="entityToInsert">The entity to insert.</param>
    /// <returns>true if inserted</returns>
    public bool Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, string tableName, string columnList, string parameterList, IEnumerable<ColumnInfo> keyProperties, object entityToInsert)
    {
        var cmd = $"insert into {tableName} ({columnList}) values ({parameterList})";
        connection.Execute(cmd, entityToInsert, transaction, commandTimeout);
        var r = connection.Query("select @@IDENTITY id", transaction: transaction, commandTimeout: commandTimeout).ToList();

        if (r[0].id == null) return false;
        var id = (int)r[0].id;

        var propertyInfos = keyProperties.Select(p => p.Property).ToArray();
        if (propertyInfos.Length == 0) return false;

        var idProperty = propertyInfos[0];
        idProperty.SetValue(entityToInsert, Convert.ChangeType(id, idProperty.PropertyType), null);

        return propertyInfos.Length > 0;
    }

    /// <summary>
    /// Adds the name of a column.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnName(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("[{0}]", columnName);
    }

    /// <summary>
    /// Adds a column equality to a parameter.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnNameEqualsValue(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("[{0}] = @{1}", columnName, columnName);
    }

    /// <summary>
    /// Returns the schema and table name
    /// </summary>
    /// <param name="tableName">The table</param>
    /// <param name="schema">The Schema if it was specified</param>
    /// <returns>schema + table</returns>
    public string FormatSchemaTable(string tableName, string schema)
    {
        return $"[{tableName}]"; //CE doesn't support schema
    }

    /// <summary>
    /// Returns sql existence statement
    /// </summary>
    /// <returns>sql</returns>
    public string GetExistsSql(string tableName, string whereClause) => $"IF EXISTS (SELECT 1 FROM {tableName} WHERE {whereClause}) SELECT 1 ELSE SELECT 0";

    /// <summary>
    /// Returns the format for table name
    /// </summary>
    public string EscapeTableName => "[{0}]";

    /// <summary>
    /// Returns the sql identifier format
    /// </summary>
    public string EscapeSqlIdentifier => "[{0}]";

    /// <summary>
    /// Returns the escaped assignment format
    /// </summary>
    public string EscapeSqlAssignment => "[{0}] = @{1}";

    /// <summary>
    /// Returns true if schemas are supported in database
    /// </summary>
    public bool SupportsSchemas => false;

}

/// <summary>
/// The MySQL database adapter.
/// </summary>
public partial class MySqlAdapter : SqlAdapter, ISqlAdapter
{
    /// <summary>
    /// Inserts <paramref name="entityToInsert"/> into the database, returning the Id of the row created.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout to use.</param>
    /// <param name="tableName">The table to insert into.</param>
    /// <param name="columnList">The columns to set with this insert.</param>
    /// <param name="parameterList">The parameters to set for this insert.</param>
    /// <param name="keyProperties">The key columns in this table.</param>
    /// <param name="entityToInsert">The entity to insert.</param>
    /// <returns>true if inserted</returns>
    public bool Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, string tableName, string columnList, string parameterList, IEnumerable<ColumnInfo> keyProperties, object entityToInsert)
    {
        var cmd = $"insert into {tableName} ({columnList}) values ({parameterList})";
        connection.Execute(cmd, entityToInsert, transaction, commandTimeout);
        var r = connection.Query("Select LAST_INSERT_ID() id", transaction: transaction, commandTimeout: commandTimeout);

        var id = r.First().id;
        if (id == null) return false;
        var propertyInfos = keyProperties.Select(p => p.Property).ToArray();
        if (propertyInfos.Length == 0) return Convert.ToInt32(id);

        var idp = propertyInfos[0];
        idp.SetValue(entityToInsert, Convert.ChangeType(id, idp.PropertyType), null);

        return id != null;
    }

    /// <summary>
    /// Adds the name of a column.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnName(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("`{0}`", columnName);
    }

    /// <summary>
    /// Adds a column equality to a parameter.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnNameEqualsValue(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("`{0}` = @{1}", columnName, columnName);
    }

    /// <summary>
    /// Returns the schema and table name
    /// </summary>
    /// <param name="tableName">The table</param>
    /// <param name="schema">The Schema if it was specified</param>
    /// <returns>schema + table</returns>
    public string FormatSchemaTable(string tableName, string schema)
    {
        return string.IsNullOrEmpty(schema) ? $"[{tableName}]" : $"[{schema}].[{ tableName}]";
    }

    /// <summary>
    /// Returns sql existence statement
    /// </summary>
    /// <returns>sql</returns>
    public string GetExistsSql(string tableName, string whereClause) => $"SELECT EXISTS (SELECT 1 FROM {tableName} WHERE {whereClause})";

    /// <summary>
    /// Returns the format for table name
    /// </summary>
    public string EscapeTableName => "`{0}`";

    /// <summary>
    /// Returns the sql identifier format
    /// </summary>
    public string EscapeSqlIdentifier => "`{0}`";

    /// <summary>
    /// Returns the escaped assignment format
    /// </summary>
    public string EscapeSqlAssignment => "`{0}` = @{1}";

    /// <summary>
    /// Returns true if schemas are supported in database
    /// </summary>
    public bool SupportsSchemas => false;

}

/// <summary>
/// The Postgres database adapter.
/// </summary>
public partial class PostgresAdapter : SqlAdapter, ISqlAdapter
{
    /// <summary>
    /// Inserts <paramref name="entityToInsert"/> into the database, returning the Id of the row created.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout to use.</param>
    /// <param name="tableName">The table to insert into.</param>
    /// <param name="columnList">The columns to set with this insert.</param>
    /// <param name="parameterList">The parameters to set for this insert.</param>
    /// <param name="keyProperties">The key columns in this table.</param>
    /// <param name="entityToInsert">The entity to insert.</param>
    /// <returns>true if inserted</returns>
    public bool Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, string tableName, string columnList, string parameterList, IEnumerable<ColumnInfo> keyProperties, object entityToInsert)
    {
        var sb = new StringBuilder();
        sb.AppendFormat("insert into {0} ({1}) values ({2})", tableName, columnList, parameterList);

        // If no primary key then safe to assume a join table with not too much data to return
        var propertyInfos = keyProperties.Select(p => p.Property).ToArray();
        if (propertyInfos.Length == 0)
        {
            sb.Append(" RETURNING *");
        }
        else
        {
            sb.Append(" RETURNING ");
            var first = true;
            foreach (var property in propertyInfos)
            {
                if (!first)
                    sb.Append(", ");
                first = false;
                sb.Append(property.Name);
            }
        }

        var results = connection.Query(sb.ToString(), entityToInsert, transaction, commandTimeout: commandTimeout).ToList();

        // Return the key by assinging the corresponding property in the object - by product is that it supports compound primary keys
        var id = 0;
        foreach (var p in propertyInfos)
        {
            var value = ((IDictionary<string, object>)results[0])[p.Name.ToLower()];
            p.SetValue(entityToInsert, value, null);
            if (id == 0)
                id = Convert.ToInt32(value);
        }
        return id > 0;
    }

    /// <summary>
    /// Adds the name of a column.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnName(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("\"{0}\"", columnName);
    }

    /// <summary>
    /// Adds a column equality to a parameter.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnNameEqualsValue(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("\"{0}\" = @{1}", columnName, columnName);
    }

    /// <summary>
    /// Returns the schema and table name
    /// </summary>
    /// <param name="tableName">The table</param>
    /// <param name="schema">The Schema if it was specified</param>
    /// <returns>schema + table</returns>
    public string FormatSchemaTable(string tableName, string schema)
    {
        return string.IsNullOrEmpty(schema) ? $"[{tableName}]" : $"[{schema}].[{ tableName}]";
    }

    /// <summary>
    /// Returns sql existence statement
    /// </summary>
    /// <returns>sql</returns>
    public string GetExistsSql(string tableName, string whereClause) => $"SELECT EXISTS (SELECT 1 FROM {tableName} WHERE {whereClause})";

    /// <summary>
    /// Returns the format for table name
    /// </summary>
    public string EscapeTableName => "[{0}]";

    /// <summary>
    /// Returns the sql identifier format
    /// </summary>
    public string EscapeSqlIdentifier => "\"{0}\"";

    /// <summary>
    /// Returns the escaped assignment format
    /// </summary>
    public string EscapeSqlAssignment => "\"{0}\" = @{1}";

    /// <summary>
    /// Returns true if schemas are supported in database
    /// </summary>
    public bool SupportsSchemas => false;

}

/// <summary>
/// The SQLite database adapter.
/// </summary>
public partial class SQLiteAdapter : SqlAdapter, ISqlAdapter
{
    /// <summary>
    /// Inserts <paramref name="entityToInsert"/> into the database, returning the Id of the row created.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout to use.</param>
    /// <param name="tableName">The table to insert into.</param>
    /// <param name="columnList">The columns to set with this insert.</param>
    /// <param name="parameterList">The parameters to set for this insert.</param>
    /// <param name="keyProperties">The key columns in this table.</param>
    /// <param name="entityToInsert">The entity to insert.</param>
    /// <returns>true if inserted</returns>
    public bool Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, string tableName, string columnList, string parameterList, IEnumerable<ColumnInfo> keyProperties, object entityToInsert)
    {
        var cmd = $"INSERT INTO {tableName} ({columnList}) VALUES ({parameterList}); SELECT last_insert_rowid() id";
        var multi = connection.QueryMultiple(cmd, entityToInsert, transaction, commandTimeout);

        var id = multi.Read().First().id;
        var propertyInfos = keyProperties.Select(p => p.Property).ToArray();
        if (id != null && propertyInfos.Any())
        {
            var idp = propertyInfos[0];
            idp.SetValue(entityToInsert, Convert.ChangeType(id, idp.PropertyType), null);
        }

        return id != null;
    }

    /// <summary>
    /// Adds the name of a column.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnName(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("\"{0}\"", columnName);
    }

    /// <summary>
    /// Adds a column equality to a parameter.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnNameEqualsValue(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("\"{0}\" = @{1}", columnName, columnName);
    }

    /// <summary>
    /// Returns the schema and table name
    /// </summary>
    /// <param name="tableName">The table</param>
    /// <param name="schema">The Schema if it was specified</param>
    /// <returns>schema + table</returns>
    public string FormatSchemaTable(string tableName, string schema)
    {
        return $"[{tableName}]"; //sqllite doesn't support schema
    }

    /// <summary>
    /// Returns sql existence statement
    /// </summary>
    /// <returns>sql</returns>
    public string GetExistsSql(string tableName, string whereClause) => $"SELECT 1 FROM {tableName} WHERE {whereClause}";

    /// <summary>
    /// Returns the format for table name
    /// </summary>
    public string EscapeTableName => "[{0}]";

    /// <summary>
    /// Returns the sql identifier format
    /// </summary>
    public string EscapeSqlIdentifier => "\"{0}\"";

    /// <summary>
    /// Returns the escaped assignment format
    /// </summary>
    public string EscapeSqlAssignment => "\"{0}\" = @{1}";

    /// <summary>
    /// Returns true if schemas are supported in database
    /// </summary>
    public bool SupportsSchemas => false;


}

/// <summary>
/// The Firebase SQL adapeter.
/// </summary>
public partial class FbAdapter : SqlAdapter, ISqlAdapter
{
    /// <summary>
    /// Inserts <paramref name="entityToInsert"/> into the database, returning the Id of the row created.
    /// </summary>
    /// <param name="connection">The connection to use.</param>
    /// <param name="transaction">The transaction to use.</param>
    /// <param name="commandTimeout">The command timeout to use.</param>
    /// <param name="tableName">The table to insert into.</param>
    /// <param name="columnList">The columns to set with this insert.</param>
    /// <param name="parameterList">The parameters to set for this insert.</param>
    /// <param name="keyProperties">The key columns in this table.</param>
    /// <param name="entityToInsert">The entity to insert.</param>
    /// <returns>true if inserted</returns>
    public bool Insert(IDbConnection connection, IDbTransaction transaction, int? commandTimeout, string tableName, string columnList, string parameterList, IEnumerable<ColumnInfo> keyProperties, object entityToInsert)
    {
        var cmd = $"insert into {tableName} ({columnList}) values ({parameterList})";
        connection.Execute(cmd, entityToInsert, transaction, commandTimeout);

        var propertyInfos = keyProperties.Select(p => p.Property).ToArray();
        var keyName = propertyInfos[0].Name;
        var r = connection.Query($"SELECT FIRST 1 {keyName} ID FROM {tableName} ORDER BY {keyName} DESC", transaction: transaction, commandTimeout: commandTimeout);

        var id = r.First().ID;
        if (id != null && propertyInfos.Any())
        {
            var idp = propertyInfos[0];
            idp.SetValue(entityToInsert, Convert.ChangeType(id, idp.PropertyType), null);
        }

        return id != null;
    }

    /// <summary>
    /// Adds the name of a column.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnName(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("{0}", columnName);
    }

    /// <summary>
    /// Adds a column equality to a parameter.
    /// </summary>
    /// <param name="sb">The string builder  to append to.</param>
    /// <param name="columnName">The column name.</param>
    public void AppendColumnNameEqualsValue(StringBuilder sb, string columnName)
    {
        sb.AppendFormat("{0} = @{1}", columnName, columnName);
    }

    /// <summary>
    /// Returns the schema and table name
    /// </summary>
    /// <param name="tableName">The table</param>
    /// <param name="schema">The Schema if it was specified</param>
    /// <returns>schema + table</returns>
    public string FormatSchemaTable(string tableName, string schema)
    {
        return string.IsNullOrEmpty(schema) ? $"[{tableName}]" : $"[{schema}].[{ tableName}]";
    }

    /// <summary>
    /// Returns sql existence statement
    /// </summary>
    /// <returns>sql</returns>
    public string GetExistsSql(string tableName, string whereClause) => $"SELECT FIRST 1 1 ID FROM {tableName} WHERE {whereClause}";

    /// <summary>
    /// Returns the format for table name
    /// </summary>
    public string EscapeTableName => "{0}";

    /// <summary>
    /// Returns the sql identifier format
    /// </summary>
    public string EscapeSqlIdentifier => "{0}";

    /// <summary>
    /// Returns the escaped assignment format
    /// </summary>
    public string EscapeSqlAssignment => "{0} = @{1}";

    /// <summary>
    /// Returns true if schemas are supported in database
    /// </summary>
    public bool SupportsSchemas => false;


}
