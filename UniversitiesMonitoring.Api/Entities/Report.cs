using System.Text.Json.Serialization;
using UniversityMonitoring.Data.Models;

namespace UniversitiesMonitoring.Api.Entities;

public class Report
{
    [JsonConstructor]
    public Report(string content, bool isOnline, ulong serviceId, ulong id = 0)
    {
        Id = id;
        Content = content;
        IsOnline = isOnline;
        ServiceId = serviceId;
    }

    public Report(UniversityServiceReport report)
    {
        Id = report.Id;
        Content = report.Content;
        IsOnline = report.IsOnline;
        ServiceId = report.ServiceId;
    }

    public ulong Id { get; }
    public string? Content { get; }
    public bool IsOnline { get; }
    public ulong ServiceId { get; }
}