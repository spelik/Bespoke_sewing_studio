using System.Text.Json;
using BespokeStudio.Api.Configuration;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.ContactMessages;
using BespokeStudio.Application.Security;
using BespokeStudio.Application.Validation;

namespace BespokeStudio.Api.Endpoints;

public static class ContactMessageEndpoints
{
    public static IEndpointRouteBuilder MapContactMessageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var publicMessages = endpoints.MapGroup("/api/contact-messages")
            .WithTags("Contact Messages");

        publicMessages.MapPost(string.Empty, CreateAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.PublicContact)
            .WithName("CreateContactMessage")
            .Produces<ContactMessageResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status429TooManyRequests);

        var admin = endpoints.MapGroup("/api/admin/contact-messages")
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithTags("Admin Contact Messages");

        admin.MapGet(string.Empty, GetAllAsync)
            .WithName("GetAdminContactMessages")
            .Produces<IReadOnlyList<ContactMessageListItemResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        admin.MapGet("/{id:guid}", GetByIdAsync)
            .WithName("GetAdminContactMessageById")
            .Produces<ContactMessageResponse>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        admin.MapPatch("/{id:guid}/status", UpdateStatusAsync)
            .WithName("UpdateAdminContactMessageStatus")
            .Produces<ContactMessageResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateContactMessageRequest request,
        IContactMessageService service,
        INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        var errors = ContactMessageValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(errors));
        }

        var result = await service.CreateAsync(request, cancellationToken);
        await notificationService.NotifyNewContactMessageCreatedAsync(result.Id, cancellationToken);

        return TypedResults.Created($"/api/contact-messages/{result.Id}", result);
    }

    private static async Task<IResult> GetAllAsync(
        int? take,
        IContactMessageService service,
        CancellationToken cancellationToken)
    {
        var limit = take ?? 100;

        if (limit is < 1 or > 200)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["take"] = ["Take must be between 1 and 200."]
            });
        }

        var messages = await service.GetAllAsync(limit, cancellationToken);
        return TypedResults.Ok(messages);
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        IContactMessageService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static async Task<IResult> UpdateStatusAsync(
        Guid id,
        UpdateContactMessageStatusRequest request,
        IContactMessageService service,
        CancellationToken cancellationToken)
    {
        var errors = ContactMessageValidator.Validate(request);
        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(ToJsonPropertyNames(errors));
        }

        var result = await service.UpdateStatusAsync(id, request, cancellationToken);
        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }

    private static Dictionary<string, string[]> ToJsonPropertyNames(IReadOnlyDictionary<string, string[]> errors) =>
        errors.ToDictionary(
            pair => JsonNamingPolicy.CamelCase.ConvertName(pair.Key),
            pair => pair.Value);
}
