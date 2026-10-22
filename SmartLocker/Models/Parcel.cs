using System;

namespace SmartLocker.Models
{
    // base class for every parcel, cant be created directly (abstract)
    public abstract class Parcel
    {
        public int Id { get; set; }
        public string Recipient { get; set; }
        public ParcelSize Size { get; set; }
        public ParcelStatus Status { get; set; }
        public int? CompartmentId { get; set; }
        public string PickupCode { get; set; }

        // each subclass says which service it is
        public abstract ServiceType ServiceType { get; }

        protected Parcel(string recipient, ParcelSize size)
        {
            if (string.IsNullOrWhiteSpace(recipient))
            {
                throw new ArgumentException("recipient cant be empty");
            }

            Recipient = recipient.Trim();
            Size = size;
            Status = ParcelStatus.Waiting;
            CompartmentId = null;
            PickupCode = null;
        }

        // polymorphism: standard and express give different delays
        public abstract int GetProcessingDelayMs();
    }
}
