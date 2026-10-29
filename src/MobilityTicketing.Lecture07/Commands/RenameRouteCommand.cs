using Microsoft.EntityFrameworkCore;
using MobilityTicketing.Lecture07.Contracts;
using MobilityTicketing.Lecture07.Data;

namespace MobilityTicketing.Lecture07.Commands;

public static class RenameRouteCommand
{
    public static async Task<RenameResult> ExecuteAsync(
        string connection, string routeId, string name, CancellationToken ct = default)
    {
        DatabaseSession.RequireText(routeId, nameof(routeId));
        DatabaseSession.RequireText(name, nameof(name));
        var trimmed = name.Trim();

        return await DatabaseSession.WriteAsync(connection, "route" + routeId, async db =>
        {
            var route = await db.Routes.SingleOrDefaultAsync(r => r.Id == routeId, ct);
            if (route is null) return RenameResult.NotFound;
            route.Name = trimmed;
            return RenameResult.Renamed;

        }, ct);
    }
}