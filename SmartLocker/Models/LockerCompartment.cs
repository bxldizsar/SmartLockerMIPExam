namespace SmartLocker.Models
{
    public class LockerCompartment
    {
        public int Id { get; private set; }
        public ParcelSize Size { get; private set; }

        // which stored parcel is inside, null if the box is free
        public int? OccupiedByParcelId { get; set; }

        public bool IsOccupied
        {
            get { return OccupiedByParcelId != null; }
        }

        public LockerCompartment(int id, ParcelSize size)
        {
            Id = id;
            Size = size;
            OccupiedByParcelId = null;
        }

        // a box fits if its at least as big as the parcel (uses the enum numbers)
        public bool CanHold(ParcelSize parcelSize)
        {
            return (int)Size >= (int)parcelSize;
        }
    }
}
