using BespokeStudio.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BespokeStudio.Api.Hubs;

[Authorize(Policy = AdminAccess.PolicyName)]
public sealed class AdminNotificationsHub : Hub
{
}
