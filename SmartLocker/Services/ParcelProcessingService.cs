using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SmartLocker.Data;
using SmartLocker.Models;

namespace SmartLocker.Services
{
    // runs the allocation on ONE background System.Threading.Thread
    public class ParcelProcessingService
    {
        private readonly IParcelRepository repository;
        private readonly Random random = new Random();
        private readonly object threadLock = new object();

        private Thread workerThread;

        // volatile: the ui thread writes it, the worker reads it
        private volatile bool stopRequested;

        // the form subscribes to these, they are raised on the WORKER thread
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

        // returns false if a worker is already running -> never 2 threads at once
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
                workerThread.IsBackground = true;
                workerThread.Name = "ParcelWorker";
                workerThread.Start();
                return true;
            }
        }

        // cooperative stop: the loop checks the flag after the current parcel
        public void RequestStop()
        {
            stopRequested = true;
        }

        // LINQ #1: waiting parcels, express first, then by id
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
                    // fresh data every round, so a collection done meanwhile is seen
                    List<Parcel> parcels = repository.GetAllParcels();
                    LockerUnit lockerUnit = repository.LoadLockerUnit();

                    // first candidate (in priority order) that fits a free box
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
                        // doesnt fit -> stays waiting, try the next one
                    }

                    if (chosenParcel == null)
                    {
                        finishReason = "no waiting parcel fits a free compartment, processing finished";
                        break;
                    }

                    // simulate the work, 300 ms express / 600 ms standard (polymorphism)
                    Thread.Sleep(chosenParcel.GetProcessingDelayMs());

                    string code = GenerateUniqueCode();
                    bool stored = repository.StoreParcel(chosenParcel.Id, chosenBox.Id, code);

                    if (stored)
                    {
                        TraceLogger.LogStored(chosenParcel.Id, chosenBox.Id);
                        ParcelStored?.Invoke("parcel " + chosenParcel.Id + " stored in box " + chosenBox.Id);
                    }
                    // if not stored (very rare conflict) the next round just tries again
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

        // 6 digits, 000000-999999, not used by any other stored parcel right now
        // the unique index + the transaction check in StoreParcel are the final safety net
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
