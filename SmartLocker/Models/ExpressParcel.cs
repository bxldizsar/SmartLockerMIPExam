namespace SmartLocker.Models
{
    public class ExpressParcel : Parcel
    {
        public ExpressParcel(string recipient, ParcelSize size) : base(recipient, size)
        {
        }

        public override ServiceType ServiceType
        {
            get { return ServiceType.Express; }
        }

        public override int GetProcessingDelayMs()
        {
            return 300;
        }
    }
}
