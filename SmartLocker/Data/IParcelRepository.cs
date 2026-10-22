using System.Collections.Generic;
using SmartLocker.Models;

namespace SmartLocker.Data
{
    // what the database can do, the sqlite class says how
    public interface IParcelRepository
    {
        int AddParcel(Parcel parcel);
        void AddParcels(List<Parcel> parcels);
        List<Parcel> GetAllParcels();
        LockerUnit LoadLockerUnit();
        int CountParcels();
        bool IsPickupCodeInUse(string pickupCode);

        // waiting -> stored in one transaction, false if something is already taken
        bool StoreParcel(int parcelId, int compartmentId, string pickupCode);

        // stored -> collected, null if the code is wrong or already used
        Parcel CollectParcel(string pickupCode);
    }
}