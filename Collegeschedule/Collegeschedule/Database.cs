using MySql.Data.MySqlClient;
using System.Data.SqlClient;

namespace Collegeschedule
{
    public static class Database
    {
        private static string connStr =
            "server=localhost;user=root;password=1111;database=college_schedule;";

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(connStr);
        }
    }
}