# CgCar: Vehicle Registry (.NET MAUI)

A .NET MAUI app (Android & iOS) for registering vehicles by category, storing everything locally in SQLite.

- **Dashboard**: one card per category (Cars, Trucks, Tractors) with its vehicle count.
- **Vehicle list**: the vehicles of a category with photo thumbnail, brand/model, plate and year; search; add (+); tap to edit; swipe left to delete.
- **Create / edit**: category, brand, model, plate number, year, comments, photo from the gallery; Save and Delete; validation on every field.

## Running it

Requirements: .NET 10 SDK with the `maui-android` / `maui-ios` workloads. iOS builds need a paired Mac.

```bash
dotnet build src/CgCar.App -t:Run -f net10.0-android   # build and deploy to a running emulator/device
dotnet test                                             # run the unit tests
```

On first launch the database is created with a few demo vehicles (see [DemoData.cs](src/CgCar.Core/Data/DemoData.cs)).

## Solution structure

```
CgCar.slnx
├── src/CgCar.Core        net10.0 class library, no MAUI dependency
│   ├── Models            Vehicle (also the SQLite table), VehicleCategory, LicensePlate
│   ├── Data              IVehicleRepository, VehicleRepository (sqlite-net), DatabaseOptions, DemoData
│   ├── Services          INavigationService, IDialogService, IPhotoService (interfaces only)
│   ├── Navigation        Routes, NavigationParameters, INavigationParameterReceiver
│   └── ViewModels        Dashboard, VehicleList, VehicleEdit (+ CategoryTile, VehicleListItem)
├── src/CgCar.App         MAUI app (Android, iOS)
│   ├── Views             XAML pages + BasePage
│   ├── Services          Shell navigation, dialogs/toasts, MediaPicker photo service
│   └── MauiProgram.cs    dependency injection setup
└── tests/CgCar.Tests     xUnit tests for Core (ViewModels, repository, plate logic)
```

Dependencies point one way: `App → Core` and `Tests → Core`. Core never references MAUI, which is what makes
the ViewModels testable without an emulator and keeps the View / ViewModel / Repository separation enforced
by the compiler rather than by convention.

```
 View (XAML, App)  ──binds to──▶  ViewModel (Core)  ──uses──▶  IVehicleRepository (Core) ──▶ SQLite
                                        │
                                        └──uses──▶  INavigationService / IDialogService / IPhotoService
                                                     (interfaces in Core, implemented in App with MAUI APIs)
```

## How it works

### MVVM
ViewModels use **CommunityToolkit.Mvvm** source generators: `[ObservableProperty]` generates property-changed
notifications, `[RelayCommand]` generates `ICommand`s. Async commands (`AsyncRelayCommand`) are disabled while
running, so double-tapping Save can't insert a vehicle twice. Pages use compiled bindings (`x:DataType`).

List rows and dashboard tiles are small item ViewModels ([VehicleListItem](src/CgCar.Core/ViewModels/VehicleListItem.cs),
[CategoryTileViewModel](src/CgCar.Core/ViewModels/CategoryTileViewModel.cs)) that carry their own commands
(`EditCommand`, `DeleteCommand`, `OpenCommand`), which call back into the page's ViewModel. The row template binds
`{Binding EditCommand}` directly. The alternative, `RelativeSource AncestorType=...ViewModel` (finding the page's
ViewModel by walking up the visual tree), finds nothing while CollectionView recycles a row: every list reload then
threw a `NullReferenceException` inside each binding, which MAUI swallows but which still costs time and stops the
Visual Studio debugger.

### Navigation
Shell routes (`vehicles`, `vehicle-edit`) are registered in [AppShell.xaml.cs](src/CgCar.App/AppShell.xaml.cs);
Shell creates the pages through DI. ViewModels navigate through `INavigationService` and pass parameters
(category or vehicle id). Because Core can't implement MAUI's `IQueryAttributable`, [BasePage](src/CgCar.App/Views/BasePage.cs)
receives Shell's parameters and forwards them to the ViewModel's `INavigationParameterReceiver.ReceiveParameters`.
Parameters are sent as single-use `ShellNavigationQueryParameters` so they aren't re-applied on back navigation.

### Keeping data fresh
Each page reloads its data when it appears (`Appearing` → `LoadCommand` via CommunityToolkit.Maui's
`EventToCommandBehavior`). So after saving or deleting, going back shows up-to-date counts and lists, with a
single cheap query. Note: toolkit behaviors don't inherit the page's `BindingContext`, so it is bound explicitly
(`BindingContext="{Binding BindingContext, Source={x:Reference ThisPage}}"`).
The edit page loads only once, so returning from the gallery doesn't wipe unsaved edits.

### Data (SQLite, sqlite-net-pcl)

| Column | Type | Notes |
|---|---|---|
| Id | int | Primary key, auto increment |
| Category | int (enum) | `Car = 0, Truck = 1, Tractor = 2`; indexed |
| Brand, Model | string | Required, max 50, `COLLATE NOCASE` for sorting |
| PlateNumber | string | Required, **unique index**, stored normalized |
| Year | int? | Optional, 1900 to next year |
| Comments | string? | Optional, max 500 |
| PhotoPath | string? | Relative reference, e.g. `photos/3f2a….jpg` |
| CreatedAtUtc / UpdatedAtUtc | DateTime / DateTime? | Set by the repository |

- **Async + lazy init**: [VehicleRepository](src/CgCar.Core/Data/VehicleRepository.cs) opens the connection on first
  use (guarded by a `SemaphoreSlim` so concurrent first calls initialize once), so app startup never waits on the database.
- **Schema versioning**: `CreateTableAsync` creates the table or adds new columns; SQLite's built-in
  `PRAGMA user_version` records one-off steps. Version 1 = initial setup + demo data, run in one transaction, so
  demo data never comes back after the user deletes it.
- **Plate uniqueness**: [LicensePlate.Normalize](src/CgCar.Core/Models/LicensePlate.cs) uppercases, strips spaces,
  dashes and accents, and maps the 14 Greek letters that look like Latin ones (Greek plates use only those) to Latin.
  So `ΙΚΑ-1234`, `ika 1234` and `IKA1234` are the same plate. The repository checks for duplicates (clear error
  message) and the unique index is the safety net. Plates are displayed as `IKA-1234`.
- **Package**: sqlite-net-pcl 1.11 is built on SQLitePCLRaw 3, which ships the native SQLite itself (and uses the
  system SQLite on iOS), so `SQLitePCLRaw.bundle_green` is no longer needed.

### Photos
1. **Pick**: `MediaPicker.PickPhotosAsync` (limit 1) opens the system picker. .NET 10's picker options resize to
   max 1600 px, compress (JPEG 85) and fix rotation. The result is copied into the **cache** folder for preview.
2. **Save**: only now is the photo copied into `AppData/photos/{guid}.jpg`, and the database stores the
   **relative** reference `photos/{guid}.jpg`. If the save fails (e.g. duplicate plate), that copy is deleted again.
3. **Replace / remove / delete vehicle**: the old file is deleted after the database change succeeds.

Why relative references: on iOS the app's container path changes with every app update, so absolute paths saved
in the database would break. Why the temporary copy: cancelling an edit leaves nothing behind in app storage.
[PhotoService](src/CgCar.App/Services/PhotoService.cs) also refuses to resolve or delete anything outside `photos/`.

Permissions: Android 13+ uses the system Photo Picker, which needs no permission ("CgCar will only have access to
the photos you select"). Older Android versions get `READ_EXTERNAL_STORAGE` (max SDK 32). `READ_MEDIA_IMAGES`
is deliberately not requested. iOS has `NSPhotoLibraryUsageDescription`.

### Validation
[VehicleEditViewModel](src/CgCar.Core/ViewModels/VehicleEditViewModel.cs) derives from `ObservableValidator`.
DataAnnotations (`[Required]`, `[MaxLength]`, `[CustomValidation]` for year and plate) validate each field as it
changes, and all fields again on Save. Each field shows its first error underneath (`BrandError`, …). The
duplicate-plate error comes from the repository and clears as soon as the plate is edited. Year is bound as text
so invalid input can be shown and explained instead of silently dropped.

### Dependency injection lifetimes
Repository and platform services are singletons (one SQLite connection). The dashboard page/ViewModel is a
singleton (root page, lives as long as the app). List and edit pages/ViewModels are transient: every navigation
gets a fresh instance with clean state.

## Key decisions

| Decision | Choice | Why | Alternative considered |
|---|---|---|---|
| Solution structure | Core library + App + Tests | Testable ViewModels, compiler-enforced layering | Single MAUI project with folders |
| Category | Enum stored as int | Matches spec; dashboard built from the enum | Separate Categories table |
| Photo storage | Copy into app storage, relative reference | Survives gallery deletion, temp-file cleanup and iOS container changes | Store the picker's original path |
| Plate uniqueness | Unique + Greek/Latin normalization | Prevents real-world duplicates typed in either alphabet | Case-insensitive only / not unique |
| Refresh | Reload on page appearing | Always consistent with the DB, simple | Messenger events between ViewModels |
| UI language | English | Owner's choice | Greek / resx localization |
| Platforms | Android + iOS only | Per spec | + Windows for faster local debugging |
| SQLite package | sqlite-net-pcl 1.11.285 | Latest stable; no bundle_green needed | 1.9.172 + bundle_green 2.1.11 |
| UI toolkit | CommunityToolkit.Maui | Converters, EventToCommandBehavior, Toast, StatusBarBehavior | Plain MAUI |
| Extras | Thumbnails, search, demo data, light/dark theme | Polish beyond the spec | |

`Microsoft.Maui.Controls` is pinned to 10.0.110 because CommunityToolkit.Maui 15 requires 10.0.90 or later
(newer than the workload's default).

## Tests

`dotnet test`: 64 xUnit tests.

- **LicensePlate**: normalization (Greek/Latin, accents, separators) and display formatting.
- **VehicleRepository**: runs against a real temporary SQLite file: insert/update timestamps, normalization,
  duplicate detection across alphabets, counts, filtering/sorting, delete, demo data seeded only once.
- **ViewModels**: dashboard counts and navigation; list search/delete/add/edit; edit validation, save,
  duplicate plate, photo pick/replace/remove/rollback, delete, load-once behavior. Navigation, dialogs and photos
  use small hand-written fakes ([Fakes.cs](tests/CgCar.Tests/TestSupport/Fakes.cs)); no mocking library needed.

## Known limitations / next steps
- iOS was not built or run (requires a Mac); Android was verified on an Android 16 emulator.
- Picked-but-unsaved photos stay in the cache folder until the OS clears it.
- UI text is English only (no localization resources).
- Search filters in memory, which is fine for a per-category list but would move to SQL for large datasets.
