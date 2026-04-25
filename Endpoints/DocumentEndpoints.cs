using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zebrahoof_EMR.Data;
using Zebrahoof_EMR.Logging;
using Zebrahoof_EMR.Services;

namespace Zebrahoof_EMR.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var documentGroup = endpoints.MapGroup("/api/documents")
            .RequireAuthorization()
            .WithTags("Documents");

        documentGroup.MapGet("/{id:int}", GetDocumentInline)
            .WithName("GetDocumentInline")
            .WithSummary("View document")
            .WithDescription("Streams the document content inline so the browser can open it in a new tab");

        documentGroup.MapGet("/{id:int}/download", GetDocumentDownload)
            .WithName("GetDocumentDownload")
            .WithSummary("Download document")
            .WithDescription("Streams the document content as an attachment");

        documentGroup.MapPut("/{id:int}/rename", RenameDocument)
            .WithName("RenameDocument")
            .WithSummary("Rename document")
            .WithDescription("Updates the display name for a stored document");
    }

    private static async Task<IResult> GetDocumentInline(
        HttpContext http,
        [FromServices] ApplicationDbContext db,
        [FromServices] IAuditLogger audit,
        [FromServices] ILoggerFactory loggerFactory,
        int id)
    {
        return await StreamDocumentAsync(http, db, audit, loggerFactory, id, asAttachment: false);
    }

    private static async Task<IResult> GetDocumentDownload(
        HttpContext http,
        [FromServices] ApplicationDbContext db,
        [FromServices] IAuditLogger audit,
        [FromServices] ILoggerFactory loggerFactory,
        int id)
    {
        return await StreamDocumentAsync(http, db, audit, loggerFactory, id, asAttachment: true);
    }

    private static async Task<IResult> StreamDocumentAsync(
        HttpContext http,
        ApplicationDbContext db,
        IAuditLogger audit,
        ILoggerFactory loggerFactory,
        int id,
        bool asAttachment)
    {
        var log = loggerFactory.CreateLogger("Zebrahoof_EMR.Api.Documents");
        var document = await db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document is null)
        {
            log.LogWarning("Document {DocumentId} not found", id);
            return Results.NotFound(new { Message = $"Document {id} not found" });
        }

        var displayName = !string.IsNullOrWhiteSpace(document.FileName)
            ? document.FileName!
            : BuildFallbackFileName(document.Name, document.Type);

        var contentType = ResolveContentType(displayName, document.Type);
        var action = asAttachment ? "document_download" : "document_view";

        if (document.Content is { Length: > 0 })
        {
            await EndpointAuditHelper.AuditAsync(
                audit,
                http,
                action,
                $"document:{id}",
                new { documentId = id, attachment = asAttachment, contentType });
            log.LogInformation(
                "Streaming document id {DocumentId} attachment {Attachment} bytes {ByteLength}",
                id,
                asAttachment,
                document.Content.Length);
            return Results.File(
                document.Content,
                contentType,
                fileDownloadName: asAttachment ? displayName : null,
                enableRangeProcessing: true);
        }

        if (!string.IsNullOrEmpty(document.ExtractedText))
        {
            await EndpointAuditHelper.AuditAsync(
                audit,
                http,
                action,
                $"document:{id}",
                new { documentId = id, attachment = asAttachment, source = "extractedText" });
            var textBytes = System.Text.Encoding.UTF8.GetBytes(document.ExtractedText);
            var textName = System.IO.Path.ChangeExtension(displayName, ".txt");
            log.LogInformation(
                "Streaming extracted text for document id {DocumentId} attachment {Attachment}",
                id,
                asAttachment);
            return Results.File(
                textBytes,
                "text/plain; charset=utf-8",
                fileDownloadName: asAttachment ? textName : null);
        }

        log.LogWarning("Document {DocumentId} has no stored content", id);
        return Results.NotFound(new { Message = "Document has no stored content" });
    }

    private static async Task<IResult> RenameDocument(
        HttpContext http,
        [FromServices] ApplicationDbContext db,
        [FromServices] IAuditLogger audit,
        [FromServices] ILoggerFactory loggerFactory,
        int id,
        [FromBody] RenameDocumentRequest request)
    {
        var log = loggerFactory.CreateLogger("Zebrahoof_EMR.Api.Documents");
        if (request is null || string.IsNullOrWhiteSpace(request.Name))
        {
            log.LogWarning("RenameDocument: validation failed for id {DocumentId}", id);
            return Results.BadRequest(new { Message = "Name is required" });
        }

        var trimmed = request.Name.Trim();
        if (trimmed.Length > 200)
        {
            log.LogWarning("RenameDocument: name too long for id {DocumentId}", id);
            return Results.BadRequest(new { Message = "Name must be 200 characters or fewer" });
        }

        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == id);
        if (document is null)
        {
            log.LogWarning("RenameDocument: document {DocumentId} not found", id);
            return Results.NotFound(new { Message = $"Document {id} not found" });
        }

        document.Name = trimmed;
        await db.SaveChangesAsync();

        log.LogInformation("RenameDocument: updated id {DocumentId} name length {NameLength}", id, trimmed.Length);
        await EndpointAuditHelper.AuditAsync(
            audit,
            http,
            "document_rename",
            $"document:{id}",
            new { documentId = id, nameLength = trimmed.Length });
        return Results.Ok(new { document.Id, document.Name });
    }

    private static string BuildFallbackFileName(string name, string type)
    {
        var safeName = string.IsNullOrWhiteSpace(name) ? "document" : name.Trim();
        var extension = type?.ToLowerInvariant() switch
        {
            "pdf" => ".pdf",
            "image" => ".png",
            "text" => ".txt",
            _ => ""
        };

        return string.IsNullOrEmpty(extension) || safeName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
            ? safeName
            : safeName + extension;
    }

    private static string ResolveContentType(string fileName, string type)
    {
        var extension = System.IO.Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".txt" => "text/plain; charset=utf-8",
            ".csv" => "text/csv; charset=utf-8",
            ".json" => "application/json",
            ".html" or ".htm" => "text/html; charset=utf-8",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => type?.ToLowerInvariant() switch
            {
                "pdf" => "application/pdf",
                "image" => "image/png",
                "text" => "text/plain; charset=utf-8",
                _ => "application/octet-stream"
            }
        };
    }
}

public class RenameDocumentRequest
{
    public string Name { get; set; } = string.Empty;
}
