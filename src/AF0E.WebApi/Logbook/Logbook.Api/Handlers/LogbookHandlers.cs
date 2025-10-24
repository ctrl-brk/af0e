using System.Net;
using AF0E.DB;
using Logbook.Api.Models;
using Logbook.Api.Responses;
using Microsoft.EntityFrameworkCore;

namespace Logbook.Api.Handlers;

public static class LogbookHandlers
{
    public static async Task<List<QsoSummary>> GetLogByCall(string call, HrdDbContext dbContext)
    {
        return [.. (await dbContext.Log.Where(x => x.ColCall == WebUtility.UrlDecode(call)).OrderByDescending(x => x.ColTimeOn).ToListAsync()).Select(x => new QsoSummary(x))];
    }

    public static async Task<List<string>> GetPartialLookup(string call, int maxResults, HrdDbContext dbContext)
    {
        if (string.IsNullOrEmpty(call))
            return [];

        // select top({maxResults * 5}) is a nasty hack, but I can't figure out how to make this work any other way
        return await dbContext.Database.SqlQuery<string>($"""
                                                          select distinct top({maxResults}) COL_CALL
                                                            from (select top({maxResults * 5}) col_call
                                                                    from TABLE_HRD_CONTACTS_V01
                                                                   where COL_CALL like {WebUtility.UrlDecode(call) + "%"}
                                                                   order by COL_TIME_ON desc) as a
                                                          """).ToListAsync();
    }

    public static async Task<LogSearchResponse> GetLog(string? call, int? skip, int? take, string? sort, int? orderBy, string? begin, string? end, HrdDbContext dbContext)
    {
        const int DEFAULT_PAGE_SIZE = 50;
        const int MAX_PAGE_SIZE = 500;

        skip ??= 0;
        if (take is null or > MAX_PAGE_SIZE) take = DEFAULT_PAGE_SIZE;

        call = WebUtility.UrlDecode(call);

#pragma warning disable CA1305
        var countQuery = begin is null || end is null ?
            dbContext.Log.CountAsync(x => call == null || x.ColCall == call) :
            dbContext.Log.CountAsync(x => (call == null || x.ColCall == call) && x.ColTimeOn >= DateTime.Parse(begin) && x.ColTimeOn <= DateTime.Parse(end).AddDays(1));

        var cnt = await countQuery;

        var logQuery = begin is null || end is null ?
            dbContext.Log.Where(x => call == null || x.ColCall == call) :
            dbContext.Log.Where(x => (call == null || x.ColCall == call) && x.ColTimeOn >= DateTime.Parse(begin) && x.ColTimeOn <= DateTime.Parse(end).AddDays(1));
#pragma warning restore CA1305

        logQuery = logQuery.Include(c => c.PotaContacts);

        logQuery = orderBy == 1 ? logQuery.OrderBy(x => x.ColTimeOn) : logQuery.OrderByDescending(x => x.ColTimeOn);
        logQuery = logQuery.Skip(skip.Value).Take(take.Value);

        var qsoList = await logQuery.Select(x => new QsoSummary(x)).ToListAsync();

        return new LogSearchResponse { TotalCount = cnt, Contacts = qsoList };
    }

    public static async Task<QsoDetails?> GetQsoDetails(int logId, HrdDbContext dbContext)
    {
        var res = await dbContext.Log
            .Include(x => x.PotaContacts)
            .ThenInclude(c => c.Activation)
            .ThenInclude(p => p.Park)
            .SingleOrDefaultAsync(x => x.ColPrimaryKey == logId);

        return res == null
            ? null
            : new QsoDetails(res);
    }
}
