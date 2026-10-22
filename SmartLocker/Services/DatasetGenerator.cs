using System;
using System.Collections.Generic;
using SmartLocker.Data;
using SmartLocker.Models;

namespace SmartLocker.Services
{
    // makes 120 real parcels and saves them in the database
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
                // only the name is random
                string recipient = firstNames[random.Next(firstNames.Length)] + " " +
                                   lastNames[random.Next(lastNames.Length)];

                // small, medium, large, small... -> exactly 40 of each
                ParcelSize size = sizes[i % 3];

                // express, standard, express... -> exactly 60 of each
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

            // sqlite gives the ids, every parcel starts as waiting
            repository.AddParcels(parcels);
            return parcels.Count;
        }
    }
}