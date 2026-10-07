using CgCar.Core.Models;

namespace CgCar.Core.Data;

/// <summary>Sample vehicles inserted on first launch so the app isn't empty in a demo.</summary>
internal static class DemoData
{
    public static IEnumerable<Vehicle> CreateVehicles()
    {
        var now = DateTime.UtcNow;

        return
        [
            new() { Category = VehicleCategory.Car, Brand = "Toyota", Model = "Yaris", PlateNumber = "IKB4521", Year = 2019, CreatedAtUtc = now },
            new() { Category = VehicleCategory.Car, Brand = "Volkswagen", Model = "Golf", PlateNumber = "ZKE1290", Year = 2021, CreatedAtUtc = now },
            new() { Category = VehicleCategory.Car, Brand = "BMW", Model = "320d", PlateNumber = "YXN8814", Year = 2017, Comments = "Company car", CreatedAtUtc = now },
            new() { Category = VehicleCategory.Truck, Brand = "Mercedes-Benz", Model = "Actros", PlateNumber = "KHT3302", Year = 2020, CreatedAtUtc = now },
            new() { Category = VehicleCategory.Truck, Brand = "Volvo", Model = "FH16", PlateNumber = "XEP7719", Year = 2018, CreatedAtUtc = now },
            new() { Category = VehicleCategory.Tractor, Brand = "John Deere", Model = "6155M", PlateNumber = "MBA1103", Year = 2016, CreatedAtUtc = now },
        ];
    }
}
