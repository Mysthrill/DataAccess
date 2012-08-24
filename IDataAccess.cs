using System.Data;

namespace DataAccess
{
    public interface IDataAccess
    {
        IDbConnection DbConnection { get; }
       
        bool CheckConnection();
        
        DataSet GetDataSet(IDbCommand command);
        
        DataTable GetDataTable(IDbCommand command);

        DataRow GetDataRow(IDbCommand command);

        int ExecuteNonQuery(IDbCommand command);

        void BeginTransaction();

        void BeginTransaction(IsolationLevel isolationLevel);

        void CommitTransaction();

        void RollBackTransaction();
    }
}
