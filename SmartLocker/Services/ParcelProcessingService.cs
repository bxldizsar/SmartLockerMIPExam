using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SmartLocker.Data;
using SmartLocker.Models;

namespace SmartLocker.Services
{
    // puts the waiting parcels into boxes on one background thread
    public class ParcelProcessingService
    {
        private readonly IParcelRepository repository;
        private readonly Random random = new Random();
        private readonly object threadLock = new object();

        private Thread workerThread;

        // volatile: the ui thread sets it, the worker thread reads it
        private volatile bool stopRequested;

        // the form listens to these, they run on the worker thread
        public event Action<string> ParcelStored;
        public event Action<string> ProcessingFinished;

        public ParcelProcessingService(IParcelRepository repository)
        {
            this.repository = repository;
        }

        public bool IsRunning
        {
            get
            {
                Thread t = workerThread;
                return t != null && t.IsAlive;
            }
        }

        // starts the thread, returns false if it is already running (never 2 threads)
        public bool Start()
        {
            lock (threadLock)
            {
                if (IsRunning)
                {
                    return false;
                }

                stopRequested = false;
                workerThread = new Thread(ProcessLoop);
                workerThread.IsBackground = true;   // closes together with the app
                workerThread.Name = "ParcelWorker";
                workerThread.Start();
                return true;
            }
        }

        // the loop stops after the current parcel, no Thread.Abort
        public void RequestStop()
        {
            stopRequested = true;
        }

        // linq #1: only waiting parcels, express first, then by id
        public static List<Parcel> GetWaitingCandidates(List<Parcel> parcels)
        {
            return parcels
                .Where(p => p.Status == ParcelStatus.Waiting)
                .OrderBy(p => p.ServiceType == ServiceType.Express ? 0 : 1)
                .ThenBy(p => p.Id)
                .ToList();
        }

        private void ProcessLoop()
        {
            string finishReason = "processing finished";

            try
            {
                while (!stopRequested)
                {
                    // fresh data every round, so we see parcels collected in the meantime
                    List<Parcel> parcels = repository.GetAllParcels();
                    LockerUnit lockerUnit = repository.LoadLockerUnit();

                    // take the first candidate that fits, the others stay waiting
                    Parcel chosenParcel = null;
                    LockerCompartment chosenBox = null;
                    foreach (Parcel candidate in GetWaitingCandidates(parcels))
                    {
                        LockerCompartment box = lockerUnit.FindBestCompartment(candidate.Size);
                        if (box != null)
                        {
                            chosenParcel = candidate;
                            chosenBox = box;
                            break;
                        }
                    }

                    // nothing fits anymore -> stop by itself
                    if (chosenParcel == null)
                    {
                        finishReason = "no waiting parcel fits a free compartment, processing finished";
                        break;
                    }

                    // 300 or 600 ms, depends on the parcel type (polymorphism)
                    Thread.Sleep(chosenParcel.GetProcessingDelayMs());

                    string code = GenerateUniqueCode();
                    bool stored = repository.StoreParcel(chosenParcel.Id, chosenBox.Id, code);

                    // if it failed (rare conflict) the next round tries again
                    if (stored)
                    {
                        TraceLogger.LogStored(chosenParcel.Id, chosenBox.Id);
                        ParcelStored?.Invoke("parcel " + chosenParcel.Id + " stored in box " + chosenBox.Id);
                    }
                }

                if (stopRequested)
                {
                    finishReason = "processing stopped";
                }
            }
            catch (Exception ex)
            {
                finishReason = "processing error: " + ex.Message;
            }

            ProcessingFinished?.Invoke(finishReason);
        }

        // 6 digits (D6 keeps the zeros, like 004213), not used by another stored parcel
        private string GenerateUniqueCode()
        {
            string code;
            do
            {
                code = random.Next(0, 1000000).ToString("D6");
            }
            while (repository.IsPickupCodeInUse(code));

            return code;
        }
    }
}