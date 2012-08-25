 using System;
using System.Data;
 using System.Transactions;
 using System.Collections.Generic;
using System.Data.Common;

namespace DataAccess
{
    public class DataAccess : IDataAccess
    {
        #region Variables
        /// <summary>
        /// Private readonly variable Connection of type IDbConnection. Holding all neccesary information about the connection.
        /// </summary>
        private readonly IDbConnection _connection;

        /// <summary>
        /// Private readonly variable DataProvider. Exposing information about the DataProvider.
        /// </summary>
        private readonly string _dataProvider;
        /// <summary>
        /// Private readonly variable Transaction of type IDbTransaction. holding information about the Transaction object.
        /// </summary>
        private IDbTransaction _transaction;

        private readonly DbProviderFactory _dbProviderFactory;
        #endregion

        #region Public Properties

        /// <summary>
        /// Public Property Connection of type IDbConnection. Holding all neccesary information about the connection.
        /// </summary>
        public IDbConnection DbConnection
        {
            get
            {
                return _connection;
            }
        }

        /// <summary>
        /// Name of the Data Provider
        /// </summary>
        public string DataProvider
        {
            get { return _dataProvider; }
        }

        /// <summary>
        /// DbProvider factory linked to the DataProvider
        /// </summary>
        public DbProviderFactory DbProviderFactory
        {
            get { return _dbProviderFactory; }
        }
        #endregion

        #region Constructors

        /// <summary>
        /// Overloaded Class Constructor.
        /// </summary>
        /// <param name="dataProvider">name of the dataprovider</param>
        /// <param name="connectionString">The connectionstring used for the dataprovider</param>
        public DataAccess(string dataProvider, string connectionString)
        {
            if (String.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("connectionString");
            if (String.IsNullOrEmpty(dataProvider))
                throw new ArgumentNullException("dataProvider");

            _dataProvider = dataProvider;

            //get the correct DbFactory providing the DB dataprovider
            _dbProviderFactory = DbProviderFactories.GetFactory(dataProvider);

            //Create a connection
            _connection = _dbProviderFactory.CreateConnection();

            //Set the connections connectionstring
            if (_connection != null)
                _connection.ConnectionString = connectionString;
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Check if you can make a connection with the database.
        /// </summary>
        /// <returns>Returns true if a connection is possible.</returns>
        public bool CheckConnection()
        {
            try
            {
                _connection.Open();
                _connection.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Generic method used to excecute a IDbCommand statement without parameters.
        /// </summary>
        /// <param name="command">Command statement of type IDbCommand</param>
        /// <returns>Returns the number of rows affected.</returns>
        public int ExecuteNonQuery(IDbCommand command)
        {
            int result;

            using (command)
            {
                _connection.Open();

                command.Connection = _connection;

                try
                {
                    //Using transactionscopes doesnt work too well on oracle databases.
                    using (TransactionScope scope = new TransactionScope())
                    {
                        result = command.ExecuteNonQuery();
                        scope.Complete();
                    }
                }
                finally
                {
                    _connection.Close();
                }
            }
            return result;
        }
        /// <summary>
        /// Start a transaction on the connection
        /// </summary>
        public void BeginTransaction()
        {
            if(_transaction == null)
                _transaction = DbConnection.BeginTransaction();
        }
        /// <summary>
        /// Begin a tracsaction on the connection by defining its isolationlevel
        /// </summary>
        /// <param name="isolationLevel"></param>
        public void BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            if (_transaction == null)
                _transaction = DbConnection.BeginTransaction(isolationLevel);
        }
        /// <summary>
        /// Commit the transaction on the connection
        /// </summary>
        public void CommitTransaction()
        {
            if(_transaction != null)
            {
                _transaction.Commit();
                _transaction.Dispose();   
            }
        }
        /// <summary>
        /// Rollback transaction on the connection
        /// </summary>
        public void RollBackTransaction()
        {
            if(_transaction != null)
            {
                _transaction.Rollback();
                _transaction.Dispose();   
            }
        }

        #region Read Data
        /// <summary>
        /// Generic method that allows you to retrieve data from your datastore in a dataset.
        /// </summary>
        /// <param name="command">Command statement of type IDbCommand</param>
        /// <returns>A dataset with values as result of the command</returns>
        public DataSet GetDataSet(IDbCommand command)
        {
            DataSet ds = new DataSet();

            _connection.Open();

            using (command)
            {
                command.Connection = _connection;
                IDbDataAdapter da = _dbProviderFactory.CreateDataAdapter();
                if (da != null)
                {
                    da.SelectCommand = command;

                    try
                    {
                        da.Fill(ds);
                    }
                    finally
                    {
                        _connection.Close();
                    }
                }
            }

            return ds;
        }
        /// <summary>
        /// Generic method that allows you to retrieve data from your datastore in a datatable.
        /// </summary>
        /// <param name="command">Command statement of type IDbCommand</param>
        /// <returns></returns>
        public DataTable GetDataTable(IDbCommand command)
        {
            return GetDataSet(command).Tables[0];
        }
        /// <summary>
        /// Generic method that allows you to retrieve data from your datastore in a datarow.
        /// </summary>
        /// <param name="command">Command statement of type IDbCommand</param>
        /// <returns></returns>
        public DataRow GetDataRow(IDbCommand command)
        {
            DataRow dr = null;
            DataTable dt = GetDataTable(command);
            if (dt.Rows.Count > 0)
            {
                dr = dt.Rows[0];
            }
            return dr;
        }

        #endregion

        #region Implementing IDisposable
        /// <summary>
        /// Disposing our connection object.
        /// </summary>
        public void Dispose()
        {
            _connection.Dispose();
        }
        #endregion

        /// <summary>
        /// Public method to execute a stored procedure on the database.
        /// </summary>
        /// <param name="spName">Name of the stored procedure of the type string</param>
        /// <returns>A DataTable containing data returned by the stored procedure</returns>
        public DataTable ExecuteStoredProcedure(string spName)
        {
            return ExecuteStoredProcedure(spName, null, null, null);
        }
        /// <summary>
        ///  Public method to execute a stored procedure on the database providing parameters
        /// </summary>
        /// <param name="spName">Parameter of type String.Name of the stored procedure.</param>
        /// <param name="inParameters">List of In Parameters of type DbParameter</param>
        /// <param name="outParameters">List of Our Parameters of type DbParameter</param>
        /// <param name="returnValue"></param>
        /// <returns>Return a datatable with whatever returns from the database.</returns>
        public DataTable ExecuteStoredProcedure(string spName, IList<DbParameter> inParameters, IList<DbParameter> outParameters, DbParameter returnValue)
        {
            var command = _dbProviderFactory.CreateCommand();
            if (command != null)
            {
                _connection.Open();

                command.Connection = (DbConnection)DbConnection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = spName;

                if (returnValue != null)
                {
                    returnValue.Direction = ParameterDirection.ReturnValue;
                    command.Parameters.Add(returnValue);
                }

                if (inParameters != null)
                {
                    foreach (var parameter in inParameters)
                    {
                        parameter.Direction = ParameterDirection.Input;
                        command.Parameters.Add(parameter);
                    }
                }

                if (outParameters != null)
                {
                    foreach (var parameter in outParameters)
                    {
                        parameter.Direction = ParameterDirection.Output;
                        command.Parameters.Add(parameter);
                    }
                }
            }


            //Does returning this datatable actually work every time?

            DataTable dataTable = null;

            try
            {
                if (command != null)
                {
                    using (IDataReader dataReader = command.ExecuteReader())
                    {
                        dataTable = new DataTable();
                        dataTable.Load(dataReader);
                    } 
                }
            }
            finally
            {
                _connection.Close();
            }

            return dataTable;
        }
        #endregion

        #region Public Static Methods
        // This example assumes a reference to System.Data.Common.
        public static DataTable GetProviderFactoryClasses()
        {
            // Retrieve the installed providers and factories.
            return DbProviderFactories.GetFactoryClasses();
        }
        #endregion
    }
}
