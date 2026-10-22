using System.Collections.Generic;
using System.Linq;

namespace SmartLocker.Models
{
    // the whole locker, it creates and owns all the boxes (composition)
    public class LockerUnit
    {
        public const int SmallCount = 8;
        public const int MediumCount = 8;
        public const int LargeCount = 8;

        // private list, only this class can add boxes
        private readonly List<LockerCompartment> compartments = new List<LockerCompartment>();

        // other classes can read the boxes but cant change the list
        public IReadOnlyList<LockerCompartment> Compartments
        {
            get { return compartments; }
        }

        // the locker makes the box object itself
        public void AddCompartment(int id, ParcelSize size)
        {
            compartments.Add(new LockerCompartment(id, size));
        }

        // builds the default locker: ids 1-8 small, 9-16 medium, 17-24 large
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

        // marks which boxes are taken, based on the parcel list

        public void ApplyOccupancy(List<Parcel> parcels)
        {
            // first we set every box to free
            foreach (LockerCompartment compartment in compartments)
            {
                compartment.OccupiedByParcelId = null;
            }

            
            // only stored parcels take a box
            // collected ones keep their old box id, but the box is free again

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
        // finds a box by its id, null if there is no such box
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

        // counts the empty boxes
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

        // best fit: the smallest free box where the parcel fits
        // if two boxes have the same size, the one with the smaller id wins
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
        // linq #2: groups the boxes by size and counts free and occupied ones
        // this is done in c#, not with sql group by
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
