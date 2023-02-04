using System.Text.Json.Serialization;
using UniversityMonitoring.Data.Models;

namespace UniversityMonitoring.Data.Entities;

public class UniversityServiceEntity
{
    public UniversityServiceEntity(UniversityService universityServiceModel)
    { 
        ServiceId = universityServiceModel.Id;
        ServiceName = universityServiceModel.Name;
        IsOnline = universityServiceModel.UniversityServiceStateChanges.FirstOrDefault()?.IsOnline ?? false;
        Subscribers = universityServiceModel.UserSubscribeToServices.Select(x => new UserEntity(x.User));
        IpAddress = $"{universityServiceModel.IpAddress[0]}:" +
                    $"{universityServiceModel.IpAddress[1]}:" +
                    $"{universityServiceModel.IpAddress[2]}:" +
                    $"{universityServiceModel.IpAddress[3]}:" +
                    $"{universityServiceModel.IpAddress[4]}:" +
                    $"{universityServiceModel.IpAddress[5]}";
    }

    [JsonConstructor]
    public UniversityServiceEntity(ulong serviceId, string serviceName, bool isOnline, string ipAddress, IEnumerable<UserEntity> subscribers)
    {
        ServiceId = serviceId;
        ServiceName = serviceName;
        IsOnline = isOnline;
        IpAddress = ipAddress;
        Subscribers = subscribers;
    }
    
    public ulong ServiceId { get; }
    public string ServiceName { get; }
    public bool IsOnline { get; }
    public string IpAddress { get; }
    public IEnumerable<UserEntity> Subscribers { get; }
}