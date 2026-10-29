
using Microsoft.EntityFrameworkCore;
using MobilityTicketing.Lecture07.Contracts;
using MobilityTicketing.Lecture07.Data;

namespace MobilityTicketing.Lecture07.Commands;

public static class BookTripCommand
{
    public static async Task<BookingResult> ExecuteAsync(
        string connection,
        TimeProvider clock,
        string tripId,
        string requestId,
        CancellationToken ct = default)
    {
        DatabaseSession.RequireText(tripId, nameof(tripId));
        DatabaseSession.RequireText(requestId, nameof(requestId));

        return await DatabaseSession.WriteAsync(
            connection,
            "booking:" + requestId,
            async db =>
            {
                // Check whether this request has already been processed.
                var existing = await db.Bookings
                    .SingleOrDefaultAsync(
                        b => b.RequestId == requestId, ct);

                if (existing is not null)
                {
                    if (existing.TripId != tripId)
                    {
                        return new BookingResult(
                            BookingStatus.RequestConflict);
                    }

                    return new BookingResult(
                        BookingStatus.AlreadyBooked,
                        new BookingReceipt(
                            existing.RequestId,
                            existing.TripId,
                            existing.BookedAtUtc));
                }

                // Lock the current trip status.
                var trip = await DatabaseSession.LockTripAsync(
                    db, tripId, ct);

                if (trip is null)
                {
                    return new BookingResult(BookingStatus.NotFound);
                }

                if (trip.Status != "Scheduled")
                {
                    return new BookingResult(BookingStatus.Cancelled);
                }

                // PostgreSQL stores timestamps to microsecond precision.
                var now = clock.GetUtcNow().UtcDateTime;
                now = new DateTime(
                    now.Ticks - now.Ticks % 10,
                    DateTimeKind.Utc);

                var booking = new Booking
                {
                    RequestId = requestId,
                    TripId = tripId,
                    BookedAtUtc = now
                };

                db.Bookings.Add(booking);

                return new BookingResult(
                    BookingStatus.Booked,
                    new BookingReceipt(
                        requestId, tripId, now));
            },
            ct);
    }
}
