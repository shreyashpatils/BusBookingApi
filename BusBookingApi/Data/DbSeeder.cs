using BusBookingApi.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusBookingApi.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // 1. Create stops if they do not exist.
        var stopData = new[]
        {
            ("Mumbai", "Mumbai"),
            ("Pune", "Pune"),
            ("Satara", "Satara"),
            ("Kolhapur", "Kolhapur"),
            ("Belgaum", "Belgaum"),
            ("Karwar", "Karwar"),
            ("Dharwad", "Dharwad"),
            ("Panaji", "Panaji")
        };

        foreach (var (name, city) in stopData)
        {
            var exists = await db.Stops
                .AnyAsync(s => s.Name == name);

            if (!exists)
            {
                db.Stops.Add(new Stop
                {
                    Name = name,
                    City = city,
                    IsActive = true
                });
            }
        }

        await db.SaveChangesAsync();

        // 2. Create two buses if they do not exist.
        var busData = new[]
        {
            ("MH01-BUS-001", "Mumbai Goa Express A"),
            ("MH01-BUS-002", "Mumbai Goa Express B")
        };

        foreach (var (number, name) in busData)
        {
            var exists = await db.Buses
                .AnyAsync(b => b.BusNumber == number);

            if (!exists)
            {
                db.Buses.Add(new Bus
                {
                    BusNumber = number,
                    BusName = name,
                    IsActive = true
                });
            }
        }

        await db.SaveChangesAsync();


        // 3. Create exactly ten initial seat records per bus.
        var buses = await db.Buses.ToListAsync();

        foreach (var bus in buses.Where(b =>
            b.BusNumber == "MH01-BUS-001" ||
            b.BusNumber == "MH01-BUS-002"))
        {
            for (var seatNumber = 1; seatNumber <= 10; seatNumber++)
            {
                var number = seatNumber.ToString();

                var exists = await db.BusSeats.AnyAsync(s =>
                    s.BusId == bus.Id &&
                    s.SeatNumber == number);

                if (!exists)
                {
                    db.BusSeats.Add(new BusSeat
                    {
                        BusId = bus.Id,
                        SeatNumber = number,
                        IsActive = true
                    });
                }
            }
        }

        await db.SaveChangesAsync();


        // 4. Create route A.
        await EnsureRouteAsync(
            db,
            "Mumbai-Goa Route A",
            new[]
            {
                ("Mumbai", 0m),
                ("Pune", 150m),
                ("Satara", 250m),
                ("Kolhapur", 380m),
                ("Belgaum", 450m),
                ("Karwar", 620m),
                ("Panaji", 700m)
            });

        // 5. Create route B.
        await EnsureRouteAsync(
            db,
            "Mumbai-Goa Route B",
            new[]
            {
                ("Mumbai", 0m),
                ("Pune", 150m),
                ("Kolhapur", 380m),
                ("Belgaum", 450m),
                ("Dharwad", 520m),
                ("Panaji", 700m)
            });

        await db.SaveChangesAsync();
    }

    private static async Task EnsureRouteAsync(
        AppDbContext db,
        string routeName,
        (string StopName, decimal Distance)[] stops)
    {
        var route = await db.BusRoutes
            .Include(r => r.RouteStops)
            .FirstOrDefaultAsync(r => r.Name == routeName);

        if (route == null)
        {
            route = new BusRoute
            {
                Name = routeName,
                IsActive = true
            };

            db.BusRoutes.Add(route);
            await db.SaveChangesAsync();
        }

        foreach (var (stopName, distance) in stops)
        {
            var stop = await db.Stops
                .SingleAsync(s => s.Name == stopName);

            var exists = await db.RouteStops.AnyAsync(rs =>
                rs.BusRouteId == route.Id &&
                rs.StopId == stop.Id);

            if (!exists)
            {
                var order = stops
                    .Select((item, index) => new
                    {
                        item.StopName,
                        Order = index + 1
                    })
                    .Single(x => x.StopName == stopName)
                    .Order;

                db.RouteStops.Add(new RouteStop
                {
                    BusRouteId = route.Id,
                    StopId = stop.Id,
                    StopOrder = order,
                    DistanceFromStartKm = distance
                });
            }
        }

        // 5. Create one scheduled trip for each bus and route.
        var routeA = await db.BusRoutes
            .SingleAsync(r => r.Name == "Mumbai-Goa Route A");

        var routeB = await db.BusRoutes
            .SingleAsync(r => r.Name == "Mumbai-Goa Route B");

        var busA = await db.Buses
            .SingleAsync(b => b.BusNumber == "MH01-BUS-001");

        var busB = await db.Buses
            .SingleAsync(b => b.BusNumber == "MH01-BUS-002");

        var travelDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

        if (!await db.Trips.AnyAsync(t =>
                t.BusId == busA.Id &&
                t.BusRouteId == routeA.Id &&
                t.TravelDate == travelDate))
        {
            db.Trips.Add(new Trip
            {
                BusId = busA.Id,
                BusRouteId = routeA.Id,
                TravelDate = travelDate,
                DepartureTime = new TimeOnly(8, 0),
                BaseFarePerKm = 2.00m,
                IsActive = true
            });
        }

        if (!await db.Trips.AnyAsync(t =>
                t.BusId == busB.Id &&
                t.BusRouteId == routeB.Id &&
                t.TravelDate == travelDate))
        {
            db.Trips.Add(new Trip
            {
                BusId = busB.Id,
                BusRouteId = routeB.Id,
                TravelDate = travelDate,
                DepartureTime = new TimeOnly(9, 0),
                BaseFarePerKm = 2.00m,
                IsActive = true
            });
        }

        await db.SaveChangesAsync();

        await db.SaveChangesAsync();
    }
}