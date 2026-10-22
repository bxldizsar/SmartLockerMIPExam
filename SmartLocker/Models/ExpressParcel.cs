namespace SmartLocker.Models
{
    // express parcel, it gets processed faster than a standard one
    public class ExpressParcel : Parcel
    {
        // just passes the data to the Parcel constructor
        public ExpressParcel(string recipient, ParcelSize size) : base(recipient, size)
        {
        }

        // this parcel is always express
        public override ServiceType ServiceType
        {
            get { return ServiceType.Express; }
        }

        // express takes 300 ms (standard takes 600)
        public override int GetProcessingDelayMs()
        {
            return 300;
        }
    }
}
