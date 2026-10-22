namespace SmartLocker.Models
{
    // one box in the locker
    public class LockerCompartment
    {
        // id and size can only be set here, from outside you can just read them
        public int Id { get; private set; }
        public ParcelSize Size { get; private set; }

        // id of the stored parcel inside, null if the box is empty
        public int? OccupiedByParcelId { get; set; }

        // true if there is a parcel in the box
        public bool IsOccupied
        {
            get { return OccupiedByParcelId != null; }
        }

        // a new box is always empty

        public LockerCompartment(int id, ParcelSize size)
        {
            Id = id;
            Size = size;
            OccupiedByParcelId = null;
        }

        // the parcel fits if the box is the same size or bigger
        // we compare the enum numbers (1, 2, 3), not the names
        public bool CanHold(ParcelSize parcelSize)
        {
            return (int)Size >= (int)parcelSize;
        }
    }
}
