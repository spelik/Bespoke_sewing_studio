using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.RepeatableContent;
using BespokeStudio.Application.Validation;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BespokeStudio.Infrastructure.Services;

public sealed class RepeatableContentService(BespokeStudioDbContext dbContext) : IRepeatableContentService
{
    public async Task<IReadOnlyList<PublicRepeatableContentGroupResponse>> GetPublicGroupsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);

        var rows = await dbContext.RepeatableContentItems
            .AsNoTracking()
            .Where(item => item.IsActive && item.ArchivedAt == null)
            .OrderBy(item => item.GroupKey)
            .ThenBy(item => item.DisplayOrder)
            .ThenBy(item => item.ItemKey)
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(item => item.GroupKey)
            .Select(group => new PublicRepeatableContentGroupResponse(group.Key, group.Select(ToPublic).ToArray()))
            .ToArray();
    }

    public async Task<PublicRepeatableContentGroupResponse> GetPublicGroupAsync(string groupKey, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);

        var key = NormalizeRequiredKey(groupKey);
        var rows = await dbContext.RepeatableContentItems
            .AsNoTracking()
            .Where(item => item.GroupKey == key && item.IsActive && item.ArchivedAt == null)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.ItemKey)
            .ToListAsync(cancellationToken);

        return new PublicRepeatableContentGroupResponse(key, rows.Select(ToPublic).ToArray());
    }

    public async Task<IReadOnlyList<AdminRepeatableContentItemResponse>> GetAdminItemsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);

        var rows = await dbContext.RepeatableContentItems
            .AsNoTracking()
            .OrderBy(item => item.GroupKey)
            .ThenBy(item => item.DisplayOrder)
            .ThenBy(item => item.ItemKey)
            .ToListAsync(cancellationToken);

        return rows.Select(ToAdmin).ToArray();
    }

    public async Task<AdminRepeatableContentItemResponse?> GetAdminByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.RepeatableContentItems
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return item is null ? null : ToAdmin(item);
    }

    public async Task<AdminRepeatableContentItemResponse> CreateAsync(SaveRepeatableContentItemRequest request, CancellationToken cancellationToken = default)
    {
        var groupKey = NormalizeRequiredKey(request.GroupKey);
        var itemKey = NormalizeRequiredKey(request.ItemKey);
        await EnsureUniqueAsync(groupKey, itemKey, exceptId: null, cancellationToken);

        var item = new RepeatableContentItem
        {
            GroupKey = groupKey,
            ItemKey = itemKey,
            Title = N(request.Title),
            Subtitle = N(request.Subtitle),
            Body = N(request.Body),
            Label = N(request.Label),
            Value = N(request.Value),
            IconKey = N(request.IconKey),
            Url = N(request.Url),
            Rating = request.Rating,
            Location = N(request.Location),
            Service = N(request.Service),
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.RepeatableContentItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdmin(item);
    }

    public async Task<AdminRepeatableContentItemResponse?> UpdateAsync(Guid id, SaveRepeatableContentItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.RepeatableContentItems
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (item is null)
        {
            return null;
        }

        var groupKey = NormalizeRequiredKey(request.GroupKey);
        var itemKey = NormalizeRequiredKey(request.ItemKey);
        await EnsureUniqueAsync(groupKey, itemKey, id, cancellationToken);

        item.GroupKey = groupKey;
        item.ItemKey = itemKey;
        item.Title = N(request.Title);
        item.Subtitle = N(request.Subtitle);
        item.Body = N(request.Body);
        item.Label = N(request.Label);
        item.Value = N(request.Value);
        item.IconKey = N(request.IconKey);
        item.Url = N(request.Url);
        item.Rating = request.Rating;
        item.Location = N(request.Location);
        item.Service = N(request.Service);
        item.DisplayOrder = request.DisplayOrder;
        item.IsActive = request.IsActive && item.ArchivedAt == null;
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdmin(item);
    }

    public async Task<DeleteRepeatableContentItemResponse?> DeleteOrArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.RepeatableContentItems
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (item is null)
        {
            return null;
        }

        item.IsActive = false;
        item.ArchivedAt ??= DateTimeOffset.UtcNow;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new DeleteRepeatableContentItemResponse(id, true, "Repeatable content item archived.");
    }

    private async Task EnsureUniqueAsync(string groupKey, string itemKey, Guid? exceptId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.RepeatableContentItems
            .AsNoTracking()
            .AnyAsync(item =>
                item.ArchivedAt == null &&
                item.GroupKey == groupKey &&
                item.ItemKey == itemKey &&
                (!exceptId.HasValue || item.Id != exceptId), cancellationToken);

        if (exists)
        {
            throw new RepeatableContentConflictException("ItemKey", "This group already has a non-archived item with this key.");
        }
    }

    private async Task EnsureDefaultsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.RepeatableContentItems.AsNoTracking().AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        dbContext.RepeatableContentItems.AddRange(
            Seed("process-steps", "initial-consultation", "Initial Consultation", body: "Send an enquiry to discuss your requirements, garments, and vision.", label: "01", iconKey: null, rating: null, location: null, service: null, order: 0, now: now),
            Seed("process-steps", "measurements-and-design", "Measurements & Design", body: "We take precise measurements and discuss fabric, style, and your preferred timeline.", label: "02", iconKey: null, rating: null, location: null, service: null, order: 1, now: now),
            Seed("process-steps", "expert-craftsmanship", "Expert Craftsmanship", body: "Your piece is crafted or altered with meticulous attention to every detail.", label: "03", iconKey: null, rating: null, location: null, service: null, order: 2, now: now),
            Seed("process-steps", "final-fitting", "Final Fitting", body: "A final fitting ensures everything is perfect before collection or delivery.", label: "04", iconKey: null, rating: null, location: null, service: null, order: 3, now: now),
            Seed("studio-values", "crafted-with-care", "Crafted with Care", body: "Every piece is carefully made to ensure the highest quality finish.", label: null, iconKey: "award", rating: null, location: null, service: null, order: 0, now: now),
            Seed("studio-values", "passion-for-craft", "Passion for Craft", body: "Every stitch placed with care, pride, and genuine love for the art of sewing.", label: null, iconKey: "heart", rating: null, location: null, service: null, order: 1, now: now),
            Seed("studio-values", "quality-guarantee", "Quality Guarantee", body: "We stand behind every piece. Your complete satisfaction is our promise.", label: null, iconKey: "shield", rating: null, location: null, service: null, order: 2, now: now),
            Seed("studio-values", "attention-to-detail", "Attention to Detail", body: "We focus on the small details that make a piece truly special and bespoke.", label: null, iconKey: "check", rating: null, location: null, service: null, order: 3, now: now),
            Seed("testimonials", "catherine-oneill", "Catherine O'Neill", body: "The alterations on my dress were absolutely perfect. Every detail was handled with such care and skill. I could not have asked for more — truly a magical experience.", label: null, iconKey: null, rating: 5, location: "Northern Ireland", service: "Occasionwear & Bridal Adjustments", order: 0, now: now),
            Seed("testimonials", "margaret-doherty", "Margaret Doherty", body: "I ordered a memory bear for my granddaughter and was moved to tears by the result. It is beautifully made. Truly gifted hands at work here.", label: null, iconKey: null, rating: 5, location: "Northern Ireland", service: "Memory Bears", order: 1, now: now),
            Seed("testimonials", "siobhan-mcbride", "Siobhán McBride", body: "My bespoke dress for my daughter's graduation was made to perfection. The attention to detail, the quality of fabric, the care taken — outstanding from the very first consultation.", label: null, iconKey: null, rating: 5, location: "Northern Ireland", service: "Luxury Custom Tailoring", order: 2, now: now),
            Seed("testimonials", "fionnuala-walsh", "Fionnuala Walsh", body: "Professional, warm, and the results are always impeccable. A true gem for anyone seeking quality tailoring and alterations.", label: null, iconKey: null, rating: 5, location: "Northern Ireland", service: "Dressmaking & Alterations", order: 3, now: now),
            Seed("privacy-sections", "information-we-collect", "1. Information We Collect", body: "We collect information you provide directly when you place an order request, contact us, or use our services. This may include your name, email address, telephone number, and details about your garment or order. We may also collect photographs of garments you upload through our order request form.", label: null, iconKey: null, rating: null, location: null, service: null, order: 0, now: now),
            Seed("privacy-sections", "how-we-use-your-information", "2. How We Use Your Information", body: "We use the information we collect to process your order requests, communicate with you about your order, provide customer service, and improve our services. We will not sell, trade, or rent your personal information to third parties under any circumstances.", label: null, iconKey: null, rating: null, location: null, service: null, order: 1, now: now),
            Seed("privacy-sections", "data-storage-and-security", "3. Data Storage and Security", body: "Your personal data is stored securely and we take appropriate technical and organisational measures to protect it against unauthorised access, loss, or alteration. We retain your data only for as long as necessary to fulfil our services or as required by applicable law.", label: null, iconKey: null, rating: null, location: null, service: null, order: 2, now: now),
            Seed("privacy-sections", "your-rights", "4. Your Rights", body: "Under the UK General Data Protection Regulation (UK GDPR) and the Data Protection Act 2018, you have the right to access, correct, or delete your personal data. You may also object to the processing of your data or request that we restrict its use. To exercise any of these rights, please contact us by phone or enquiry.", label: null, iconKey: null, rating: null, location: null, service: null, order: 3, now: now),
            Seed("privacy-sections", "cookies", "5. Cookies", body: "Our website uses only essential cookies necessary for its operation. We do not use tracking, advertising, or analytics cookies without your explicit consent. You can disable cookies in your browser settings at any time, though this may affect certain functionality.", label: null, iconKey: null, rating: null, location: null, service: null, order: 4, now: now),
            Seed("privacy-sections", "contact", "6. Contact", body: "Bespoke Sewing Studio is the data controller for your personal information. If you have questions about this Privacy Policy or wish to exercise your data rights, please contact us by phone or enquiry.", label: null, iconKey: null, rating: null, location: null, service: null, order: 5, now: now));

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            if (!await dbContext.RepeatableContentItems.AsNoTracking().AnyAsync(cancellationToken))
            {
                throw;
            }
        }
    }

    private static RepeatableContentItem Seed(
        string groupKey,
        string itemKey,
        string title,
        string body,
        string? label,
        string? iconKey,
        int? rating,
        string? location,
        string? service,
        int order,
        DateTimeOffset now) => new()
        {
            GroupKey = groupKey,
            ItemKey = itemKey,
            Title = title,
            Body = body,
            Label = label,
            IconKey = iconKey,
            Rating = rating,
            Location = location,
            Service = service,
            DisplayOrder = order,
            UpdatedAt = now
        };

    private static PublicRepeatableContentItemResponse ToPublic(RepeatableContentItem item) => new(
        item.Id,
        item.ItemKey,
        item.Title,
        item.Subtitle,
        item.Body,
        item.Label,
        item.Value,
        item.IconKey,
        item.Url,
        item.Rating,
        item.Location,
        item.Service,
        item.DisplayOrder);

    private static AdminRepeatableContentItemResponse ToAdmin(RepeatableContentItem item) => new(
        item.Id,
        item.GroupKey,
        item.ItemKey,
        item.Title,
        item.Subtitle,
        item.Body,
        item.Label,
        item.Value,
        item.IconKey,
        item.Url,
        item.Rating,
        item.Location,
        item.Service,
        item.DisplayOrder,
        item.IsActive,
        item.UpdatedAt,
        item.ArchivedAt);

    private static string NormalizeRequiredKey(string value) => value.Trim().ToLowerInvariant();
    private static string? N(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
