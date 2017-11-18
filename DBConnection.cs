using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Configuration;
using System.Diagnostics;

namespace PlayLogger
{
    public class DBConnection
    {
        private DBConnection()
        {
            ServerAddress = Config.Instance.Get("DbServer");
            DatabaseName = Config.Instance.Get("DbName");
            UserName = Config.Instance.Get("DbUserName");
            Password = Config.Instance.Get("DbPassword");
        }
        public string ServerAddress { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }


        private MySqlConnection connection = null;
        public MySqlConnection Connection
        {
            get { return connection; }
        }

        private static DBConnection _instance = null;
        public static DBConnection Instance()
        {
            if (_instance == null)
                _instance = new DBConnection();
            return _instance;
        }

        public bool IsConnect()
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
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        public void Close()
        {
            connection.Close();
        }
    }
}
