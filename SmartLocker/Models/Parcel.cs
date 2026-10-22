using System;

namespace SmartLocker.Models
{
    // base class, you cant create a Parcel directly, only Standard or Express
    public abstract class Parcel
    {
        public int Id { get; set; }
        public string Recipient { get; set; }
        public ParcelSize Size { get; set; }
        public ParcelStatus Status { get; set; }
        public int? CompartmentId { get; set; }   // null while waiting
        public string PickupCode { get; set; }

        public abstract ServiceType ServiceType { get; }

        // every new parcel starts as waiting, with no box and no code
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

        // each subclass gives its own delay (polymorphism)
        public abstract int GetProcessingDelayMs();
    }
}