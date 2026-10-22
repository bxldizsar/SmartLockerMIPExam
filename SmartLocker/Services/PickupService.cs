using SmartLocker.Data;
using SmartLocker.Models;

namespace SmartLocker.Services
{
    // handles the pickup with the 6 digit code
    public class PickupService
    {
        private readonly IParcelRepository repository;

        public PickupService(IParcelRepository repository)
        {
            this.repository = repository;
        }

        // exactly 6 characters, only digits
        public static bool IsValidCodeFormat(string code)
        {
            if (code == null || code.Length != 6)
            {
                return false;
            }

            foreach (char c in code)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }
            return true;
        }

        // returns the collected parcel, or null if the code is wrong or already used
        public Parcel Collect(string code)
        {
            if (!IsValidCodeFormat(code))
            {
                return null;
            }

            Parcel collected = repository.CollectParcel(code);
            if (collected != null)
            {
                // we log only the ids, never the pickup code
                TraceLogger.LogCollected(collected.Id, collected.CompartmentId.Value);
            }
            return collected;
        }
    }
}