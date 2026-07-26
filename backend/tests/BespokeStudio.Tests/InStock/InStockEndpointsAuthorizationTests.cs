using BespokeStudio.Api.Endpoints;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Contracts.Common;
using BespokeStudio.Application.Contracts.InStock;
using BespokeStudio.Application.Contracts.Orders;
using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Application.Security;
using BespokeStudio.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace BespokeStudio.Tests.InStock;

public sealed class InStockEndpointsAuthorizationTests
{
    [Fact]
    public void AdminInStockEndpoints_RequireAdminOnlyPolicy()
    {
        using var app = CreateApp();
        app.MapInStockEndpoints();

        var endpoints = GetRouteEndpoints(app)
            .Where(endpoint => endpoint.RoutePattern.RawText?.Contains("/api/admin/in-stock", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, endpoint =>
        {
            var authorize = endpoint.Metadata.GetMetadata<IAuthorizeData>();
            Assert.NotNull(authorize);
            Assert.Equal(AdminAccess.PolicyName, authorize.Policy);
        });
    }

    [Fact]
    public void PublicInStockEndpoints_AllowAnonymous()
    {
        using var app = CreateApp();
        app.MapInStockEndpoints();

        var endpoints = GetRouteEndpoints(app)
            .Where(endpoint =>
                endpoint.RoutePattern.RawText?.Contains("/api/in-stock", StringComparison.Ordinal) == true &&
                endpoint.RoutePattern.RawText?.Contains("/api/admin/", StringComparison.Ordinal) != true)
            .ToArray();

        Assert.NotEmpty(endpoints);
        Assert.All(endpoints, endpoint =>
            Assert.Contains(endpoint.Metadata, metadata => metadata is IAllowAnonymous));
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddLogging();
        builder.Services.AddRouting();
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IInStockService, StubInStockService>();
        builder.Services.AddSingleton<IUploadService, StubUploadService>();
        builder.Services.AddSingleton<IAdminAuditLogService, StubAuditLogService>();
        builder.Services.AddSingleton<IOutputCacheStore, StubOutputCacheStore>();
        return builder.Build();
    }

    private static IReadOnlyList<RouteEndpoint> GetRouteEndpoints(WebApplication app) =>
        ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToArray();

    private sealed class StubInStockService : IInStockService
    {
        public Task<IReadOnlyList<PublicInStockItemResponse>> GetPublicItemsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublicInStockItemResponse>>([]);

        public Task<PublicInStockItemResponse?> GetPublicItemBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<PublicInStockItemResponse?>(null);

        public Task<IReadOnlyList<AdminInStockItemResponse>> GetAdminItemsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<AdminInStockItemResponse>>([]);

        public Task<AdminInStockItemResponse?> GetAdminItemByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AdminInStockItemResponse?>(null);

        public Task<AdminInStockItemResponse> CreateItemAsync(
            SaveInStockItemRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AdminInStockItemResponse?> UpdateItemAsync(
            Guid id,
            SaveInStockItemRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ArchiveInStockItemResponse?> ArchiveItemAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ArchiveInStockItemResponse?> RestoreItemAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AdminInStockImageResponse?> AddImageAsync(
            Guid itemId,
            UploadFileRequest file,
            string? altText,
            int? displayOrder,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AdminInStockImageResponse?> UpdateImageAsync(
            Guid itemId,
            Guid imageId,
            UpdateInStockImageRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AdminInStockImageResponse>?> ReorderImagesAsync(
            Guid itemId,
            ReorderInStockImagesRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteImageAsync(
            Guid itemId,
            Guid imageId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubUploadService : IUploadService
    {
        public Task<IReadOnlyList<UploadedFileResponse>> UploadOrderAttachmentsAsync(
            IReadOnlyCollection<UploadFileRequest> files,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadedFileResponse> UploadPortfolioImageAsync(
            UploadFileRequest file,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PreparedUploadFile> PrepareInStockImageAsync(
            UploadFileRequest file,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task CompensateOrphanedPromotedFileAsync(
            string storageKey,
            string? originalFileName,
            long? fileSizeBytes,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadDownloadResponse?> OpenPublicInStockImageAsync(
            Guid imageId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<UploadDownloadResponse?>(null);

        public Task<UploadDownloadResponse?> OpenInStockImageForAdminAsync(
            Guid imageId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<UploadDownloadResponse?>(null);

        public Task<UploadedFileResponse> UploadContentImageAsync(
            UploadFileRequest file,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadDownloadResponse?> OpenPublicContentImageAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadDownloadResponse?> OpenContentImageForAdminAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadedFileResponse> UploadBrandImageAsync(
            UploadFileRequest file,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadDownloadResponse?> OpenPublicBrandImageAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadDownloadResponse?> OpenBrandImageForAdminAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadDownloadResponse?> OpenPublicPortfolioImageAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadDownloadResponse?> OpenPortfolioImageForAdminAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadDownloadResponse?> OpenOrderAttachmentAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<DeleteOrderAttachmentResult?> DeleteOrderAttachmentAsync(
            Guid orderId,
            Guid attachmentId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadMetadataResponse?> GetMetadataAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<UploadedFileResponse>> GetAllAsync(
            UploadPurpose? purpose = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubAuditLogService : IAdminAuditLogService
    {
        public Task<PagedResponse<AdminAuditLogEntryResponse>> GetAsync(
            AdminAuditLogQueryRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RecordAsync(
            AdminAuditLogWriteRequest request,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void AddPending(AdminAuditLogWriteRequest request)
        {
        }
    }

    private sealed class StubOutputCacheStore : IOutputCacheStore
    {
        public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken) =>
            ValueTask.FromResult<byte[]?>(null);

        public ValueTask SetAsync(
            string key,
            byte[] value,
            string[]? tags,
            TimeSpan validFor,
            CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;

        public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken) =>
            ValueTask.CompletedTask;
    }
}
