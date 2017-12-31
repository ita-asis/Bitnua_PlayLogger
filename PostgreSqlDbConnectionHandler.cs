using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Diagnostics;
using System.Data.Common;
using Npgsql;

namespace PlayLogger
{
    public class PostgreSqlDbConnectionHandler : MyDbConnectionBase
    {
        public PostgreSqlDbConnectionHandler()
        {
            ServerAddress = Config.Instance.Get("DbServer");
            ServerPort = Config.Instance.Get("DbPort");
            DatabaseName = Config.Instance.Get("DbName");
            UserName = Config.Instance.Get("DbUserName");
            Password = Config.Instance.Get("DbPassword");
            IsConnect();
        }
        private string ServerAddress { get; set; }
        private string ServerPort { get; set; }
        private string DatabaseName { get; set; }
        private string UserName { get; set; }
        private string Password { get; set; }


        private NpgsqlConnection Connection
        {
            get { return (NpgsqlConnection)connection; }
        }

        public override bool IsConnect()
        {
            bool result = true;

            try
            {

                if (Connection == null || Connection.State != System.Data.ConnectionState.Open)
                {
                    if (String.IsNullOrEmpty(ServerAddress) || String.IsNullOrEmpty(DatabaseName))
                    {
                        result = false;
                    }
                    else
                    {
                        string connstring = string.Format("Server={0};Port={1};Database={2};User Id={3};Password={4}; SSL Mode=Prefer; Trust Server Certificate=true;", ServerAddress, ServerPort, DatabaseName, UserName, Password);
                        connection = new NpgsqlConnection(connstring);

                        connection.Open();
                        DbCommand test = CreateCmd("select 1");
                        test.ExecuteScalar();
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                result = false;
                MainViewModel.LogException(ex);
            }

            return result;
        }

        public override DbCommand CreateCmd(string query = null)
        {
            return new NpgsqlCommand(query, Connection);
        }

        public override DbParameter CreateParam(string name, object value)
        {
            return new NpgsqlParameter(name, value);
        }

    }
}
