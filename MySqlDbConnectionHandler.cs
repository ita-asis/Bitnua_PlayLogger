using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Diagnostics;
using System.Data.Common;

namespace PlayLogger
{
    public class MySqlDbConnectionHandler : MyDbConnectionBase
    {
        public MySqlDbConnectionHandler()
        {
            ServerAddress = Config.Instance.Get("DbServer");
            DatabaseName = Config.Instance.Get("DbName");
            UserName = Config.Instance.Get("DbUserName");
            Password = Config.Instance.Get("DbPassword");
            IsConnect();
        }
        private string ServerAddress { get; set; }
        private string DatabaseName { get; set; }
        private string UserName { get; set; }
        private string Password { get; set; }


        private MySqlConnection Connection
        {
            get { return (MySqlConnection)connection; }
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
                        string connstring = string.Format("Server={0}; database={1}; UID={2}; password={3};charset=utf8;", ServerAddress, DatabaseName, UserName, Password);
                        connection = new MySqlConnection(connstring);
                        connection.Open();
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
            return new MySqlCommand(query, Connection);
        }

        public override DbParameter CreateParam(string name, object value)
        {
            return new MySqlParameter(name, value);
        }
    }
}
