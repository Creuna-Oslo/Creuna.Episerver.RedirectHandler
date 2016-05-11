using Creuna.Episerver.RedirectHandler.Core.Configuration;
using Creuna.Episerver.RedirectHandler.Core.Data;
using EPiServer.Logging.Compatibility;

namespace Creuna.Episerver.RedirectHandler.Core.Upgrade
{
    public static class Upgrader
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Upgrader));

        public static bool Valid { get; set; }

        public static void Start(int version)
        {
            if (version == -1)
                Create();
            else
                Upgrade();
        }
        /// <summary>
        /// Create redirects table and SP for version number
        /// </summary>
        private static void Create()
        {
            var dba = DataAccessBaseEx.GetWorker();

            Log.Info("Create 404 handler redirects table START");
            const string createTableScript = @"CREATE TABLE " + DataAccessBaseEx.RedirectsTable + " ( "
                                             + " [ID] [int] IDENTITY(1,1) NOT NULL, "
                                             + " [OldUrl] [nvarchar](2000) NOT NULL,"
                                             + " [Requested] [datetime] NULL,       "
                                             + " [Referer] [nvarchar](2000) NULL    "
                                             + " ) ON [PRIMARY]";
            var create = dba.ExecuteNonQuery(createTableScript);

            Log.Info("Create 404 handler redirects table END");


            if (create)
            {
                Log.Info("Create 404 handler version SP START");
                string versionSp = @"CREATE PROCEDURE " + DataAccessBaseEx.VersionStoredProc + " AS RETURN " + RedirectConfiguration.CurrentVersion;

                if (!dba.ExecuteNonQuery(versionSp))
                {
                    create = false;
                    Log.Error("An error occured during the creation of the 404 handler version stored procedure. Canceling.");

                    Log.Info("Create 404 handler version SP END");
                }
            }
            Valid = create;
        }

        private static void Upgrade()
        {
            string versionSp = @"ALTER PROCEDURE " + DataAccessBaseEx.VersionStoredProc + " AS RETURN " + RedirectConfiguration.CurrentVersion;
            var dba = DataAccessBaseEx.GetWorker();
            Valid = dba.ExecuteNonQuery(versionSp);
        }



    }
}