﻿using System;
using System.Data.SqlClient;
using RedGate.SQLCompare.Engine;

namespace CustomerDatabaseDeploy
{
    static class SqlExceptionExentension
    {
        public static void WarnUserAboutDatabaseRegistryFailure(this SqlException e, ConnectionProperties sourceConnectionProperties)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(@"
Cannot connect to database '{0}' on server '{1}'. The most common causes of this error are:
        o The sample databases are not installed
        o ServerName not set to the location of the target database
        o For sql server authentication, username and password incorrect or not supplied in ConnectionProperties constructor
        o Remote connections not enabled", sourceConnectionProperties.DatabaseName,
                sourceConnectionProperties.ServerName);
        }

    }
}