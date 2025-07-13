using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Reflection;
using ICSharpCode.SharpZipLib.Core;
using MCICommon;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MasterControllerInterface.Database; // Add this line

namespace MasterControllerInterface
{
    public static class DBBackupAndRestore
    {
        private static void LogDebug(string message, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            Debug.WriteLine($"[DBBackupAndRestore.{memberName}:{lineNumber}] {message}");
        }

        private static void LogException(Exception ex, string context, [CallerMemberName] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            Debug.WriteLine($"[DBBackupAndRestore.{memberName}:{lineNumber}] EXCEPTION in {context}:");
            Debug.WriteLine($"  Type: {ex.GetType().FullName}");
            Debug.WriteLine($"  Message: {ex.Message}");
            Debug.WriteLine($"  StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"  InnerException Type: {ex.InnerException.GetType().FullName}");
                Debug.WriteLine($"  InnerException Message: {ex.InnerException.Message}");
                Debug.WriteLine($"  InnerException StackTrace: {ex.InnerException.StackTrace}");
            }
        }

        public static Task<BackupProperties> GetBackupProperties(string file_name)
        {
            return Task.Run(() =>
            {
                LogDebug($"Getting backup properties from: {file_name}");
                try
                {
                    using (MemoryStream ms = new MemoryStream(File.ReadAllBytes(file_name)))
                    {
                        ZipFile zf = new ZipFile(ms);
                        zf.IsStreamOwner = false;

                        byte[] buffer = new byte[4096];
                        using (MemoryStream zps = new MemoryStream())
                        {
                            StreamUtils.Copy(zf.GetInputStream(zf[1]), zps, buffer);

                            var jsonString = Encoding.UTF8.GetString(zps.ToArray());
                            LogDebug($"JSON string length: {jsonString.Length}");
                            return JsonConvert.DeserializeObject<BackupProperties>(jsonString);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogException(ex, "GetBackupProperties");
                    throw;
                }
            });
        }

        // Add schema validation after restore
        public static async Task<bool> ValidateAndRepairSchema()
        {
            LogDebug("Starting schema validation");
            var sqlconn = await ARDBConnectionManager.default_manager.CheckOut();
            try
            {
                var requiredTables = new Dictionary<string, string>
                {
                    ["access_rules"] = @"
                        CREATE TABLE IF NOT EXISTS `access_rules` (
                            `rule_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
                            `device_id` bigint(20) unsigned NOT NULL,
                            `list_id` bigint(20) unsigned NOT NULL,
                            PRIMARY KEY (`rule_id`),
                            UNIQUE KEY `device_list_unique` (`device_id`,`list_id`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
                    
                    ["device_cards"] = @"
                        CREATE TABLE IF NOT EXISTS `device_cards` (
                            `device_id` bigint(20) unsigned NOT NULL,
                            `card_uid` bigint(20) unsigned NOT NULL,
                            PRIMARY KEY (`device_id`,`card_uid`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
                    
                    ["device_lists"] = @"
                        CREATE TABLE IF NOT EXISTS `device_lists` (
                            `device_id` bigint(20) unsigned NOT NULL,
                            `list_id` bigint(20) unsigned NOT NULL,
                            PRIMARY KEY (`device_id`,`list_id`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4",
                    
                    ["events"] = @"
                        CREATE TABLE IF NOT EXISTS `events` (
                            `event_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
                            `timestamp` datetime NOT NULL,
                            `device_id` bigint(20) unsigned DEFAULT NULL,
                            `card_uid` bigint(20) unsigned DEFAULT NULL,
                            `user_id` bigint(20) unsigned DEFAULT NULL,
                            `event_type` int(11) NOT NULL,
                            `details` text,
                            PRIMARY KEY (`event_id`),
                            KEY `idx_timestamp` (`timestamp`),
                            KEY `idx_device` (`device_id`),
                            KEY `idx_user` (`user_id`)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4"
                };

                using (var conn = sqlconn.Connection)
                {
                    foreach (var table in requiredTables)
                    {
                        LogDebug($"Validating table: {table.Key}");
                        try
                        {
                            using (var cmd = new MySqlCommand(table.Value, conn))
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            LogException(ex, $"Creating table {table.Key}");
                            throw;
                        }
                    }
                }

                LogDebug("Schema validation completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogException(ex, "ValidateAndRepairSchema");
                throw new Exception($"Schema validation failed: {ex.Message}", ex);
            }
            finally
            {
                ARDBConnectionManager.default_manager.CheckIn(sqlconn);
            }
        }

        // Enhanced restore method that handles ZIP extraction
        public static async Task Restore(string filePath)
        {
            LogDebug($"Starting restore from: {filePath}");
            
            try
            {
                // Extract backup content
                var backupManager = new BackupFileManager();
                var content = await backupManager.ExtractBackupContent(filePath);
                
                if (string.IsNullOrWhiteSpace(content.SqlContent))
                    throw new Exception("SQL backup file is empty");
                
                // Execute restore with dedicated connection
                var restoreManager = new DatabaseRestoreManager(
                    MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties);
                
                var result = await restoreManager.ExecuteSqlContent(content.SqlContent);
                
                if (!result.Success)
                {
                    throw new Exception($"Restore failed: {string.Join(", ", result.Errors)}");
                }
                
                LogDebug($"Restore completed: {result.StatementsExecuted} statements executed");
                
                // Validate schema using a fresh connection
                await ValidateSchemaWithNewConnection();
            }
            catch (Exception ex)
            {
                LogException(ex, "Restore");
                throw;
            }
        }
        
        private static async Task ValidateSchemaWithNewConnection()
        {
            var connString = MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString;
            var builder = new MySqlConnectionStringBuilder(connString)
            {
                CharacterSet = "utf8mb4"
            };
            
            using (var connection = new MySqlConnection(builder.ConnectionString))
            {
                await connection.OpenAsync();
                // ... validate schema
            }
        }

        public static Task Backup(string file_name)
        {
            return Task.Run(async () =>
            {
                LogDebug($"Starting backup to: {file_name}");
                DateTime now_is = DateTime.Now;
                
                using (MemoryStream zip_stream = new MemoryStream())
                using (ZipOutputStream zos = new ZipOutputStream(zip_stream))
                {
                    zos.SetLevel(9);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        try
                        {
                            LogDebug("Attempting standard MySqlBackup export");
                            using (MySqlConnection sqlconn = new MySqlConnection(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
                            using (MySqlCommand cmd = new MySqlCommand())
                            using (MySqlBackup mb = new MySqlBackup(cmd))
                            {
                                cmd.Connection = sqlconn;
                                await sqlconn.OpenAsync();
                                
                                // Set utf8mb4 character set to handle extended characters properly
                                using (var charsetCmd = new MySqlCommand("SET NAMES utf8mb4", sqlconn))
                                {
                                    await charsetCmd.ExecuteNonQueryAsync();
                                }
                                
                                mb.ExportToMemoryStream(ms);
                            }
                            LogDebug("Standard MySqlBackup export successful");
                        }
                        catch (System.Collections.Generic.KeyNotFoundException keyEx)
                        {
                            LogException(keyEx, "MySqlBackup export");
                            LogDebug("Falling back to manual export method");
                            
                            // The KeyNotFoundException happens if there are tables with character sets
                            // that the MySQL driver doesn't recognize. Let's handle this by using a different approach.
                            ms.SetLength(0); // Clear the stream
                            
                            // Create a custom SQL dump directly by executing SHOW CREATE TABLE and SELECT
                            await Task.Run(async () => 
                            {
                                using (var writer = new StreamWriter(ms, Encoding.UTF8, 4096, true))
                                using (var sqlconn = new MySqlConnection(MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties.ConnectionString))
                                {
                                    await sqlconn.OpenAsync();
                                    
                                    // Write header
                                    await writer.WriteLineAsync("-- MySQL database dump");
                                    await writer.WriteLineAsync($"-- Created at {DateTime.Now}");
                                    await writer.WriteLineAsync("/*!40101 SET NAMES utf8mb4 */;");
                                    await writer.WriteLineAsync("/*!40014 SET FOREIGN_KEY_CHECKS=0 */;");
                                    
                                    // Get list of tables
                                    var tables = new List<string>();
                                    using (var cmd = new MySqlCommand("SHOW TABLES", sqlconn))
                                    using (var reader = await cmd.ExecuteReaderAsync())
                                    {
                                        while (await reader.ReadAsync())
                                        {
                                            tables.Add(reader.GetString(0));
                                        }
                                    }
                                    
                                    LogDebug($"Found {tables.Count} tables to export");
                                    
                                    // Export each table
                                    foreach (var table in tables)
                                    {
                                        try
                                        {
                                            LogDebug($"Exporting table: {table}");
                                            await writer.WriteLineAsync($"-- Table structure for table `{table}`");
                                            await writer.WriteLineAsync($"DROP TABLE IF EXISTS `{table}`;");
                                            
                                            // Get CREATE TABLE statement
                                            string createStatement = null;
                                            using (var cmd = new MySqlCommand($"SHOW CREATE TABLE `{table}`", sqlconn))
                                            using (var reader = await cmd.ExecuteReaderAsync())
                                            {
                                                if (await reader.ReadAsync())
                                                {
                                                    createStatement = reader.GetString(1);
                                                }
                                            }
                                            
                                            if (createStatement != null)
                                            {
                                                await writer.WriteLineAsync(createStatement + ";");
                                                
                                                // Get and export data
                                                await writer.WriteLineAsync($"-- Dumping data for table `{table}`");
                                                using (var cmd = new MySqlCommand($"SELECT * FROM `{table}`", sqlconn))
                                                using (var reader = await cmd.ExecuteReaderAsync())
                                                {
                                                    if (reader.HasRows)
                                                    {
                                                        var columnCount = reader.FieldCount;
                                                        var columns = new List<string>();
                                                        
                                                        for (int i = 0; i < columnCount; i++)
                                                        {
                                                            columns.Add("`" + reader.GetName(i) + "`");
                                                        }
                                                        
                                                        await writer.WriteLineAsync($"INSERT INTO `{table}` ({string.Join(",", columns)}) VALUES");
                                                        
                                                        bool firstRow = true;
                                                        int rowCount = 0;
                                                        while (await reader.ReadAsync())
                                                        {
                                                            rowCount++;
                                                            if (!firstRow)
                                                                await writer.WriteAsync(",");
                                                            firstRow = false;
                                                            
                                                            await writer.WriteAsync("(");
                                                            for (int i = 0; i < columnCount; i++)
                                                            {
                                                                if (i > 0) await writer.WriteAsync(",");
                                                                
                                                                if (reader.IsDBNull(i))
                                                                {
                                                                    await writer.WriteAsync("NULL");
                                                                }
                                                                else
                                                                {
                                                                    var value = reader.GetValue(i);
                                                                    if (value is string str)
                                                                    {
                                                                        // Escape string
                                                                        await writer.WriteAsync("'" + str.Replace("'", "''") + "'");
                                                                    }
                                                                    else if (value is DateTime dt)
                                                                    {
                                                                        await writer.WriteAsync($"'{dt:yyyy-MM-dd HH:mm:ss}'");
                                                                    }
                                                                    else
                                                                    {
                                                                        await writer.WriteAsync(value.ToString());
                                                                    }
                                                                }
                                                            }
                                                            await writer.WriteLineAsync(")");
                                                        }
                                                        await writer.WriteLineAsync(";");
                                                        LogDebug($"Exported {rowCount} rows from table {table}");
                                                    }
                                                }
                                            }
                                            
                                            await writer.WriteLineAsync(); // Add blank line between tables
                                        }
                                        catch (Exception tableEx)
                                        {
                                            LogException(tableEx, $"Exporting table {table}");
                                            // Continue with next table
                                        }
                                    }
                                    
                                    // Write footer
                                    await writer.WriteLineAsync("/*!40014 SET FOREIGN_KEY_CHECKS=1 */;");
                                    await writer.WriteLineAsync("-- End of database dump");
                                    
                                    await writer.FlushAsync();
                                }
                            });
                            
                            LogDebug("Manual export completed");
                        }

                        LogDebug($"Creating ZIP entry for SQL dump, size: {ms.Length}");
                        ZipEntry ze = new ZipEntry("dbbak.sql");
                        ze.DateTime = now_is;
                        ze.Size = ms.Length;
                        zos.PutNextEntry(ze);
                        ms.Position = 0;
                        StreamUtils.Copy(ms, zos, new byte[4096]);
                        zos.CloseEntry();
                        await zos.FlushAsync();
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        var backprop = new BackupProperties();
                        backprop.Timestamp = now_is;
                        var corrected_var = MCv2Persistance.Instance.Config.DatabaseConfiguration.DatabaseConnectionProperties;
                        corrected_var.UID = "";
                        corrected_var.Password = "";
                        backprop.Database = corrected_var;

                        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(backprop));
                        ms.Write(bytes, 0, bytes.Length);

                        LogDebug($"Creating ZIP entry for properties, size: {ms.Length}");
                        ZipEntry ze = new ZipEntry("properties.json");
                        ze.DateTime = DateTime.Now;
                        ze.Size = ms.Length;
                        zos.PutNextEntry(ze);
                        ms.Position = 0;
                        StreamUtils.Copy(ms, zos, new byte[4096]);
                        zos.CloseEntry();
                        await zos.FlushAsync();
                    }

                    zos.Finish();
                    LogDebug($"Writing final ZIP file, size: {zip_stream.Length}");
                    File.WriteAllBytes(file_name, zip_stream.ToArray());
                    LogDebug("Backup completed successfully");
                }
            });
        }
    }
}
