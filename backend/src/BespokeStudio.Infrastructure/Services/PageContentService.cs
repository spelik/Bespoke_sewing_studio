using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Content;
using BespokeStudio.Application.Validation;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BespokeStudio.Infrastructure.Services;

public sealed class PageContentService(BespokeStudioDbContext dbContext) : IPageContentService
{
    private const string ImageRoute="/api/content/images/";
    public async Task<PublicPageContentResponse> GetPublicPageAsync(string pageKey,CancellationToken ct=default)
    {
        await EnsureDefaultsAsync(ct); var key=pageKey.Trim().ToLowerInvariant();
        var rows=await dbContext.PageContents.AsNoTracking().Where(x=>x.PageKey==key&&x.IsActive&&x.ArchivedAt==null)
            .OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.SectionKey).ToListAsync(ct);
        return new(key,rows.Select(ToPublic).ToArray());
    }
    public async Task<IReadOnlyList<AdminPageContentResponse>> GetAdminContentAsync(CancellationToken ct=default)
    { await EnsureDefaultsAsync(ct); return (await dbContext.PageContents.AsNoTracking().OrderBy(x=>x.PageKey).ThenBy(x=>x.DisplayOrder).ThenBy(x=>x.SectionKey).ToListAsync(ct)).Select(ToAdmin).ToArray(); }
    public async Task<AdminPageContentResponse?> GetAdminByIdAsync(Guid id,CancellationToken ct=default)
    { var x=await dbContext.PageContents.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct);return x is null?null:ToAdmin(x); }
    public async Task<AdminPageContentResponse> CreateAsync(SavePageContentRequest r,CancellationToken ct=default)
    {
        var page=r.PageKey.Trim();var section=r.SectionKey.Trim();await EnsureUniqueAsync(page,section,null,ct);await EnsureImageAsync(r.ImageFileId,ct);
        var x=new PageContent{PageKey=page,SectionKey=section,Title=N(r.Title),Subtitle=N(r.Subtitle),Body=N(r.Body),CtaLabel=N(r.CtaLabel),CtaUrl=N(r.CtaUrl),ImageFileId=r.ImageFileId,ImageAltText=N(r.ImageAltText),DisplayOrder=r.DisplayOrder,IsActive=r.IsActive,UpdatedAt=DateTimeOffset.UtcNow};
        dbContext.PageContents.Add(x);await dbContext.SaveChangesAsync(ct);return ToAdmin(x);
    }
    public async Task<AdminPageContentResponse?> UpdateAsync(Guid id,SavePageContentRequest r,CancellationToken ct=default)
    {
        var x=await dbContext.PageContents.SingleOrDefaultAsync(x=>x.Id==id,ct);if(x is null)return null;
        var page=r.PageKey.Trim();var section=r.SectionKey.Trim();await EnsureUniqueAsync(page,section,id,ct);await EnsureImageAsync(r.ImageFileId,ct);
        x.PageKey=page;x.SectionKey=section;x.Title=N(r.Title);x.Subtitle=N(r.Subtitle);x.Body=N(r.Body);x.CtaLabel=N(r.CtaLabel);x.CtaUrl=N(r.CtaUrl);x.ImageFileId=r.ImageFileId;x.ImageAltText=N(r.ImageAltText);x.DisplayOrder=r.DisplayOrder;x.IsActive=r.IsActive&&x.ArchivedAt==null;x.UpdatedAt=DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(ct);return ToAdmin(x);
    }
    public async Task<DeletePageContentResponse?> DeleteOrArchiveAsync(Guid id,CancellationToken ct=default)
    {var x=await dbContext.PageContents.SingleOrDefaultAsync(x=>x.Id==id,ct);if(x is null)return null;x.IsActive=false;x.ArchivedAt??=DateTimeOffset.UtcNow;x.UpdatedAt=DateTimeOffset.UtcNow;await dbContext.SaveChangesAsync(ct);return new(id,true,"Content section archived. Linked files were retained.");}
    private async Task EnsureUniqueAsync(string page,string section,Guid? except,CancellationToken ct){if(await dbContext.PageContents.AsNoTracking().AnyAsync(x=>x.ArchivedAt==null&&x.PageKey==page&&x.SectionKey==section&&(!except.HasValue||x.Id!=except),ct))throw new PageContentConflictException("SectionKey","This page already has a non-archived section with this key.");}
    private async Task EnsureImageAsync(Guid? id,CancellationToken ct){if(id is null)return;if(!await dbContext.UploadedFiles.AsNoTracking().AnyAsync(x=>x.Id==id&&x.Purpose==UploadPurpose.SiteAsset,ct))throw new PageContentConflictException("ImageFileId","Select a valid uploaded content image.");}
    private async Task EnsureDefaultsAsync(CancellationToken ct)
    {
        if(await dbContext.PageContents.AsNoTracking().AnyAsync(ct))return; var now=DateTimeOffset.UtcNow;
        dbContext.PageContents.AddRange(
          Seed("home","hero","Premium Sewing,\nTailoring, Dressmaking\n& Memory Bears.","Exceptional tailoring, delicate dressmaking, expert alterations, and deeply personal memory bears, crafted with a refined touch.",null,"Request an Order","/order",0,now),
          Seed("home","intro","Every Stitch, a Promise of Care.",null,"From bridal alterations to fully bespoke commissions, we handle your garments with the reverence they deserve. Browse our complete range of specialist services.","All Services","/services",1,now),
          Seed("about","hero","A Studio\nBuilt on Craft.",null,null,null,null,0,now),
          Seed("about","main-content","Crafted with passion,\nand a refined personal touch.","Our Story","Bespoke Sewing Studio offers premium sewing, tailoring, dressmaking, alterations, and memory bears.\n\nBorn from a lifelong passion for fabric, form, and the art of creating garments that truly fit, our work ranges from intricate wedding dress alterations to full bespoke commissions and deeply personal memory bears, always with a quiet dedication to quality.\n\nEvery piece is created with care, attention to detail, and a refined personal touch.",null,null,1,now),
          Seed("services","intro","Our Craft,\nYour Vision.",null,"Every garment that passes through our studio receives the same devoted attention — whether a quick repair or a fully bespoke commission.",null,null,0,now),
          Seed("portfolio","intro","Gallery of\nOur Work.",null,null,null,null,0,now),
          Seed("order","intro","Begin Your\nRequest.",null,"Complete the form below and we will be in touch within one working day to discuss your requirements and arrange a consultation.",null,null,0,now),
          Seed("contact","intro","Get in\nTouch.",null,null,null,null,0,now),
          Seed("privacy","main-content","Privacy Policy","Last updated: June 2024","Bespoke Sewing Studio (“we”, “our”, “us”) is committed to protecting your personal data and complying with applicable data protection legislation. This Privacy Policy explains how we collect, use, and protect your information when you interact with our studio or website.",null,null,0,now));
        try{await dbContext.SaveChangesAsync(ct);}catch(DbUpdateException){dbContext.ChangeTracker.Clear();if(!await dbContext.PageContents.AsNoTracking().AnyAsync(ct))throw;}
    }
    private static PageContent Seed(string page,string section,string? title,string? subtitle,string? body,string? cta,string? url,int order,DateTimeOffset now)=>new(){PageKey=page,SectionKey=section,Title=title,Subtitle=subtitle,Body=body,CtaLabel=cta,CtaUrl=url,DisplayOrder=order,UpdatedAt=now};
    private static PublicPageContentSectionResponse ToPublic(PageContent x)=>new(x.Id,x.SectionKey,x.Title,x.Subtitle,x.Body,x.CtaLabel,x.CtaUrl,x.ImageFileId is null?null:$"{ImageRoute}{x.ImageFileId}",x.ImageAltText,x.DisplayOrder);
    private static AdminPageContentResponse ToAdmin(PageContent x)=>new(x.Id,x.PageKey,x.SectionKey,x.Title,x.Subtitle,x.Body,x.CtaLabel,x.CtaUrl,x.ImageFileId,x.ImageFileId is null?null:$"{ImageRoute}{x.ImageFileId}",x.ImageAltText,x.DisplayOrder,x.IsActive,x.UpdatedAt,x.ArchivedAt);
    private static string? N(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
}
