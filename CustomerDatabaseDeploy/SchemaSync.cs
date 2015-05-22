using System;
using System.Data.SqlClient;
using RedGate.Shared.SQL.ExecutionBlock;
using RedGate.SQLCompare.Engine;
using RedGate.SQLCompare.Engine.ReadFromFolder;

namespace CustomerDatabaseDeploy
{
    class SchemaSync
    {
        public static void DoSchemaSync(string sourceScriptsFolder, Database scriptsFolder,
            ConnectionProperties targetConnectionProperties, Database targetDatabase, MainWindow form)
        {
            // Ignore any tSQLt objects in the scripts folder
            var options = Options.Default.Plus(Options.IgnoreTSQLT);

            form.UpdateOutputText(String.Format("Registering Script Folder {0}", sourceScriptsFolder));
            
            // Create a default dbinfo, as not included in a Nuget package
            var dbInfo = new ScriptDatabaseInformation();
            
            // Read the scripts folder
            scriptsFolder.Register(sourceScriptsFolder, dbInfo, options);


            //Output Parser errors in Script files...
            foreach (ParserMessage m in scriptsFolder.ParserMessages)
            {
                form.UpdateOutputText(String.Format("Warning :{0} in {1}, line {2}", m.Type, m.File, m.LineNumber));
            }


            try
            {
                // Read the schema for the target database
                form.UpdateOutputText(String.Format("Registering database " + targetConnectionProperties.DatabaseName));
                targetDatabase.Register(targetConnectionProperties, options);
            }
            catch (SqlException e)
            {
                e.WarnUserAboutDatabaseRegistryFailure(targetConnectionProperties);
            }


            // Compare the scripts folder with the target database
            Differences sourceVsTarget = scriptsFolder.CompareWith(targetDatabase, options);

            // Display the results on the console
            foreach (Difference difference in sourceVsTarget)
            {
                form.UpdateOutputText(String.Format("{0} {1} {2}", (object)difference.Type, (object)difference.DatabaseObjectType,
                    difference.Name));
                difference.Selected = true;
            }

            // From the differences, figure out the work required to sync
            Work work = new Work();

            work.BuildFromDifferences(sourceVsTarget, options, true);

            if (work.Messages.Count > 0)
            {
                // We can now access the messages and warnings
                form.UpdateOutputText("Messages:");

                foreach (Message message in work.Messages)
                {
                    form.UpdateOutputText(message.Text);
                }
            }

            if (work.Warnings.Count > 0)
            {
                form.UpdateOutputText("Warnings:");

                foreach (Message message in work.Warnings)
                {
                    form.UpdateOutputText(message.Text);
                }
            }

            // Disposing the execution block when it's not needed any more is important to ensure
            // that all the temporary files are cleaned up
            using (ExecutionBlock block = work.ExecutionBlock)
            {
                // Finally, use a BlockExecutor to run the SQL against the WidgetProduction database
                BlockExecutor executor = new BlockExecutor();

                executor.ExecuteBlock(block, targetConnectionProperties.ToDBConnectionInformation());
            }
        }
    }
}
