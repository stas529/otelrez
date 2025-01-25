using System;
using MySql.Data.MySqlClient;

namespace OtelRezervasyon.DataAccess
{
    public class DatabaseConnection
    {
        //local icin private readonly string _connectionString = "Server=localhost;Database=hoteldb;Uid=root;Pwd=;Port=3306";
        private readonly string _connectionString = "Server=172.21.54.253;Database=hoteldb;User=25_132330043;Password=!nif.ogr43TA";

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}