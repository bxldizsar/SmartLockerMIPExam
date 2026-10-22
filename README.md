# SmartLocker | Parcel terminal

**UMFST Practical Programming Exam – Variant B**

Size-aware box allocation, customer pickup and compartment reuse.

| Duration | Scoring | Allowed tools |
|---|---|---|
| 120 minutes | 20 points | Visual Studio, Copilot, internet/documentation |

**Scenario.** A parcel terminal has 24 physical boxes. Parcels arrive faster than they can be collected. Build a Windows Forms simulator that assigns parcels to compatible empty compartments, issues pickup codes, and releases boxes upon collection.

> **NON-NEGOTIABLE DATASET GATE**
> Implement a working Generate dataset feature, and submit an SQLite database containing at least 100 valid parcel rows. If either condition fails, the examination score is fixed at 1 out of 20, regardless of other functionality.

## General conditions

- Create a C# Windows Forms desktop application. Use a local SQLite database and `System.Threading.Thread` for background work.
- GitHub Copilot, internet resources and documentation are allowed. You must understand your code and be able to explain the implementation.
- Implement at least two meaningful LINQ operations and trace at least two specified event types to a persistent `trace.log` file.
- The application must compile, remain responsive during processing, and persist data across restarts.

## Dataset and terminal specification

- Generate at least 100 Parcel records programmatically and persist them in SQLite.
- At least 30 Small, 30 Medium and 30 Large parcels; at least 20 Standard and 20 Express parcels.
- Each generated parcel starts as Waiting, has a nonempty Recipient, and receives a unique auto-generated Id.
- Create exactly one LockerUnit with 24 distinct compartments: 8 Small, 8 Medium and 8 Large. These 24 records do not count toward the 100 parcels.

## 1. Object-oriented design (3 points)

- Abstract `Parcel`: Id, Recipient, Size, Status, CompartmentId, PickupCode and abstract `GetProcessingDelayMs()`.
- `StandardParcel` returns 600 ms; `ExpressParcel` returns 300 ms.
- `LockerCompartment` contains its Id and Size; `LockerUnit` owns/manages a collection of `LockerCompartment` objects (composition).
- `IParcelRepository` defines database operations; `SqliteParcelRepository` implements them. Use polymorphism, abstraction and encapsulation.

## 2. SQLite database (4 points)

Create `smartlocker.db` automatically if needed. Define both tables and use parameterized SQL:

| Parcels column | SQLite type / constraint |
|---|---|
| Id | INTEGER PRIMARY KEY AUTOINCREMENT |
| Recipient | TEXT NOT NULL |
| Size | TEXT NOT NULL |
| ServiceType | TEXT NOT NULL |
| Status | TEXT NOT NULL |
| CompartmentId | INTEGER NULL |
| PickupCode | TEXT NULL |

| Compartments column | SQLite type / constraint |
|---|---|
| Id | INTEGER PRIMARY KEY |
| Size | TEXT NOT NULL |

- Valid parcel statuses: Waiting, Stored, Collected. A Stored parcel occupies its referenced compartment.
- Persist insertions, allocation (compartment + code + Stored), pickup (Collected) and loading of all data.
- Allocation and collection must be atomic SQLite transactions. Two Stored parcels may never occupy the same compartment.

## 3. Windows Forms interface (2 points)

- Recipient textbox; Size selector (S/M/L); Service selector (Standard/Express); Add parcel; Generate dataset.
- Start processing; Stop processing; pickup-code textbox; Collect parcel.
- Parcel DataGridView and a separate 24-compartment overview showing size and current availability.
- Show Waiting, Stored, Collected and free-compartment counts; validate all inputs.

## 4. Background processing with threads (4 points)

- Start exactly one background `System.Threading.Thread`. Use the parcel-selection rules in Task 5.
- Simulate the chosen parcel using the polymorphic delay; allocate a compartment; generate a six-digit pickup code not currently used by another Stored parcel.
- Commit compartment, pickup code and Stored status to SQLite in one operation; update the UI using `Invoke` or `BeginInvoke`.
- The interface stays responsive. Stop cooperatively after the active parcel finishes; restart must work. `Thread.Abort()` is forbidden.
- Finish automatically when no Waiting parcel fits any currently free compartment.

## 5. Best-fit allocation and collection (4 points)

### Size compatibility and best fit

| Parcel size | Compatible compartment sizes |
|---|---|
| Small | S, M or L |
| Medium | M or L |
| Large | L only |

- Choose the smallest free compatible box; when sizes are equal, choose the compartment with the smallest Id.
- Use explicit size order Small < Medium < Large. Alphabetical string sorting is not valid.
- Express parcels are considered before Standard parcels; within each service type, use ascending parcel Id.
- Skip Waiting parcels that cannot fit; leave them Waiting and try the next candidate. Stop only when no candidate can fit.

### Pickup workflow

- A valid code identifies exactly one Stored parcel. Mark it Collected in SQLite and free its compartment.
- An invalid or already collected code must be rejected without changing data.
- Collected parcels may retain CompartmentId as history, but must not block future allocations.
- A freed box can be assigned to another Waiting parcel on the next processing run.

| Box Id | Box size | Parcel Id / size / service |
|---|---|---|
| 1 | S | 11 / Small / Standard |
| 2 | M | 10 / Medium / Express |
| 3 | L | 12 / Large / Standard |

**Illustrative result:** for waiting parcels 10 (Medium/Express), 11 (Small/Standard), 12 (Large/Standard), 13 (Medium/Standard), boxes 1(S), 2(M), 3(L): parcels 10, 11, 12 are stored in boxes 2, 1, 3 respectively; parcel 13 remains Waiting. After collecting parcel 10, parcel 13 can use box 2.

## 6. LINQ — two required uses (2 points)

- Use LINQ on Parcel objects to filter/order waiting parcels by ServiceType and Id during allocation.
- Use LINQ and GroupBy on compartment/occupancy objects to calculate free and occupied counts by size. SQL GROUP BY alone does not count.

## 7. Trace — two required event types (1 point)

- Configure `System.Diagnostics.Trace` and a `TextWriterTraceListener` writing to `trace.log`.
- Log a parcel being stored in a compartment and a parcel being collected (timestamp, Parcel Id and Compartment Id).
- Flush the log. Never write pickup codes to `trace.log`.

## Submission

- Submit the Git repository URL before the 120-minute deadline.
- Include the Visual Studio solution, all source code, a short README with run instructions, and the completed SQLite database.
- State the exact database file location in the README. The submitted `smartlocker.db` must contain at least 100 parcel rows and be the file actually used by the application.
- Include the dataset generator in source code; the instructor may create a fresh database to test it.
- Include `trace.log` or instructions to generate it during a run. The instructor may request a brief explanation of any implementation detail.

> **Dataset grading rule**
> A missing or non-working generator, fewer than 100 valid parcel rows in the submitted SQLite database, or fake UI-only rows results in a fixed score of 1/20. The 24 compartment rows do not count toward the 100-parcel minimum.

## Pre-submission checklist

- [ ] Verify the real SQLite file has 100 or more main-entity rows and the generator runs on a fresh database.
- [ ] Restart the application and confirm the saved records are still visible.
- [ ] Start, stop and restart the processing thread without UI freezes or errors.
- [ ] Verify both LINQ requirements and both Trace event types in source code and log output.
- [ ] Confirm the repository contains the database, solution, source code and README.

---

# How to run

- C# Windows Forms, .NET Framework 4.8, Visual Studio
- NuGet package: `System.Data.SQLite.Core` (restored automatically on build)

1. Open the solution in Visual Studio.
2. Build > Build Solution.
3. Run with F5.
4. Click **Generate dataset** (adds 120 parcels: 40 Small, 40 Medium, 40 Large, 60 Express, 60 Standard), then **Start processing**.

## Database location

`SmartLocker\smartlocker.db` — in the project folder, next to `SmartLocker.csproj`.
`Program.FindProjectFolder()` walks up from `bin\Debug` to the folder with the `.csproj`, so the app always uses this file. If it is missing, it is created on startup with both tables and the 24 compartments. To test on a fresh database, close the app, delete the file and start again.

## Trace log

`SmartLocker\trace.log` — same folder as the database, created on the first run. It contains `STORED` and `COLLECTED` lines with timestamp, ParcelId and CompartmentId. Pickup codes are never written.

## Where things are in the code

| Requirement | Code |
|---|---|
| Dataset generator | `Services/DatasetGenerator.cs` |
| Background thread | `Services/ParcelProcessingService.cs` |
| LINQ #1 (Where / OrderBy / ThenBy) | `ParcelProcessingService.GetWaitingCandidates()` |
| LINQ #2 (GroupBy) | `LockerUnit.GetOccupancyBySize()` |
| Best fit | `LockerUnit.FindBestCompartment()` |
| Transactions | `SqliteParcelRepository.StoreParcel()`, `CollectParcel()` |
| Trace | `Services/TraceLogger.cs` |
