using Microsoft.EntityFrameworkCore;
using MobilityTicketing.Lecture07.Commands;
using MobilityTicketing.Lecture07.Contracts;
using MobilityTicketing.Lecture07.Data;
using MobilityTicketing.Lecture07.Diagnostics;
using MobilityTicketing.Lecture07.Queries;

namespace MobilityTicketing.Lecture07;

public sealed class StudentApplication(string connection, TimeProvider? clock = null,
    ApplicationHooks? hooks = null, RunMeasurement? measurement = null) : ILectureApplication
{
    public string Connection { get; } = connection;
    public TimeProvider Clock { get; } = clock ?? TimeProvider.System;
    public ApplicationHooks Hooks { get; } = hooks ?? new();
    public RunMeasurement Measurement { get; } = measurement ?? new();
    public LectureDbContext CreateContext() => LectureDbContext.Create(Connection, measurement: Measurement);

    // Connect your command and query classes here.
    public async Task<List<RouteOverviewDto>> ReadOverviewAsync(TimeWindow window, CancellationToken ct = default)
    {
        window.Validate();
        await using var db =  CreateContext();
        return await LiveBoardQuery.ExecuteAsync(db, window, ct);
    }
    
    public async Task<CancelResult> CancelTripAsync(string tripId, CancellationToken ct = default) =>
        await CancelTripCommand.ExecuteAsync(Connection, tripId, ct);

    public async Task<RenameResult> RenameRouteAsync(string routeId, string name, CancellationToken ct = default) =>
        await RenameRouteCommand.ExecuteAsync(Connection, routeId, name, ct);
    
    public Task<BookingResult> BookTripAsync(
        string tripId,
        string requestId,
        CancellationToken ct = default) =>
        BookTripCommand.ExecuteAsync(Connection, Clock, tripId, requestId, ct);
    
    
    public async Task<PublishResult> PublishSnapshotAsync(
        string snapshotId,
        TimeWindow window,
        CancellationToken ct = default) =>
        await PublishSnapshotCommand.ExecuteAsync(
            Connection, Clock, Hooks.BeforePublishCommit, snapshotId, window, ct);

    
    public async Task<PublishedSnapshot> ReadSnapshotAsync(
        string snapshotId,
        CancellationToken ct = default)
    {
        DatabaseSession.RequireText(snapshotId,  nameof(snapshotId));
        await using var db = CreateContext();

        var plan = await db.PublishedPlans
            .AsNoTracking()
            .Where(x => x.Id == snapshotId)
            .Select(x => new
            {
                x.Id,
                x.FromUtc,
                x.ToUtc,
                x.PublishedAtUtc,
                Routes = x.Routes
                    .OrderBy(r => r.RouteId)
                    .Select(r => new RouteOverviewDto(
                        r.RouteId,
                        r.RouteName,
                        r.ScheduledTripCount,
                        r.NextDepartureUtc))
                    .ToList()
            })
            .SingleOrDefaultAsync(ct);

        if (plan is null)
            throw new SnapshotNotFoundException(snapshotId);

        return new PublishedSnapshot(
            plan.Id,
            new TimeWindow(plan.FromUtc, plan.ToUtc),
            plan.PublishedAtUtc,
            plan.Routes);
    }

}
