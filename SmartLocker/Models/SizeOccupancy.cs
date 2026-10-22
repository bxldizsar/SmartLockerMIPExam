namespace SmartLocker.Models
{
    // one row of the "free / occupied by size" summary
    public class SizeOccupancy
    {
        public ParcelSize Size { get; set; }
        public int Free { get; set; }
        public int Occupied { get; set; }
    }
}
