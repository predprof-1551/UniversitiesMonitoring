using UniversitiesMonitoring.Api.Entities;
using UniversityMonitoring.Data.Models;

namespace UniversitiesMonitoring.Api.Services;

public interface IServicesProvider
{
    Task<UniversityService?> GetServiceAsync(ulong serviceId);
    double? GetServiceUptime(ulong serviceId);
    Task<IEnumerable<UniversityService>> GetAllServicesAsync(ulong? universityId = null);
    Task SubscribeUserAsync(User user, UniversityService service);
    Task<University?> GetUniversityAsync(ulong universityId);
    IQueryable<University> GetAllUniversities();
    Task UnsubscribeUserAsync(User user, UniversityService service);
    Task UpdateServiceStateAsync(UniversityService service, bool isOnline, bool forceSafe, DateTime? updateTime = null);
    Task LeaveCommentAsync(UniversityService service, User author, Comment comment);
    Task CreateReportAsync(UniversityService service, User issuer, Report report);
    Task<UniversityServiceReport?> GetReportAsync(ulong reportId);
    Task<byte[]> CreateExcelReportAsync(ulong serviceId);
    IEnumerable<UniversityServiceReport> GetAllReports();
    IEnumerable<UniversityServiceReport> GetReportsByOffline(UniversityService service);
    Task SolveReportAsync(UniversityServiceReport report);
}