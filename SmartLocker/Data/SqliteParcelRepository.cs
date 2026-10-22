using System;
using System.Collections.Generic;
using System.Data.SQLite;
using SmartLocker.Models;

namespace SmartLocker.Data
{
    // all database work with sqlite, every query uses parameters (@...)
    public class SqliteParcelRepository : IParcelRepository
    {
        private readonly string connectionString;

        // protects the database from the ui and worker threads
        private static readonly object dbLock = new object();

        public SqliteParcelRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        private SQLiteConnection OpenConnection()
        {
            SQLiteConnection connection = new SQLiteConnection(connectionString);
            connection.Open();
            return connection;
        }

        public int AddParcel(Parcel parcel)
        {
            lock (dbLock)
            {
                using (SQLiteConnection connection = OpenConnection())
                {
                    return InsertParcel(connection, null, parcel);
                }
            }
        }

        // used by the dataset generator, all rows in one transaction
        public void AddParcels(List<Parcel> parcels)
        {
            lock (dbLock)
            {
                using (SQLiteConnection connection = OpenConnection())
                using (SQLiteTransaction transaction = connection.BeginTransaction())
                {
                    foreach (Parcel parcel in parcels)
                    {
                        InsertParcel(connection, transaction, parcel);
                    }
                    transaction.Commit();
                }
            }
        }

        private int InsertParcel(SQLiteConnection connection, SQLiteTransaction transaction, Parcel parcel)
        {
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"INSERT INTO Parcels (Recipient, Size, ServiceType, Status, CompartmentId, PickupCode)
                                        VALUES (@recipient, @size, @service, @status, NULL, NULL);
                                        SELECT last_insert_rowid();";   // returns the new id
                command.Parameters.AddWithValue("@recipient", parcel.Recipient);
                command.Parameters.AddWithValue("@size", parcel.Size.ToString());
                command.Parameters.AddWithValue("@service", parcel.ServiceType.ToString());
                command.Parameters.AddWithValue("@status", ParcelStatus.Waiting.ToString());

                int newId = Convert.ToInt32(command.ExecuteScalar());
                parcel.Id = newId;
                parcel.Status = ParcelStatus.Waiting;
                return newId;
            }
        }

        public List<Parcel> GetAllParcels()
        {
            List<Parcel> parcels = new List<Parcel>();

            lock (dbLock)
            {
                using (SQLiteConnection connection = OpenConnection())
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Id, Recipient, Size, ServiceType, Status, CompartmentId, PickupCode FROM Parcels ORDER BY Id";
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            parcels.Add(ReadParcel(reader));
                        }
                    }
                }
            }

            return parcels;
        }

        // makes an ExpressParcel or StandardParcel from one row
        private Parcel ReadParcel(SQLiteDataReader reader)
        {
            string recipient = reader.GetString(1);
            ParcelSize size = (ParcelSize)Enum.Parse(typeof(ParcelSize), reader.GetString(2));
            ServiceType service = (ServiceType)Enum.Parse(typeof(ServiceType), reader.GetString(3));

            Parcel parcel;
            if (service == ServiceType.Express)
            {
                parcel = new ExpressParcel(recipient, size);
            }
            else
            {
                parcel = new StandardParcel(recipient, size);
            }

            parcel.Id = reader.GetInt32(0);
            parcel.Status = (ParcelStatus)Enum.Parse(typeof(ParcelStatus), reader.GetString(4));

            if (reader.IsDBNull(5))
            {
                parcel.CompartmentId = null;
            }
            else
            {
                parcel.CompartmentId = reader.GetInt32(5);
            }

            if (reader.IsDBNull(6))
            {
                parcel.PickupCode = null;
            }
            else
            {
                parcel.PickupCode = reader.GetString(6);
            }

            return parcel;
        }

        // loads the boxes and marks which ones are taken
        public LockerUnit LoadLockerUnit()
        {
            LockerUnit unit = new LockerUnit();

            lock (dbLock)
            {
                using (SQLiteConnection connection = OpenConnection())
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT Id, Size FROM Compartments ORDER BY Id";
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int id = reader.GetInt32(0);
                            ParcelSize size = (ParcelSize)Enum.Parse(typeof(ParcelSize), reader.GetString(1));
                            unit.AddCompartment(id, size);
                        }
                    }
                }
            }

            unit.ApplyOccupancy(GetAllParcels());
            return unit;
        }

        public int CountParcels()
        {
            lock (dbLock)
            {
                using (SQLiteConnection connection = OpenConnection())
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Parcels";
                    return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public bool IsPickupCodeInUse(string pickupCode)
        {
            lock (dbLock)
            {
                using (SQLiteConnection connection = OpenConnection())
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) FROM Parcels WHERE PickupCode = @code AND Status = 'Stored'";
                    command.Parameters.AddWithValue("@code", pickupCode);
                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
        }

        // box + code + stored status in one transaction
        // if any check fails we rollback and nothing changes
        public bool StoreParcel(int parcelId, int compartmentId, string pickupCode)
        {
            lock (dbLock)
            {
                using (SQLiteConnection connection = OpenConnection())
                using (SQLiteTransaction transaction = connection.BeginTransaction())
                {
                    // make sure the box is free
                    using (SQLiteCommand check = connection.CreateCommand())
                    {
                        check.Transaction = transaction;
                        check.CommandText = "SELECT COUNT(*) FROM Parcels WHERE CompartmentId = @box AND Status = 'Stored'";
                        check.Parameters.AddWithValue("@box", compartmentId);
                        if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                        {
                            transaction.Rollback();
                            return false;
                        }
                    }

                    // make sure the code is not already in use
                    using (SQLiteCommand check = connection.CreateCommand())
                    {
                        check.Transaction = transaction;
                        check.CommandText = "SELECT COUNT(*) FROM Parcels WHERE PickupCode = @code AND Status = 'Stored'";
                        check.Parameters.AddWithValue("@code", pickupCode);
                        if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                        {
                            transaction.Rollback();
                            return false;
                        }
                    }

                    // only works if the parcel is still waiting
                    int changedRows;
                    using (SQLiteCommand update = connection.CreateCommand())
                    {
                        update.Transaction = transaction;
                        update.CommandText = @"UPDATE Parcels
                                               SET Status = 'Stored', CompartmentId = @box, PickupCode = @code
                                               WHERE Id = @id AND Status = 'Waiting'";
                        update.Parameters.AddWithValue("@box", compartmentId);
                        update.Parameters.AddWithValue("@code", pickupCode);
                        update.Parameters.AddWithValue("@id", parcelId);
                        changedRows = update.ExecuteNonQuery();
                    }

                    if (changedRows != 1)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    transaction.Commit();
                    return true;
                }
            }
        }

        // stored -> collected in one transaction
        // wrong or already used code -> rollback, nothing changes
        public Parcel CollectParcel(string pickupCode)
        {
            lock (dbLock)
            {
                using (SQLiteConnection connection = OpenConnection())
                using (SQLiteTransaction transaction = connection.BeginTransaction())
                {
                    Parcel parcel = null;

                    using (SQLiteCommand find = connection.CreateCommand())
                    {
                        find.Transaction = transaction;
                        find.CommandText = @"SELECT Id, Recipient, Size, ServiceType, Status, CompartmentId, PickupCode
                                             FROM Parcels WHERE PickupCode = @code AND Status = 'Stored'";
                        find.Parameters.AddWithValue("@code", pickupCode);
                        using (SQLiteDataReader reader = find.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                parcel = ReadParcel(reader);
                            }
                        }
                    }

                    if (parcel == null)
                    {
                        transaction.Rollback();
                        return null;
                    }

                    // the compartment id stays as history, the box is free because the status changed
                    using (SQLiteCommand update = connection.CreateCommand())
                    {
                        update.Transaction = transaction;
                        update.CommandText = "UPDATE Parcels SET Status = 'Collected' WHERE Id = @id AND Status = 'Stored'";
                        update.Parameters.AddWithValue("@id", parcel.Id);
                        if (update.ExecuteNonQuery() != 1)
                        {
                            transaction.Rollback();
                            return null;
                        }
                    }

                    transaction.Commit();
                    parcel.Status = ParcelStatus.Collected;
                    return parcel;
                }
            }
        }
    }
}