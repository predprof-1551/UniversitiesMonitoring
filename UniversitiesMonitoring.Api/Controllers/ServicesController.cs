using Castle.Core.Internal;
using Microsoft.AspNetCore.Authorization;
using UniversitiesMonitoring.Api.Entities;
using UniversitiesMonitoring.Api.Services;
using UniversitiesMonitoring.Api.WebSocket;
using UniversityMonitoring.Data.Entities;
using UniversityMonitoring.Data.Models;

namespace UniversitiesMonitoring.Api.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly IServicesProvider _servicesProvider;
    private readonly IUsersProvider _usersProvider;
    private readonly IWebSocketUpdateStateNotifier _webSocketUpdateStateNotifier;
    private readonly IModulesProvider _modulesProvider;
    private readonly string[] _trustedHosts;

    private bool IsTrustedRequest => _trustedHosts.Contains(Request.Host.Host);

    public ServicesController(IServicesProvider servicesProvider,
        IUsersProvider usersProvider,
        IWebSocketUpdateStateNotifier webSocketUpdateStateNotifier,
        IModulesProvider modulesProvider,
        IConfiguration configuration)
    {
        _servicesProvider = servicesProvider;
        _usersProvider = usersProvider;
        _webSocketUpdateStateNotifier = webSocketUpdateStateNotifier;
        _modulesProvider = modulesProvider;
        _trustedHosts = (Environment.GetEnvironmentVariable("TRUSTED_HOSTS") ??
                         configuration["TrustedHosts"] ??
                         string.Empty).Split(";");
    }

    [Authorize(Roles = JwtGenerator.UserRole)]
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetService([FromRoute] ulong id)
    {
        var service = await _servicesProvider.GetServiceAsync(id);

        if (service == null)
        {
            return BadRequest("Сервис не найден");
        }

        var serviceEntity = new UniversityServiceEntity(service,
            isSubscribed: CheckIfUserSubscribed(service, ulong.Parse(User.Identity!.Name!)));

        return Ok(serviceEntity);
    }

    [Authorize(Roles = JwtGenerator.UserRole)]
    [HttpGet("{id:long}/uptime")]
    public IActionResult GetServiceUptime([FromRoute] ulong id)
    {
        var uptime = _servicesProvider.GetServiceUptime(id);

        return Ok(new
        {
            uptime
        });
    }

    [Authorize(Roles = JwtGenerator.UserRole)]
    [HttpGet("{id:long}/test")]
    public async Task<IActionResult> Test([FromRoute] ulong id)
    {
        var service = await _servicesProvider.GetServiceAsync(id);

        if (service == null)
        {
            return BadRequest("Сервис не найден");
        }

        var testResult = await _modulesProvider.TestServiceAsync(service);

        return Ok(testResult);
    }

    [Authorize(Roles = JwtGenerator.UserRole)]
    [HttpPost("{id:long}/subscribe")]
    public async Task<IActionResult> SubscribeService([FromRoute] ulong id)
    {
        var service = await _servicesProvider.GetServiceAsync(id);
        var user = await _usersProvider.GetUserAsync(User.Identity!.Name!);

        if (service == null || user == null)
        {
            return BadRequest("Сервис или пользователь не найден");
        }

        if (CheckIfUserSubscribed(service, user.Id))
        {
            return BadRequest("Вы уже подписаны на этот сервис");
        }

        await _servicesProvider.SubscribeUserAsync(user, service);
        return Ok();
    }

    [Authorize(Roles = JwtGenerator.UserRole)]
    [HttpDelete("{id:long}/unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromRoute] ulong id)
    {
        var service = await _servicesProvider.GetServiceAsync(id);
        var user = await _usersProvider.GetUserAsync(User.Identity!.Name!);

        if (service == null || user == null)
        {
            return BadRequest("Сервис или пользователь не найден");
        }

        if (!CheckIfUserSubscribed(service, user.Id))
        {
            return BadRequest("Вы не подписаны на данный сервис");
        }

        await _servicesProvider.UnsubscribeUserAsync(user, service);
        return Ok();
    }

    [Authorize(Roles = JwtGenerator.UserRole)]
    [HttpPost("{id:long}/comment")]
    public async Task<IActionResult> Comment(
        [FromRoute] ulong id,
        [FromBody] Comment comment)
    {
        var service = await _servicesProvider.GetServiceAsync(id);
        var user = await _usersProvider.GetUserAsync(User.Identity!.Name!);

        if (service == null || user == null)
        {
            return BadRequest("Сервис или пользователь не найден");
        }

        await _servicesProvider.LeaveCommentAsync(service, user, comment);
        return Ok();
    }

    [Authorize(Roles = JwtGenerator.UserRole)]
    [HttpPost("{id:long}/report")]
    public async Task<IActionResult> Report(
        [FromRoute] ulong id,
        [FromBody] Report report)
    {
        var service = await _servicesProvider.GetServiceAsync(id);
        var user = await _usersProvider.GetUserAsync(User.Identity!.Name!);

        if (service == null || user == null)
        {
            return BadRequest("Сервис или пользователь не найдены");
        }

        await _servicesProvider.CreateReportAsync(service, user, report);

        return Ok();
    }

    [Authorize(Roles = JwtGenerator.UserRole)]
    [HttpGet("{id:long}/reports-by-offline")]
    public async Task<ActionResult> GetAllReportsByOffline([FromRoute] ulong id)
    {
        var service = await _servicesProvider.GetServiceAsync(id);

        if (service == null)
        {
            return BadRequest("Сервис не найден");
        }

        return Ok(from report in _servicesProvider.GetReportsByOffline(service) select new ReportEntity(report));
    }

    [HttpGet]
    public async Task<IActionResult> GetAllServices(
        [FromQuery] bool loadUsers = false,
        [FromQuery] bool loadComments = false,
        [FromQuery] ulong? universityId = null,
        [FromQuery] ulong[]? ids = null)
    {
        // Нужно, чтобы не сливать инфу
        var trustedRequest = IsTrustedRequest || User.IsInRole(JwtGenerator.UserRole);

        loadUsers = loadUsers && trustedRequest;
        loadComments = loadComments && trustedRequest;

        var services = (await _servicesProvider.GetAllServicesAsync(universityId)).ToArray();

        var servicesApiEntities = User.IsInRole(JwtGenerator.UserRole)
            ? from service in services
            select new UniversityServiceEntity(
                service,
                loadUsers,
                loadComments,
                CheckIfUserSubscribed(service, ulong.Parse(User.Identity!.Name!)))
            : from service in services
            select new UniversityServiceEntity(service, loadUsers, loadComments);

        if (!ids.IsNullOrEmpty())
        {
            servicesApiEntities = servicesApiEntities.Where(x => ids!.Contains(x.ServiceId));
        }

        return Ok(servicesApiEntities);
    }

    [HttpGet("universities/{id:long}")]
    public async Task<IActionResult> GetUniversity([FromRoute] ulong id)
    {
        var university = await _servicesProvider.GetUniversityAsync(id);

        if (university == null)
        {
            return BadRequest("Университет не найден");
        }

        var universityEntity = new UniversityEntity(university)
        {
            IsSubscribed = User.IsInRole(JwtGenerator.UserRole)
                ? university.UniversityServices.Any(service =>
                    service.UserSubscribeToServices.Any(subscribe =>
                        subscribe.UserId.ToString() == User.Identity!.Name!))
                : null
        };

        return Ok(universityEntity);
    }

    [HttpGet("universities")]
    public IActionResult GetAllUniversities() => Ok(
        from university in _servicesProvider.GetAllUniversities()
            .ToList()
        select new UniversityEntity(university)
        {
            IsSubscribed = User.IsInRole(JwtGenerator.UserRole)
                ? university.UniversityServices.Any(service =>
                    service.UserSubscribeToServices.Any(subscribe =>
                        subscribe.UserId.ToString() == User.Identity!.Name!))
                : null
        });

    [HttpPut("update")]
    public async Task<IActionResult> UpdateService([FromBody] ChangeStateEntity[] updates)
    {
        if (!IsTrustedRequest) return Ok();

        var updateSuccess = true;

        var servicesId = new ulong[updates.Length];

        for (var i = 0; i < updates.Length; i++)
        {
            var update = updates[i];
            servicesId[i] = updates[i].ServiceId;
            var service = await _servicesProvider.GetServiceAsync(update.ServiceId);

            if (service == null)
            {
                updateSuccess = updateSuccess && false;
                continue;
            }

            await _servicesProvider.UpdateServiceStateAsync(service, update.IsOnline, i == updates.Length - 1);
        }

        await _webSocketUpdateStateNotifier.NotifyAsync(servicesId);

        if (updateSuccess) return Ok();
        return BadRequest("Часть сервисов не найдены");
    }

    [HttpGet("{id:long}/excel")]
    public async Task<IActionResult> ExcelExport([FromRoute] ulong id) =>
        File(await _servicesProvider.CreateExcelReportAsync(id), "application/vnd.ms-excel", $"{id}.xlsx");

    private static bool CheckIfUserSubscribed(UniversityService service, ulong userId) =>
        service.UserSubscribeToServices.Any(x => x.UserId == userId);
}