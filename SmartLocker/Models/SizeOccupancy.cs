namespace SmartLocker.Models
{
    // one row of the GroupBy result: how many boxes are free / taken for one size
    public class SizeOccupancy
    {
        public ParcelSize Size { get; set; }
        public int Free { get; set; }
        public int Occupied { get; set; }
    }
}
