using UniversityMonitoring.Data.Entities;
using UniversityMonitoring.Data.Models;

namespace UniversityMonitoring.Data;

public static class UniversityServiceExtensions
{
    public static string GenerateUrl(this UniversityService service) =>
        $"http://univermonitoring.gym1551.ru/service?serviceId={service.Id}";
    
    public static string GenerateUrl(this UniversityServiceEntity service) =>
        $"http://univermonitoring.gym1551.ru/service?serviceId={service.ServiceId}";
}