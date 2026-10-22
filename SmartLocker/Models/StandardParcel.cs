namespace SmartLocker.Models
{
    // normal parcel, slower than express
    public class StandardParcel : Parcel
    {
        public StandardParcel(string recipient, ParcelSize size) : base(recipient, size)
        {
        }

        public override ServiceType ServiceType
        {
            get { return ServiceType.Standard; }
        }

        // standard takes 600 ms
        public override int GetProcessingDelayMs()
        {
            return 600;
        }
    }
}