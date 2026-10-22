using System.Data.SQLite;
using SmartLocker.Models;

namespace SmartLocker.Data
{
    // creates the tables and compartments
    public static class DatabaseInitializer
    {
        public static void Initialize(string connectionString)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                // if smartlocker.db doesnt exist yet, this creates it
                connection.Open();

                string createTables = @"
                    CREATE TABLE IF NOT EXISTS Compartments (
                        Id   INTEGER PRIMARY KEY,
                        Size TEXT NOT NULL CHECK (Size IN ('Small','Medium','Large'))
                    );

                    CREATE TABLE IF NOT EXISTS Parcels (
                        Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                        Recipient     TEXT NOT NULL CHECK (length(trim(Recipient)) > 0),
                        Size          TEXT NOT NULL CHECK (Size IN ('Small','Medium','Large')),
                        ServiceType   TEXT NOT NULL CHECK (ServiceType IN ('Standard','Express')),
                        Status        TEXT NOT NULL CHECK (Status IN ('Waiting','Stored','Collected')),
                        CompartmentId INTEGER NULL REFERENCES Compartments(Id),
                        PickupCode    TEXT NULL
                    );

                    -- two stored parcels cant be in the same box
                    CREATE UNIQUE INDEX IF NOT EXISTS UX_Parcels_StoredCompartment
                        ON Parcels(CompartmentId) WHERE Status = 'Stored';

                    -- two stored parcels cant have the same pickup code
                    CREATE UNIQUE INDEX IF NOT EXISTS UX_Parcels_StoredPickupCode
                        ON Parcels(PickupCode) WHERE Status = 'Stored';";

                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = createTables;
                    command.ExecuteNonQuery();
                }

                // adds the 24 boxes, INSERT OR IGNORE skips them if they already exist
                LockerUnit layout = LockerUnit.CreateStandardLayout();
                using (SQLiteTransaction transaction = connection.BeginTransaction())
                {
                    foreach (LockerCompartment compartment in layout.Compartments)
                    {
                        using (SQLiteCommand insert = connection.CreateCommand())
                        {
                            insert.Transaction = transaction;
                            insert.CommandText = "INSERT OR IGNORE INTO Compartments (Id, Size) VALUES (@id, @size)";
                            insert.Parameters.AddWithValue("@id", compartment.Id);
                            insert.Parameters.AddWithValue("@size", compartment.Size.ToString());
                            insert.ExecuteNonQuery();
                        }
                    }
                    transaction.Commit();
                }
            }
        }
    }
}