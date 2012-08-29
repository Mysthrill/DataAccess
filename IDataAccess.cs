using System.Data;
using System.Data.Common;
using System;
using System.Collections.Generic;

namespace DataAccess
{
    public interface IDataAccess : IDisposable
    {
        IDbConnection DbConnection { get; }

        string DataProvider { get; }

        DbProviderFactory DbProviderFactory { get; }

        bool CheckConnection();

        DataSet GetDataSet(IDbCommand command);

        DataTable GetDataTable(IDbCommand command);

        DataRow GetDataRow(IDbCommand command);

        int ExecuteNonQuery(IDbCommand command);

        void BeginTransaction();

        void BeginTransaction(IsolationLevel isolationLevel);

        void CommitTransaction();

        void RollBackTransaction();

        DataTable ExecuteStoredProcedure(string spName);

        DataTable ExecuteStoredProcedure(string spName, IList<DbParameter> inParameters,
                                                IList<DbParameter> outParameters, DbParameter returnValue);
    }
}
