using System;
using System.Data.SqlClient;
using Microsoft.Win32;

namespace RegistryToSQLApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string registryPath = @"SOFTWARE\WOW6432Node\nexRIS\agora\nexris\Parameters\Business Rules";
            string registrySection = "Registry";
            string connString = "Server=localhost;Database=ApplicationConfiguration;Trusted_Connection=True;";

            try
            {
                using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(registryPath))
                {
                    if (baseKey == null)
                    {
                        Console.WriteLine("Registry path not found.");
                        return;
                    }

                    foreach (var valueName in baseKey.GetValueNames())
                    {
                        object value = baseKey.GetValue(valueName);
                        string valueStr = value != null ? value.ToString() : "MISSING";

                        InsertIntoDatabase(connString, valueName, valueStr, registryPath, registrySection);
                        Console.WriteLine($"Inserted: {valueName} = {valueStr}");
                    }

                    Console.WriteLine("All registry values inserted successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }

        static void InsertIntoDatabase(string connectionString, string key, string value, string regPath, string section)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
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
                    cmd.Parameters.AddWithValue("@ConfigKey", key);
                    cmd.Parameters.AddWithValue("@ConfigSection", section);
                    cmd.Parameters.AddWithValue("@ConfigValue", value);
                    cmd.Parameters.AddWithValue("@RegistryNode", regPath);
                    cmd.Parameters.AddWithValue("@RegistryKey", key);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
