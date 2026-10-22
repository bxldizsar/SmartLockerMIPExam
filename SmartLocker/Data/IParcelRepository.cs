using System.Collections.Generic;
using SmartLocker.Models;

namespace SmartLocker.Data
{
    // what the app needs from the database, without saying HOW (abstraction)
    public interface IParcelRepository
    {
        int AddParcel(Parcel parcel);
        void AddParcels(List<Parcel> parcels);
        List<Parcel> GetAllParcels();
        LockerUnit LoadLockerUnit();
        int CountParcels();
        bool IsPickupCodeInUse(string pickupCode);

        // waiting -> stored, compartment + code in one transaction
        bool StoreParcel(int parcelId, int compartmentId, string pickupCode);

        // stored -> collected in one transaction, returns null if the code is not valid
        Parcel CollectParcel(string pickupCode);
    }
}
