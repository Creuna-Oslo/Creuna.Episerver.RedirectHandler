using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using EPiServer.Data;
using EPiServer.DataAccess;
using EPiServer.Logging;
using EPiServer.ServiceLocation;

namespace Creuna.Episerver.RedirectHandler.Core.Data
{
    public class DataAccessBaseEx : DataAccessBase
    {
        internal const string RedirectsTable = "[dbo].[BVN.NotFoundRequests]";
        internal const string VersionStoredProc = "[dbo].[Creuna.RedirectHandler.Version]";

        private static readonly ILogger Logger = LogManager.GetLogger();

        public DataAccessBaseEx(IDatabaseHandler handler)
            : base(handler)
        {
            Database = handler;
        }

        public static DataAccessBaseEx GetWorker()
        {
            return ServiceLocator.Current.GetInstance<DataAccessBaseEx>();
        }

        public DataSet ExecuteSql(string sqlCommand, List<IDbDataParameter> parameters)
        {
            return base.Database.Execute(delegate
            {
                var ds = new DataSet();
                try
                {
                    DbCommand command = CreateCommand(sqlCommand);
                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                        {
                            command.Parameters.Add(parameter);
                        }
                    }
                    command.CommandType = CommandType.Text;
                    base.CreateDataAdapter(command).Fill(ds);
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        string.Format(
                            "An error occured in the ExecuteSQL method with the following sql: {0}. Exception:{1}",
                            sqlCommand, ex));
                }

                return ds;
            });
        }

        public bool ExecuteNonQuery(string sqlCommand)
        {
            return base.Database.Execute(delegate
            {
                bool success = true;

                try
                {
                    IDbCommand command = CreateCommand(sqlCommand);
                    command.CommandType = CommandType.Text;
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    success = false;
                    Logger.Error(
                        string.Format(
                            "An error occureding in the ExecuteSQL method with the following sql{0}. Exception:{1}",
                            sqlCommand, ex));
                }
                return success;
            });
        }


        public DataSet GetAllClientRequestCount()
        {
            string sqlCommand =
                string.Format(
                    "SELECT [OldUrl], COUNT(*) as Requests FROM {0} GROUP BY [OldUrl] order by Requests desc",
                    RedirectsTable);
            return ExecuteSql(sqlCommand, null);
        }

        public void DeleteRowsForRequest(string oldUrl)
        {
            string sqlCommand = string.Format("DELETE FROM {0} WHERE [OldUrl] = @oldurl", RedirectsTable);
            DbParameter oldUrlParam = CreateParameter("oldurl", DbType.String, 4000);
            oldUrlParam.Value = oldUrl;
            var parameters = new List<IDbDataParameter>();
            parameters.Add(oldUrlParam);
            ExecuteSql(sqlCommand, parameters);
        }

        public void DeleteSuggestions(int maxErrors, int minimumDaysOld)
        {
            string sqlCommand = string.Format(@"delete from {0}
                                                where [OldUrl] in (
                                                select [OldUrl]
                                                  from (
                                                      select [OldUrl]
                                                      from {0}
                                                      Where DATEDIFF(day, [Requested], getdate()) >= {1}
                                                      group by [OldUrl]
                                                      having count(*) <= {2} 
                                                      ) t
                                                )", RedirectsTable, minimumDaysOld, maxErrors);
            ExecuteSql(sqlCommand, null);
        }

        public void DeleteAllSuggestions()
        {
            string sqlCommand = string.Format(@"delete from {0}", RedirectsTable);
            ExecuteSql(sqlCommand, null);
        }

        public DataSet GetRequestReferers(string url)
        {
            string sqlCommand =
                string.Format(
                    "SELECT [Referer], COUNT(*) as Requests FROM {0} where [OldUrl] = @oldurl  GROUP BY [Referer] order by Requests desc",
                    RedirectsTable);
            DbParameter oldUrlParam = CreateParameter("oldurl", DbType.String, 4000);
            oldUrlParam.Value = url;

            var parameters = new List<IDbDataParameter>();
            parameters.Add(oldUrlParam);
            return ExecuteSql(sqlCommand, parameters);
        }

        public DataSet GetTotalNumberOfSuggestions()
        {
            string sqlCommand = string.Format("SELECT COUNT(DISTINCT [OldUrl]) FROM {0}", RedirectsTable);
            return ExecuteSql(sqlCommand, null);
        }


        public int CheckModuleVersion()
        {
            return Database.Execute(() =>
            {
                const string sqlCommand = DataAccessBaseEx.VersionStoredProc;
                int version = -1;
                try
                {
                    //  base.Database.Connection.Open();
                    DbCommand command = CreateCommand();

                    command.Parameters.Add(CreateReturnParameter());
                    command.CommandText = sqlCommand;
                    command.CommandType = CommandType.StoredProcedure;
                    //  command.Connection = base.Database.Connection;
                    command.ExecuteNonQuery();
                    version = Convert.ToInt32(GetReturnValue(command).ToString());
                }
                catch (SqlException)
                {
                    Logger.Information("Stored procedure not found. Creating it.");
                    return version;
                }
                catch (Exception ex)
                {
                    Logger.Error(string.Format("Error during NotFoundHandler version check:{0}", ex));
                }
                finally
                {
                    // base.Database.Connection.Close();
                }
                return version;
            });
        }


        public void LogRequestToDb(string oldUrl, string referer, DateTime now)
        {
            Database.Execute(() =>
            {
                string sqlCommand = "INSERT INTO [dbo].[BVN.NotFoundRequests] (" +
                                    "Requested, OldUrl, " +
                                    "Referer" +
                                    ") VALUES (" +
                                    "@requested, @oldurl, " +
                                    "@referer" +
                                    ")";
                try
                {
                    //   base.Database.Connection.Open();
                    // this.OpenConnection();
                    IDbCommand command = CreateCommand();

                    DbParameter requstedParam = CreateParameter("requested", DbType.DateTime, 0);
                    requstedParam.Value = now;
                    DbParameter refererParam = CreateParameter("referer", DbType.String, 4000);
                    refererParam.Value = referer;
                    DbParameter oldUrlParam = CreateParameter("oldurl", DbType.String, 4000);
                    oldUrlParam.Value = oldUrl;
                    command.Parameters.Add(requstedParam);
                    command.Parameters.Add(refererParam);
                    command.Parameters.Add(oldUrlParam);
                    command.CommandText = sqlCommand;
                    command.CommandType = CommandType.Text;
                    command.Connection = base.Database.Connection;
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Logger.Error("An error occured while logging a 404 handler error. Ex:" + ex);
                }
                finally
                {
                    //    base.Database.Connection.Close();
                }
                return true;
            });
        }
    }
}