using System;
using System.Collections.Generic;
using SmartLocker.Data;
using SmartLocker.Models;

namespace SmartLocker.Services
{
    // makes 120 real parcels in the database
    // sizes go small, medium, large, small... -> exactly 40 of each
    // service goes express, standard, express... -> exactly 60 of each
    // so the minimum counts (30/30/30 and 20/20) are guaranteed, not random
    public class DatasetGenerator
    {
        public const int ParcelCount = 120;

        private readonly IParcelRepository repository;

        private static readonly string[] firstNames =
        {
            "Andrei", "Maria", "Istvan", "Anna", "Mihai", "Eszter", "Gabor", "Ioana",
            "Peter", "Elena", "Zoltan", "Diana", "Laszlo", "Cristina", "Bence", "Alexandra"
        };

        private static readonly string[] lastNames =
        {
            "Popescu", "Kovacs", "Ionescu", "Nagy", "Szabo", "Moldovan", "Toth", "Pop",
            "Farkas", "Rusu", "Balogh", "Muresan"
        };

        public DatasetGenerator(IParcelRepository repository)
        {
            this.repository = repository;
        }

        public int Generate()
        {
            List<Parcel> parcels = new List<Parcel>();
            Random random = new Random();
            ParcelSize[] sizes = { ParcelSize.Small, ParcelSize.Medium, ParcelSize.Large };

            for (int i = 0; i < ParcelCount; i++)
            {
                string recipient = firstNames[random.Next(firstNames.Length)] + " " +
                                   lastNames[random.Next(lastNames.Length)];
                ParcelSize size = sizes[i % 3];

                Parcel parcel;
                if (i % 2 == 0)
                {
                    parcel = new ExpressParcel(recipient, size);
                }
                else
                {
                    parcel = new StandardParcel(recipient, size);
                }

                parcels.Add(parcel);
            }

            // ids come from sqlite autoincrement, everything starts as Waiting
            repository.AddParcels(parcels);
            return parcels.Count;
        }
    }
}
