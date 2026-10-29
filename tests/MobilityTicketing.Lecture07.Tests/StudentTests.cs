using MobilityTicketing.Lecture07.Contracts;
using Xunit;

namespace MobilityTicketing.Lecture07.Tests;

[Trait("Category", "Student")]
public class StudentTests : Fixture
{
    [Fact]
    public async Task Rename_trims_the_name_and_a_blank_name_is_rejected_without_writing()
    {
        Assert.Equal(RenameResult.Renamed, await App.RenameRouteAsync("R1", "  City Ring  "));
        Assert.Equal("City Ring", (await App.ReadOverviewAsync(TimeWindow.Morning))[0].RouteName);

        var before = await State();
        await Assert.ThrowsAsync<ArgumentException>(() => App.RenameRouteAsync("R1", "   "));
        Assert.Equal(before, await State());
    }

    [Fact]
    public async Task A_new_plan_records_the_changed_values_and_the_old_plan_keeps_its_own()
    {
        await App.PublishSnapshotAsync("morning", TimeWindow.Morning);
        await App.CancelTripAsync("T102");
        await App.RenameRouteAsync("R1", "City Ring");

        var live = await App.ReadOverviewAsync(TimeWindow.Morning);
        Assert.Equal(new RouteOverviewDto("R1", "City Ring", 1, Utc(6, 20)), live[0]);
        Assert.Equal(new RouteOverviewDto("R1", "City Loop", 2, Utc(6, 0)),
            (await App.ReadSnapshotAsync("morning")).Routes[0]);

        Assert.Equal(BookingStatus.Cancelled, (await App.BookTripAsync("T102", "fresh-request")).Status);

        await App.PublishSnapshotAsync("revised", TimeWindow.Morning);
        Assert.Equal(new RouteOverviewDto("R1", "City Ring", 1, Utc(6, 20)),
            (await App.ReadSnapshotAsync("revised")).Routes[0]);
    }

    [Fact]
    public async Task Two_different_plans_published_at_the_same_time_keep_their_own_window_and_rows()
    {
        var early = App.PublishSnapshotAsync("early", new(Utc(6, 0), Utc(7, 0)));
        var late = App.PublishSnapshotAsync("late", new(Utc(7, 0), Utc(8, 0)));
        await Task.WhenAll(early, late);

        var savedEarly = await App.ReadSnapshotAsync("early");
        var savedLate = await App.ReadSnapshotAsync("late");

        Assert.Equal(new TimeWindow(Utc(6, 0), Utc(7, 0)), savedEarly.Window);
        AssertRows(Baseline, savedEarly.Routes);

        Assert.Equal(new TimeWindow(Utc(7, 0), Utc(8, 0)), savedLate.Window);
        Assert.Equal(4, savedLate.Routes.Count);
        Assert.Equal(1, savedLate.Routes[0].ScheduledTripCount);
        Assert.Equal(Utc(7, 0), savedLate.Routes[0].NextDepartureUtc);
    }

    [Fact]
    public async Task A_window_finer_than_one_microsecond_is_rejected_and_nothing_is_written()
    {
        var before = await State();
        var tooFine = new TimeWindow(Utc(6, 0).AddTicks(7), Utc(7, 0));

        await Assert.ThrowsAsync<ArgumentException>(() => App.ReadOverviewAsync(tooFine));
        await Assert.ThrowsAsync<ArgumentException>(() => App.PublishSnapshotAsync("fine-id", tooFine));
        Assert.Equal(before, await State());
    }
}
