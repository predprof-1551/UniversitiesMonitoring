using System.Text.Json.Serialization;
using UniversityMonitoring.Data.Models;

namespace UniversityMonitoring.Data.Entities;

public class UniversityServiceEntity
{
    public UniversityServiceEntity(UniversityService universityServiceModel,
        bool loadUsers = true,
        bool loadComments = true,
        bool? isSubscribed = null)
    {
        ServiceId = universityServiceModel.Id;
        ServiceName = universityServiceModel.Name;
        UniversityName = universityServiceModel.University.Name;

        var lastChange = universityServiceModel.UniversityServiceStateChanges.LastOrDefault();

        IsOnline = lastChange?.IsOnline ?? false;
        ChangedStatusAt = lastChange?.ChangedAt;

        Subscribers = loadUsers
            ? universityServiceModel.UserSubscribeToServices.Select(x => new UserEntity(x.User))
            : Array.Empty<UserEntity>();
        Comments = loadComments
            ? universityServiceModel.UserRateOfServices.Select(x => new CommentEntity(x))
            : Array.Empty<CommentEntity>();
        Url = universityServiceModel.Url;
        IsSubscribed = isSubscribed;
    }

    [JsonConstructor]
    public UniversityServiceEntity(ulong serviceId,
        string serviceName,
        string universityName,
        bool isOnline,
        string url,
        IEnumerable<UserEntity> subscribers,
        IEnumerable<CommentEntity> comments,
        bool? isSubscribed)
    {
        ServiceId = serviceId;
        ServiceName = serviceName;
        UniversityName = universityName;
        IsOnline = isOnline;
        Url = url;
        Subscribers = subscribers;
        Comments = comments;
        IsSubscribed = isSubscribed;
    }

    public ulong ServiceId { get; }
    public string ServiceName { get; }
    public string UniversityName { get; }
    public bool IsOnline { get; }
    public DateTime? ChangedStatusAt { get; }
    public string Url { get; }
    public bool? IsSubscribed { get; }
    public IEnumerable<UserEntity> Subscribers { get; }
    public IEnumerable<CommentEntity> Comments { get; }
}