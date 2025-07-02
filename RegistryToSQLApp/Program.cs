using System;
using System.Data.SqlClient;
using System.IO;

namespace RegistryToSQLApp
{
    class Program
    {
        static string connString = "Server=localhost;Database=ApplicationConfiguration;Trusted_Connection=True;";
        static string logFilePath = "log.txt";

        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Log("Usage: RegistryToSQLApp.exe <RegSection> <RegNode> <RegKey>");
                return;
            }

            string regSection = args[0];
            string regNode = args[1];
            string regKey = args[2];
            string configValue;

            try
            {
                configValue = ReadRegistryValueFromSQL(regSection, regNode, regKey);

                if (string.IsNullOrEmpty(configValue))
                    configValue = "MISSING";

                InsertIntoDatabase(regKey, configValue, regNode, regSection);
                Log($"SUCCESS: Inserted '{regKey}' = '{configValue}' into ApplicationConfigurationValues.");
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message);
            }
        }

        static string ReadRegistryValueFromSQL(string section, string node, string key)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                string sql = "SELECT dbo.ReadRegistry(@section, @node, @key)";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@section", section);
                    cmd.Parameters.AddWithValue("@node", node);
                    cmd.Parameters.AddWithValue("@key", key);

                    object result = cmd.ExecuteScalar();
                    return result?.ToString();
                }
            }
        }

        static void InsertIntoDatabase(string configKey, string configValue, string regNode, string regSection)
        {
            using (SqlConnection conn = new SqlConnection(connString))
            {
                conn.Open();

                string insertQuery = @"
IF NOT EXISTS (
    SELECT 1 FROM ApplicationConfigurationValues 
    WHERE ApplicationId = @ApplicationId AND ConfigKey = @ConfigKey AND RegistryNode = @RegistryNode
)
BEGIN
    INSERT INTO ApplicationConfigurationValues 
    (ApplicationId, ConfigKey, ConfigSection, ConfigValue, RegistryNode, RegistryKey) 
    VALUES (@ApplicationId, @ConfigKey, @ConfigSection, @ConfigValue, @RegistryNode, @RegistryKey)
END";

                using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ApplicationId", 1);
                    cmd.Parameters.AddWithValue("@ConfigKey", configKey);
                    cmd.Parameters.AddWithValue("@ConfigSection", regSection);
                    cmd.Parameters.AddWithValue("@ConfigValue", configValue);
                    cmd.Parameters.AddWithValue("@RegistryNode", regNode);
                    cmd.Parameters.AddWithValue("@RegistryKey", configKey);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        static void Log(string message)
        {
            string entry = $"[{DateTime.Now}] {message}";
            Console.WriteLine(entry);
            File.AppendAllText(logFilePath, entry + Environment.NewLine);
        }
    }
}
