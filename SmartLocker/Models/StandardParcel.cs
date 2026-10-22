namespace SmartLocker.Models
{
    public class StandardParcel : Parcel
    {
        public StandardParcel(string recipient, ParcelSize size) : base(recipient, size)
        {
        }

        public override ServiceType ServiceType
        {
            get { return ServiceType.Standard; }
        }

        public override int GetProcessingDelayMs()
        {
            return 600;
        }
    }
}
