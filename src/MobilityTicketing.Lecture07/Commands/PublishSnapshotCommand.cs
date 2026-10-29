using Microsoft.EntityFrameworkCore;
using MobilityTicketing.Lecture07.Contracts;
using MobilityTicketing.Lecture07.Data;
using MobilityTicketing.Lecture07.Queries;

namespace MobilityTicketing.Lecture07.Commands;

public static class PublishSnapshotCommand
{
    public static async Task<PublishResult> ExecuteAsync(
        string connection, TimeProvider clock, Action? beforeCommit,
        string snapshotId, TimeWindow window, CancellationToken ct = default)
    {
        DatabaseSession.RequireText(snapshotId, nameof(snapshotId));
        window.Validate();

        return await DatabaseSession.WriteAsync(connection, "snapshot:" + snapshotId, async db =>
        {
            var existing = await db.PublishedPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == snapshotId, ct);

            if (existing is not null)
            {
                if (existing.FromUtc == window.FromUtc && existing.ToUtc == window.ToUtc)
                    return PublishResult.AlreadyPublished;

                throw new SnapshotConflictException(snapshotId);
            }

            var routes = await LiveBoardQuery.ExecuteAsync(db, window, ct);

            var now = clock.GetUtcNow().UtcDateTime;
            now = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerMicrosecond, DateTimeKind.Utc);

            db.PublishedPlans.Add(new PublishedPlan
            {
                Id = snapshotId,
                FromUtc = window.FromUtc,
                ToUtc = window.ToUtc,
                PublishedAtUtc = now,
                Routes = routes.Select(r => new PublishedPlanRoute
                {
                    PlanId = snapshotId,
                    RouteId = r.RouteId,
                    RouteName = r.RouteName,
                    ScheduledTripCount = r.ScheduledTripCount,
                    NextDepartureUtc = r.NextDepartureUtc
                }).ToList()
            });

            return PublishResult.Published;
        }, ct, beforeCommit);
    }
}