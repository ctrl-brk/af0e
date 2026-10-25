using System.Security.Claims;
using AF0E.Common.Qrz;
using AF0E.DB;
using AF0E.DB.Models;
using AF0E.Services.Qrz;
using Logbook.Api.Handlers;
using Logbook.Api.Models;
using Logbook.Api.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AF0E.LogbookApi.UnitTests;

public sealed class HamEventAssignmentTests
{
    [Theory]
    [InlineData("|AF0E|", true)]
    [InlineData("|AF0E|W1AW|", true)]
    [InlineData("|W1AW|AF0E|", true)]
    [InlineData("|W1AW|AF0E|K1ABC|", true)]
    [InlineData("|af0e|W1AW|", true)]
    [InlineData("|AF0E|AF0E|", true)]
    [InlineData("|AF0EX|XAF0E|AF0E/P|", false)]
    [InlineData("|W1AW|", false)]
    [InlineData("| AF0E |", false)]
    [InlineData("AF0E", false)]
    [InlineData("", false)]
    [InlineData(" | ", false)]
    [InlineData(null, false)]
    public async Task CreateQsoMatchesOnlyCompleteParticipatingCallsigns(string? calls, bool expectedMatch)
    {
        using var db = await CreateDatabaseAsync();
        db.HamEvents.Add(CreateEvent(1, calls));
        await db.SaveChangesAsync();

        var result = await CreateQsoAsync(CreateQso(), db);
        db.ChangeTracker.Clear();

        var contacts = await db.HamEventContacts.Where(x => x.LogId == result.Id).ToListAsync();
        Assert.Equal(expectedMatch ? 1 : 0, contacts.Count);
        if (expectedMatch)
            Assert.Equal(1, contacts[0].HamEventId);
    }

    [Fact]
    public async Task CreateQsoRequiresBothDatesAndUsesInclusiveUtcDateBounds()
    {
        using var db = await CreateDatabaseAsync();
        var today = DateTime.UtcNow.Date;
        db.HamEvents.AddRange(
            CreateEvent(1, "|AF0E|", today.AddDays(-1), today.AddDays(1)),
            CreateEvent(2, "|AF0E|", today, today),
            CreateEvent(3, "|AF0E|", null, today),
            CreateEvent(4, "|AF0E|", today, null),
            CreateEvent(5, "|AF0E|", null, null),
            CreateEvent(6, "|AF0E|", today.AddDays(1), today.AddDays(2)),
            CreateEvent(7, "|AF0E|", today.AddDays(-2), today.AddDays(-1)));
        await db.SaveChangesAsync();

        var qso = CreateQso();
        qso.Date = today.AddDays(-30);
        var result = await CreateQsoAsync(qso, db);
        db.ChangeTracker.Clear();

        var eventIds = await db.HamEventContacts.Where(x => x.LogId == result.Id)
            .OrderBy(x => x.HamEventId).Select(x => x.HamEventId).ToListAsync();
        Assert.Equal([1, 2], eventIds);
    }

    [Fact]
    public async Task CreateQsoPreservesExplicitEventsAndDoesNotDuplicateMatches()
    {
        using var db = await CreateDatabaseAsync();
        db.HamEvents.AddRange(
            CreateEvent(1, "|AF0E|"),
            CreateEvent(2, "|W1AW|", DateTime.UtcNow.Date.AddDays(-2), DateTime.UtcNow.Date.AddDays(-1)),
            CreateEvent(3, "|AF0E|"));
        await db.SaveChangesAsync();
        var qso = CreateQso();
        qso.HamEventIds = [1, 2, 2];

        var result = await CreateQsoAsync(qso, db);
        db.ChangeTracker.Clear();

        var eventIds = await db.HamEventContacts.Where(x => x.LogId == result.Id)
            .OrderBy(x => x.HamEventId).Select(x => x.HamEventId).ToListAsync();
        Assert.Equal([1, 2, 3], eventIds);
    }

    [Fact]
    public async Task CreateQsoStillRejectsInvalidExplicitEventIds()
    {
        using var db = await CreateDatabaseAsync();
        db.HamEvents.Add(CreateEvent(1, "|AF0E|"));
        await db.SaveChangesAsync();
        var qso = CreateQso();
        qso.HamEventIds = [999];

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => CreateQsoAsync(qso, db));

        Assert.Contains("Invalid Ham Event IDs: 999", exception.Message, StringComparison.Ordinal);
        Assert.Empty(await db.Log.ToListAsync());
        Assert.Empty(await db.HamEventContacts.ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateQsoDetailsUsesOnlyExplicitEventIds(bool selectEvent)
    {
        using var db = await CreateDatabaseAsync();
        db.HamEvents.AddRange(CreateEvent(1, "|AF0E|"), CreateEvent(2, "|W1AW|"));
        await db.SaveChangesAsync();
        var created = await CreateQsoAsync(CreateQso(), db);
        db.ChangeTracker.Clear();
        var qso = CreateQso();
        qso.Id = created.Id;
        qso.HamEventIds = selectEvent ? [2] : [];

        var result = await LogbookHandlers.UpdateQsoDetails(
            qso, db, new AllowAuthorizationService(), new HttpContextAccessor(),
            new NullEventsPublisher(), CancellationToken.None);
        db.ChangeTracker.Clear();

        Assert.NotNull(result);
        var eventIds = await db.HamEventContacts.Where(x => x.LogId == created.Id)
            .Select(x => x.HamEventId).ToListAsync();
        Assert.Equal(selectEvent ? [2] : Array.Empty<int>(), eventIds);
    }

    private static QsoDetails CreateQso() => new()
    {
        Call = "af0e",
        Date = DateTime.UtcNow,
        Band = "20m",
        Mode = "CW",
        Name = "Test operator"
    };

    private static HamEvent CreateEvent(int id, string? calls)
        => CreateEvent(id, calls, DateTime.UtcNow.Date.AddDays(-1), DateTime.UtcNow.Date.AddDays(1));

    private static HamEvent CreateEvent(int id, string? calls, DateTime? start, DateTime? end) => new()
    {
        HamEventId = id,
        EventType = "E",
        Name = $"Event {id}",
        CallSigns = calls,
        StartDate = start,
        EndDate = end
    };

    private static async Task<QsoDetails> CreateQsoAsync(QsoDetails qso, HrdDbContext db)
    {
        using var qrz = new UnexpectedQrzService();
        return await LogbookHandlers.CreateQso(
            qso, null, qrz, new AllowAuthorizationService(), db, new HttpContextAccessor(),
            new NullEventsPublisher(), CancellationToken.None).ConfigureAwait(false);
    }

    private static async Task<TestDbContext> CreateDatabaseAsync()
    {
        var db = new TestDbContext();
        await db.Database.OpenConnectionAsync().ConfigureAwait(false);
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);
        return db;
    }

    private sealed class TestDbContext() : HrdDbContext(string.Empty)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite("Data Source=:memory:")
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<PotaPark>().Ignore(x => x.GeoPoint);
        }
    }

    private sealed class AllowAuthorizationService : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
            => Task.FromResult(AuthorizationResult.Success());

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(AuthorizationResult.Success());
    }

    private sealed class NullEventsPublisher : ILogEventsPublisher
    {
        public Task PublishAsync(LogChangedEvent evt, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class UnexpectedQrzService : IQrzService
    {
        public Task<(QrzDatabase? response, bool notFound)> QueryCallsignAsync(string callSign, CancellationToken ct = default)
            => throw new InvalidOperationException("QRZ lookup is not expected when the QSO already has a name.");

        public void Dispose() { }
    }
}
