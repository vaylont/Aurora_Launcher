using System;
using Npgsql;
using System.Configuration;

namespace AuroraLauncher
{
    public static class DbCon
    {
        private static readonly string connString = "Host=localhost;Port=5432;Username=postgres;Password=root;Database=postgres;SearchPath=public";

        public static NpgsqlConnection GetConnection()
        {
            var conn = new NpgsqlConnection(connString);
            conn.Open();
            return conn;
        }
    }
}
