using System;
using System.IO;
using RedGate.SQLCompare.Engine;
using System.IO.Compression;


namespace CustomerDatabaseDeploy
{
    class DatabaseDeployment
    {
        public MainWindow m_Form { get; set; }

        public DatabaseDeployment(MainWindow form)
        {
            m_Form = form;
        }

        private string m_TempFolder;

        public void Deploy(string targetServerName, string targetDatabaseName, string sourceNugetPackage)
        {
            // Folder to extract Nuget package to
            m_TempFolder = Path.Combine("C:\\Temp\\", Path.GetFileNameWithoutExtension(sourceNugetPackage));
            
            try
            {
                // Set up data sources
                ConnectionProperties targetConnectionProperties = new ConnectionProperties(targetServerName,
                    targetDatabaseName);
                string sourceScriptsFolder = Path.Combine(m_TempFolder, "db\\state");
                ExtractNugetPackage(sourceNugetPackage);


                using (Database scriptsFolder = new Database(), targetDatabase = new Database())
                {
                    // Sync the schema
                    SchemaSync.DoSchemaSync(sourceScriptsFolder, scriptsFolder, targetConnectionProperties,
                        targetDatabase, m_Form);
                    m_Form.UpdateOutputText("Schema deployment complete");

                    // Sync the static data
                    DataSync.DoDataSync(sourceScriptsFolder, scriptsFolder, targetConnectionProperties, targetDatabase, m_Form);
                    m_Form.UpdateOutputText("Static data deployment complete");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);    
            }

            // Delete extracted scripts folder
            CleanUp();
        }

        public void WriteOutput(string output)
        {
            m_Form.UpdateOutputText(output);
        }

        private void ExtractNugetPackage(string sourceNugetPackage)
        {
            // Delete the temp folder
            if (Directory.Exists(m_TempFolder))
            {
                Directory.Delete(m_TempFolder, true);   
            }
            // Extract the Nuget package to the temp folder
            m_Form.UpdateOutputText(String.Format("Extracting Nuget package to {0}", m_TempFolder));
            ZipFile.ExtractToDirectory(sourceNugetPackage, m_TempFolder);
        }

        private void CleanUp()
        {
            // Delete our temporary files
            Directory.Delete(m_TempFolder, true);
        }

    }
}
