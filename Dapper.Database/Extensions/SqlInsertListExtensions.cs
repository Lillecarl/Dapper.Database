﻿using System.Collections.Generic;
using System.Data;

namespace Dapper.Database.Extensions
{
    /// <summary>
    /// The Dapper.Database extensions for Dapper
    /// </summary>
    public static partial class SqlMapperExtensions
    {

        #region InsertList Queries
        /// <summary>
        /// Inserts an entity into table "Ts" and returns identity id or number of inserted rows if inserting a list.
        /// </summary>
        /// <typeparam name="T">The type to insert.</typeparam>
        /// <param name="connection">Open SqlConnection</param>
        /// <param name="entitiesToInsert">List of Entities to insert</param>
        /// <param name="transaction">The transaction to run under, null (the default) if none</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>the entity to insert or the list of entities</returns>
        public static bool InsertList<T>(this IDbConnection connection, IEnumerable<T> entitiesToInsert, IDbTransaction transaction = null, int? commandTimeout = null) where T : class
        {
            var type = typeof(T);
            var adapter = GetFormatter(connection);
            var tinfo = TableInfoCache(type);
            return adapter.InsertList(connection, transaction, commandTimeout, tinfo, entitiesToInsert);
        }

        #endregion

    }
}
