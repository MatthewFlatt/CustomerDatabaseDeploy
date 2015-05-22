using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using RedGate.Shared.SQL.ExecutionBlock;
using RedGate.SQLCompare.Engine;
using RedGate.SQLDataCompare.Engine;

namespace CustomerDatabaseDeploy
{
    class DataSync
    {
        public static void DoDataSync(string sourceScriptsFolder, Database scriptsFolder,
                    ConnectionProperties targetConnectionProperties, Database targetDatabase, MainWindow form)
        {
            // Register the source scripts folder from extracted Nuget package
            form.UpdateOutputText(String.Format("Registering Script Folder {0} for Data Compare", sourceScriptsFolder));
            scriptsFolder.RegisterForDataCompare(sourceScriptsFolder, null, Options.Default);

            try
            {
                // Read the schema for the target database
                form.UpdateOutputText(String.Format("Registering database {0} for Data Compare", targetConnectionProperties.DatabaseName));
                targetDatabase.RegisterForDataCompare(targetConnectionProperties, Options.Default);
            }
            catch (SqlException e)
            {
                e.WarnUserAboutDatabaseRegistryFailure(targetConnectionProperties);
            }


            // Create schema mappings
            var smMappings = CreateSchemaMappings(sourceScriptsFolder, scriptsFolder, targetDatabase, form);

            using (var comparisonSession = new ComparisonSession())
            {
                // Compare databases
                comparisonSession.CompareDatabases(scriptsFolder, targetDatabase, smMappings);
                TableDifferences diffs = comparisonSession.TableDifferences;
                // Get diffs and display them
                form.UpdateOutputText("Differences summary:\r\n");
                form.UpdateOutputText("Rows diff.     Table");
                form.UpdateOutputText("====================\r\n");
                foreach (var tableDifference in diffs)
                {
                    if (tableDifference.DifferencesSummary.DifferenceCount() > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(tableDifference.DifferencesSummary.DifferenceCount().ToString().PadRight(15) +
                                  tableDifference.Name);
                        form.UpdateOutputText(sb.ToString());
                    }
                }

                // Using the comparison sesson, calculate work to sync and sync
                SqlProvider provider = new SqlProvider();
                provider.Options = comparisonSession.Options;

                try
                {
                    // Create the script to do the synchronisation
                    ExecutionBlock block = provider.GetMigrationSQL(comparisonSession, true);
                    form.UpdateOutputText(String.Format("Synchronization contains " + block.LineCount + " lines in " + +block.BatchCount +
                                      " batches"));

                    form.UpdateOutputText("Synchronizing Data...");
                    // Run the sync script
                    BlockExecutor executor = new BlockExecutor();
                    executor.ExecuteBlock(block, targetConnectionProperties.ToDBConnectionInformation());
                    block.Dispose();
                }
                catch (Exception ex)
                {
                    form.UpdateOutputText(String.Format("Error synchronizing data:\r\n" + ex.Message));
                    ExecutionBlock block = provider.Block;
                    if (block != null)
                        block.Dispose();
                }
            }
        }

        /// <summary>
        /// Create schema mappings, including only static data tables
        /// </summary>
        /// <param name="sourceScriptsFolder"></param>
        /// <param name="scriptsFolder"></param>
        /// <param name="targetDatabase"></param>
        /// <returns></returns>
        private static SchemaMappings CreateSchemaMappings(string sourceScriptsFolder, Database scriptsFolder,
            Database targetDatabase, MainWindow form)
        {
            var staticDataTables = GetStaticDataTables(sourceScriptsFolder);

            form.UpdateOutputText("Creating Mappings...");

            // Create mappings between source and target, exlcluding everything
            SchemaMappings smMappings = new SchemaMappings();
            smMappings.CreateMappings(scriptsFolder, targetDatabase);

            foreach (var tableMapping in smMappings.TableMappings)
            {
                tableMapping.Include = false;
            }

            // Go through each static data table in our list and match it to a mapping
            foreach (var staticDataTable in staticDataTables)
            {
                foreach (var tableMapping in smMappings.TableMappings)
                {
                    IDatabaseObject databaseTable = tableMapping.Obj2;
                    // Include the table if it can be compared as is one of the static data tables
                    if (databaseTable != null && databaseTable.Name == staticDataTable)
                    {
                        if (tableMapping.Status != TableMappingStatus.UnableToCompare)
                        {
                            tableMapping.Include = true;
                            form.UpdateOutputText(String.Format("Including Static data table {0}", staticDataTable));
                        }
                        else
                        {
                            form.UpdateOutputText(String.Format("Static data table {0} can't be included for comparison", staticDataTable));
                        }
                    }
                }
            }
            return smMappings;
        }

        /// <summary>
        /// Find which tables in our scripts folder contain static data
        /// </summary>
        /// <param name="sourceScriptsFolder"></param>
        /// <returns></returns>
        private static List<string> GetStaticDataTables(string sourceScriptsFolder)
        {
            var staticDataTables = new List<string>();

            // Iterate through all files in the Data directory of scripts folder
            foreach (var file in Directory.GetFiles(Path.Combine(sourceScriptsFolder, "Data")))
            {
                // Only interested in .sql files
                if (Path.GetExtension(file).Equals(".sql"))
                {
                    // Get the filename without extension and split on . to get schema and table name separate
                    var filenameWithoutExtension = Path.GetFileNameWithoutExtension(file).Split('.');
                    // Get the last split which should be tablename
                    var tableName = filenameWithoutExtension[filenameWithoutExtension.Count() - 1];
                    // Add tablename to the list without the _Data suffix
                    staticDataTables.Add(tableName.Substring(0, tableName.Length - 5));
                }
            }
            return staticDataTables;
        }
    }
}
