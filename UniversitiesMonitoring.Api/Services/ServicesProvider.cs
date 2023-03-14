using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using UniversitiesMonitoring.Api.Entities;
using UniversityMonitoring.Data.Models;
using UniversityMonitoring.Data.Repositories;

namespace UniversitiesMonitoring.Api.Services;

public class ServicesProvider : IServicesProvider
{
    private readonly IDataProvider _dataProvider;
    private readonly IMemoryCache _cache;

    public ServicesProvider(IDataProvider dataProvider, IMemoryCache cache)
    {
        _dataProvider = dataProvider;
        _cache = cache;
    }

    public Task<UniversityService?> GetServiceAsync(ulong serviceId) =>
        _dataProvider.UniversityServices.FindAsync(serviceId);

    /// <inheritdoc />
    public double? GetServiceUptime(ulong serviceId)
    {
        if (_cache.TryGetValue<UptimeData>(GenerateCacheKeyForUptimeData(serviceId), out var uptimeData))
        {
            return Math.Round(uptimeData.OnlineTime / uptimeData.TotalTime, 2);
        }

        var stateChanges = _dataProvider.UniversityServiceStateChange.GetlAll()
            .Where(x => x.ServiceId == serviceId).OrderBy(x => x.ChangedAt)
            .ToList();

        if (stateChanges.Count == 0) return null;

        stateChanges.Add(new UniversityServiceStateChange()
        {
            ChangedAt = DateTime.UtcNow,
            IsOnline = !stateChanges[0].IsOnline
        });

        var onlineTime = 0d; // В секундах время онлайн
        var totalTime = 0d;

        for (var i = 1; i < stateChanges.Count; i++)
        {
            var ctxChange = stateChanges[i];
            var prevChange = stateChanges[i - 1];
            var deltaSec = (ctxChange.ChangedAt - prevChange.ChangedAt).TotalSeconds;

            if (prevChange.IsOnline) // Сервис был все это время онлайн, а сменил состояние сейчас на оффайн. Добавляем секунды к онлайну 
            {
                onlineTime += deltaSec;
            }

            totalTime += deltaSec;
        }

        var uptime = Math.Round(onlineTime / totalTime, 2);

        _cache.Set(GenerateCacheKeyForUptimeData(serviceId), new UptimeData(totalTime, onlineTime));
        return uptime;
    }

    public async Task<IEnumerable<UniversityService>> GetAllServicesAsync(ulong? universityId = null)
    {
        if (universityId == null) return _dataProvider.UniversityServices.GetlAll();

        var university = await _dataProvider.Universities.FindAsync(universityId.Value);

        if (university == null) return Array.Empty<UniversityService>();

        return university.UniversityServices;
    }

    public Task<University?> GetUniversityAsync(ulong universityId) =>
        _dataProvider.Universities.FindAsync(universityId);

    public IQueryable<University> GetAllUniversities() => _dataProvider.Universities.GetlAll();

    public async Task SubscribeUserAsync(User user, UniversityService service)
    {
        var subscribe = new UserSubscribeToService()
        {
            User = user,
            Service = service
        };

        await _dataProvider.Subscribes.AddAsync(subscribe);
        await SaveChangesAsync();
    }

    public async Task UnsubscribeUserAsync(User user, UniversityService service)
    {
        var subscribe = _dataProvider.Subscribes.ExecuteSql(
                $"SELECT * FROM universities_monitoring.UserSubscribeToService WHERE UserId={user.Id} AND ServiceId={service.Id}")
            .FirstOrDefault();

        if (subscribe == null)
        {
            throw new InvalidOperationException($"Can't find user({user.Id})'s subscribe to the service({service.Id})");
        }

        _dataProvider.Subscribes.Remove(subscribe);
        await SaveChangesAsync();
    }

    public async Task UpdateServiceStateAsync(UniversityService service,
        bool isOnline,
        bool forceSafe,
        DateTime? updateTime = null)
    {
        var updateState = new UniversityServiceStateChange()
        {
            Service = service,
            IsOnline = isOnline,
            ChangedAt = updateTime ?? DateTime.UtcNow
        };

        var lastUpdate = await _dataProvider.UniversityServiceStateChange.GetlAll()
            .Where(x => x.ServiceId == service.Id)
            .OrderByDescending(x => x.ChangedAt)
            .FirstOrDefaultAsync();

        if (lastUpdate != null && lastUpdate.IsOnline == isOnline)
        {
            throw new InvalidOperationException("New state mustn't has same value as previous state");
        }

        await _dataProvider.UniversityServiceStateChange.AddAsync(updateState);

        if (forceSafe) await SaveChangesAsync();

        _cache.Remove(GenerateCacheKeyForReports(service));

        if (_cache.TryGetValue<UptimeData>(GenerateCacheKeyForUptimeData(service.Id), out var uptimeData))
        {
            var delta = (updateState.ChangedAt - lastUpdate!.ChangedAt).TotalSeconds;

            uptimeData.TotalTime += delta;

            if (lastUpdate.IsOnline)
            {
                uptimeData.OnlineTime += delta;
            }
        }
    }

    public async Task LeaveCommentAsync(UniversityService service, User author, Comment comment)
    {
        var rate = new UserRateOfService()
        {
            Service = service,
            Author = author,
            Rate = comment.Rate,
            Comment = comment.Content
        };

        await _dataProvider.Rates.AddAsync(rate);
        await SaveChangesAsync();
    }

    public async Task CreateReportAsync(UniversityService service, User issuer, Report report)
    {
        var solvedDueOffline = (service.UniversityServiceStateChanges.LastOrDefault()?.IsOnline ?? false) ==
                               report.IsOnline;

        var reportEntity = new UniversityServiceReport()
        {
            Content = report.Content,
            IsOnline = report.IsOnline,
            Issuer = issuer,
            Service = service,
            IsSolved = solvedDueOffline
        };

        if (solvedDueOffline &&
            _cache.TryGetValue<List<UniversityServiceReport>>(
                GenerateCacheKeyForReports(service),
                out var cachedReports))
        {
            cachedReports.Add(reportEntity);
        }

        await _dataProvider.Reports.AddAsync(reportEntity);
        await SaveChangesAsync();
    }

    public async Task SolveReportAsync(UniversityServiceReport report)
    {
        report.IsSolved = true;
        await SaveChangesAsync();
    }

    public Task<UniversityServiceReport?> GetReportAsync(ulong reportId) => _dataProvider.Reports.FindAsync(reportId);

    /// <inheritdoc />
    public async Task<byte[]> CreateExcelReportAsync(ulong serviceId)
    {
        var service = await _dataProvider.UniversityServices.GetlAll()
            .FirstOrDefaultAsync(x => x.Id == serviceId);

        if (service == null)
        {
            throw new InvalidOperationException("Service not found");
        }
        
        return ServiceExcelReportBuilder.BuildExcel(service);
    }

    public IEnumerable<UniversityServiceReport>GetAllReports() => _dataProvider.Reports.GetlAll()
        .Include(x => x.Service)
        .Include(x => x.Issuer)
        .Where(x => !x.IsSolved).ToList();

    public IEnumerable<UniversityServiceReport> GetReportsByOffline(UniversityService service)
    {
        var cacheKey = GenerateCacheKeyForReports(service);
        var contains = _cache.TryGetValue<List<UniversityServiceReport>>(cacheKey, out var cachedReports);

        if (contains) return cachedReports;

        var lastStatus = service.UniversityServiceStateChanges.LastOrDefault();
        if (lastStatus == null || lastStatus.IsOnline) return Array.Empty<UniversityServiceReport>();

        var lastSeenOffline = GetSqlTime(lastStatus.ChangedAt);

        var result = _dataProvider.Reports.ExecuteSql(
            $"SELECT * FROM universities_monitoring.UniversityServiceReport WHERE ServiceId = {service.Id} AND " +
            $"AddedAt >= {lastSeenOffline}").ToList();

        _cache.Set(cacheKey, result);

        return result;
    }


    private string GetSqlTime(DateTime dateTime) =>
        $"STR_TO_DATE('{dateTime.Year}-{dateTime.Month}-{dateTime.Day} {dateTime.Hour}:{dateTime.Minute}:{dateTime.Second}', '%Y-%m-%d %H:%i:%s')";

    private async Task SaveChangesAsync() => await _dataProvider.SaveChangesAsync();

    private string GenerateCacheKeyForReports(UniversityService service) => $"REPORTS_{service.Id}";
    private string GenerateCacheKeyForUptimeData(ulong serviceId) => $"UPTIME_{serviceId}";
}