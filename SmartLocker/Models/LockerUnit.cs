using System.Collections.Generic;
using System.Linq;

namespace SmartLocker.Models
{
    // composition: the locker unit creates and owns its compartments
    public class LockerUnit
    {
        public const int SmallCount = 8;
        public const int MediumCount = 8;
        public const int LargeCount = 8;

        private readonly List<LockerCompartment> compartments = new List<LockerCompartment>();

        // outside code can read the list but cant add/remove from it
        public IReadOnlyList<LockerCompartment> Compartments
        {
            get { return compartments; }
        }

        public void AddCompartment(int id, ParcelSize size)
        {
            compartments.Add(new LockerCompartment(id, size));
        }

        // the default terminal: ids 1-8 small, 9-16 medium, 17-24 large
        public static LockerUnit CreateStandardLayout()
        {
            LockerUnit unit = new LockerUnit();
            int id = 1;
            for (int i = 0; i < SmallCount; i++)
            {
                unit.AddCompartment(id++, ParcelSize.Small);
            }
            for (int i = 0; i < MediumCount; i++)
            {
                unit.AddCompartment(id++, ParcelSize.Medium);
            }
            for (int i = 0; i < LargeCount; i++)
            {
                unit.AddCompartment(id++, ParcelSize.Large);
            }
            return unit;
        }

        // marks boxes as occupied based on the stored parcels
        // collected parcels keep their compartment id as history but dont block the box
        public void ApplyOccupancy(List<Parcel> parcels)
        {
            foreach (LockerCompartment compartment in compartments)
            {
                compartment.OccupiedByParcelId = null;
            }

            foreach (Parcel parcel in parcels)
            {
                if (parcel.Status == ParcelStatus.Stored && parcel.CompartmentId != null)
                {
                    LockerCompartment box = FindById(parcel.CompartmentId.Value);
                    if (box != null)
                    {
                        box.OccupiedByParcelId = parcel.Id;
                    }
                }
            }
        }

        public LockerCompartment FindById(int id)
        {
            foreach (LockerCompartment compartment in compartments)
            {
                if (compartment.Id == id)
                {
                    return compartment;
                }
            }
            return null;
        }

        public int CountFree()
        {
            int free = 0;
            foreach (LockerCompartment compartment in compartments)
            {
                if (!compartment.IsOccupied)
                {
                    free++;
                }
            }
            return free;
        }

        // best fit: smallest free box that can hold the parcel, smaller id wins on a tie
        // returns null if nothing fits right now
        public LockerCompartment FindBestCompartment(ParcelSize parcelSize)
        {
            LockerCompartment best = null;

            foreach (LockerCompartment compartment in compartments)
            {
                if (compartment.IsOccupied || !compartment.CanHold(parcelSize))
                {
                    continue;
                }

                if (best == null)
                {
                    best = compartment;
                }
                else if ((int)compartment.Size < (int)best.Size)
                {
                    best = compartment;
                }
                else if (compartment.Size == best.Size && compartment.Id < best.Id)
                {
                    best = compartment;
                }
            }

            return best;
        }

        // LINQ #2: GroupBy on the compartment objects -> free and occupied count per size
        public List<SizeOccupancy> GetOccupancyBySize()
        {
            return compartments
                .GroupBy(c => c.Size)
                .OrderBy(g => (int)g.Key)
                .Select(g => new SizeOccupancy
                {
                    Size = g.Key,
                    Free = g.Count(c => !c.IsOccupied),
                    Occupied = g.Count(c => c.IsOccupied)
                })
                .ToList();
        }
    }
}
