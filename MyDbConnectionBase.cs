using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace PlayLogger
{
    public abstract class MyDbConnectionBase : IDisposable
    {

        protected DbConnection connection = null;

        public abstract bool IsConnect();

        public virtual void Close()
        {
            if (connection != null && connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }

        public abstract DbCommand CreateCmd(string query = null);
        public abstract DbParameter CreateParam(string name, object value);


        public virtual void Dispose()
        {
            Close();
            if (connection != null)
            {
                connection.Dispose();
            }
        }

        public static MyDbConnectionBase CreateInstace()
        {
            MyDbConnectionBase res = null;
            eDataBaseTypes dbType = eDataBaseTypes.Postgresql;
            Enum.TryParse<eDataBaseTypes>(Config.Instance.Get("DbType"), out dbType);

            switch (dbType)
            {
                case eDataBaseTypes.Postgresql:
                    res = new PostgreSqlDbConnectionHandler();
                    break;
                case eDataBaseTypes.MySql:
                    res = new MySqlDbConnectionHandler();
                    break;
                default:
                    break;
            }

            return res;
        }
    }
}
