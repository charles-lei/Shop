//===============================================================================
// This file is based on the Microsoft Data Access Application Block for .NET
// For more information please go to 
// http://msdn.microsoft.com/library/en-us/dnbda/html/daab-rm.asp
//===============================================================================

using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace BrnShop.Core.Data.RDBS
{
    /// <summary>
    /// 关系数据库帮助类
    /// </summary>
    public class RdbsHelper
    {
        public static object Locker { get; } = new object();

        private static readonly DbProviderFactory Factory;//抽象数据工厂

        /// <summary>
        /// 关系数据库对象前缀
        /// </summary>
        public static string RdbsTablePre { get; }

        /// <summary>
        /// 关系数据库连接字符串
        /// </summary>
        public static string ConnectionString { get; }

#if DEBUG

        /// <summary>
        /// 数据库执行次数
        /// </summary>
        public static int ExecuteCount { get; set; }

        /// <summary>
        /// 数据库执行细节
        /// </summary>
        public static string ExecuteDetail { get; set; } = string.Empty;

        /// <summary>
        /// 设置数据库执行细节
        /// </summary>
        /// <param name="commandText">数据库执行语句</param>
        /// <param name="startTime">数据库执行开始时间</param>
        /// <param name="endTime">数据库执行结束时间</param>
        /// <param name="commandParameters">数据库执行参数列表</param>
        /// <returns></returns>
        private static string GetExecuteDetail(string commandText, DateTime startTime, DateTime endTime, DbParameter[] commandParameters)
        {
            if (commandParameters != null && commandParameters.Length > 0)
            {
                StringBuilder paramdetails = new StringBuilder("<div style=\"display:block;clear:both;margin-left:auto;margin-right:auto;width:100%;\"><table cellspacing=\"0\" cellpadding=\"0\" style=\"border: 1px solid black;background:rgb(255, 255, 255) none repeat scroll 0%;font-size:12px;display:block;margin-left:auto;margin-right:auto;margin-top:5px;margin-bottom:5px;width:960px;\">");
                paramdetails.AppendFormat("<tr><td colspan=\"3\">执行SQL：{0}</td></tr>", commandText);
                paramdetails.AppendFormat("<tr><td colspan=\"3\">执行时间：{0}</td></tr>", endTime.Subtract(startTime).TotalMilliseconds / 1000);
                foreach (DbParameter param in commandParameters)
                {
                    if (param == null)
                        continue;

                    paramdetails.Append("<tr>");
                    paramdetails.AppendFormat("<td width=\"250px\">参数名称：{0}</td>", param.ParameterName);
                    paramdetails.AppendFormat("<td width=\"250px\">参数类型：{0}</td>", param.DbType);
                    paramdetails.AppendFormat("<td>参数值：{0}</td>", param.Value);
                    paramdetails.Append("</tr>");
                }
                return paramdetails.Append("</table></div>").ToString();
            }
            return string.Empty;
        }
#endif

        static RdbsHelper()
        {
            //设置数据工厂
            Factory = BSPData.RDBS.GetDbProviderFactory();
            //设置关系数据库对象前缀
            RdbsTablePre = BSPConfig.RDBSConfig.RDBSTablePre;
            //设置关系数据库连接字符串
            ConnectionString = BSPConfig.RDBSConfig.RDBSConnectString;
        }

        #region ExecuteNonQuery

        public static int ExecuteNonQuery(string cmdText)
        {
            return ExecuteNonQuery(CommandType.Text, cmdText, null);
        }

        public static int ExecuteNonQuery(CommandType cmdType, string cmdText)
        {
            return ExecuteNonQuery(cmdType, cmdText, null);
        }

        public static int ExecuteNonQuery(CommandType cmdType, string cmdText, params DbParameter[] commandParameters)
        {
#if DEBUG
            ExecuteCount++;
#endif
            var cmd = Factory.CreateCommand();

            using (var conn = Factory.CreateConnection())
            {
                if (conn != null)
                {
                    conn.ConnectionString = ConnectionString;
                    PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                }
#if DEBUG
                var startTime = DateTime.Now;
#endif
                if (cmd != null)
                {
                    var val = cmd.ExecuteNonQuery();
#if DEBUG
                    var endTime = DateTime.Now;
                    ExecuteDetail += GetExecuteDetail(cmd.CommandText, startTime, endTime, commandParameters);
#endif
                    cmd.Parameters.Clear();
                    return val;
                }
            }
            return 0;
        }



        public static int ExecuteNonQuery(DbTransaction trans, CommandType cmdType, string cmdText)
        {
            return ExecuteNonQuery(trans, cmdType, cmdText, null);
        }

        public static int ExecuteNonQuery(DbTransaction trans, CommandType cmdType, string cmdText, params DbParameter[] commandParameters)
        {
#if DEBUG
            ExecuteCount++;
#endif

            var cmd = Factory.CreateCommand();
            PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, commandParameters);
#if DEBUG
            var startTime = DateTime.Now;
#endif
            if (cmd == null) return 0;
            var val = cmd.ExecuteNonQuery();
#if DEBUG
            var endTime = DateTime.Now;
            ExecuteDetail += GetExecuteDetail(cmd.CommandText, startTime, endTime, commandParameters);
#endif
            cmd.Parameters.Clear();
            return val;
        }

        #endregion

        #region ExecuteReader

        public static DbDataReader ExecuteReader(CommandType cmdType, string cmdText)
        {
            return ExecuteReader(cmdType, cmdText, null);
        }

        public static DbDataReader ExecuteReader(CommandType cmdType, string cmdText, params DbParameter[] commandParameters)
        {
#if DEBUG
            ExecuteCount++;
#endif

            var cmd = Factory.CreateCommand();
            var conn = Factory.CreateConnection();
            if (conn != null)
            {
                conn.ConnectionString = ConnectionString;

                try
                {
                    PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
#if DEBUG
                    var startTime = DateTime.Now;
#endif
                    if (cmd != null)
                    {
                        var rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
#if DEBUG
                        var endTime = DateTime.Now;
                        ExecuteDetail += GetExecuteDetail(cmd.CommandText, startTime, endTime, commandParameters);
#endif
                        cmd.Parameters.Clear();
                        return rdr;
                    }
                }
                catch
                {
                    conn.Close();
                    throw;
                }
            }
            return null;
        }



        public static DbDataReader ExecuteReader(DbTransaction trans, CommandType cmdType, string cmdText)
        {
            return ExecuteReader(trans, cmdType, cmdText, null);
        }

        public static DbDataReader ExecuteReader(DbTransaction trans, CommandType cmdType, string cmdText, params DbParameter[] commandParameters)
        {
#if DEBUG
            ExecuteCount++;
#endif

            var cmd = Factory.CreateCommand();

            PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, commandParameters);
#if DEBUG
            var startTime = DateTime.Now;
#endif
            if (cmd == null) return null;
            var rdr = cmd.ExecuteReader();
#if DEBUG
            var endTime = DateTime.Now;
            ExecuteDetail += GetExecuteDetail(cmd.CommandText, startTime, endTime, commandParameters);
#endif
            cmd.Parameters.Clear();
            return rdr;
        }

        #endregion

        #region ExecuteScalar

        public static object ExecuteScalar(CommandType cmdType, string cmdText)
        {
            return ExecuteScalar(cmdType, cmdText, null);
        }

        public static object ExecuteScalar(CommandType cmdType, string cmdText, params DbParameter[] commandParameters)
        {
#if DEBUG
            ExecuteCount++;
#endif

            var cmd = Factory.CreateCommand();

            using (var connection = Factory.CreateConnection())
            {
                if (connection != null)
                {
                    connection.ConnectionString = ConnectionString;
                    PrepareCommand(cmd, connection, null, cmdType, cmdText, commandParameters);
                }
#if DEBUG
                var startTime = DateTime.Now;
#endif
                if (cmd == null) return null;
                var val = cmd.ExecuteScalar();
#if DEBUG
                var endTime = DateTime.Now;
                ExecuteDetail += GetExecuteDetail(cmd.CommandText, startTime, endTime, commandParameters);
#endif
                cmd.Parameters.Clear();
                return val;
            }
        }



        public static object ExecuteScalar(DbTransaction trans, CommandType cmdType, string cmdText)
        {
            return ExecuteScalar(trans, cmdType, cmdText, null);
        }

        public static object ExecuteScalar(DbTransaction trans, CommandType cmdType, string cmdText, params DbParameter[] commandParameters)
        {
#if DEBUG
            ExecuteCount++;
#endif

            var cmd = Factory.CreateCommand();
            PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, commandParameters);
#if DEBUG
            var startTime = DateTime.Now;
#endif
            if (cmd == null) return null;
            var val = cmd.ExecuteScalar();
#if DEBUG
            var endTime = DateTime.Now;
            ExecuteDetail += GetExecuteDetail(cmd.CommandText, startTime, endTime, commandParameters);
#endif
            cmd.Parameters.Clear();
            return val;
        }

        #endregion

        #region ExecuteDataset

        public static DataSet ExecuteDataset(CommandType cmdType, string cmdText)
        {
            return ExecuteDataset(cmdType, cmdText, null);
        }

        public static DataSet ExecuteDataset(CommandType cmdType, string cmdText, params DbParameter[] commandParameters)
        {
#if DEBUG
            ExecuteCount++;
#endif

            var cmd = Factory.CreateCommand();
            var conn = Factory.CreateConnection();
            if (conn != null)
            {
                conn.ConnectionString = ConnectionString;
                var adapter = Factory.CreateDataAdapter();

                try
                {
                    PrepareCommand(cmd, conn, null, cmdType, cmdText, commandParameters);
                    if (adapter != null)
                    {
                        adapter.SelectCommand = cmd;
                        var ds = new DataSet();

#if DEBUG
                        var startTime = DateTime.Now;
#endif
                        adapter.Fill(ds);
#if DEBUG
                        var endTime = DateTime.Now;
                        if (cmd == null) return ds;
                        ExecuteDetail += GetExecuteDetail(cmd.CommandText, startTime, endTime, commandParameters);
#endif
                        cmd.Parameters.Clear();
                        return ds;
                    }
                }
                finally
                {
                    adapter?.Dispose();
                    conn.Close();
                }
            }
            return new DataSet();
        }



        public static DataSet ExecuteDataset(DbTransaction trans, CommandType cmdType, string cmdText)
        {
            return ExecuteDataset(trans, cmdType, cmdText, null);
        }

        public static DataSet ExecuteDataset(DbTransaction trans, CommandType cmdType, string cmdText, params DbParameter[] commandParameters)
        {
#if DEBUG
            ExecuteCount++;
#endif

            DbCommand cmd = Factory.CreateCommand();
            DbDataAdapter adapter = Factory.CreateDataAdapter();

            PrepareCommand(cmd, trans.Connection, trans, cmdType, cmdText, commandParameters);
            if (adapter != null)
            {
                adapter.SelectCommand = cmd;
                var ds = new DataSet();

#if DEBUG
                var startTime = DateTime.Now;
#endif
                adapter.Fill(ds);
#if DEBUG
                var endTime = DateTime.Now;
                if (cmd == null) return ds;
                ExecuteDetail += GetExecuteDetail(cmd.CommandText, startTime, endTime, commandParameters);
#endif
                cmd.Parameters.Clear();
                return ds;
            }
            return new DataSet();
        }

        #endregion

        private static void PrepareCommand(DbCommand cmd, DbConnection conn, DbTransaction trans, CommandType cmdType, string cmdText, DbParameter[] cmdParms)
        {

            if (conn.State != ConnectionState.Open)
                conn.Open();

            cmd.Connection = conn;
            cmd.CommandText = cmdText;

            if (trans != null)
                cmd.Transaction = trans;

            cmd.CommandType = cmdType;

            if (cmdParms == null) return;
            foreach (var parm in cmdParms.Where(parm => parm != null))
            {
                if ((parm.Direction == ParameterDirection.InputOutput || parm.Direction == ParameterDirection.Input) &&
                    (parm.Value == null))
                {
                    parm.Value = DBNull.Value;
                }
                cmd.Parameters.Add(parm);
            }
        }

    }
}
