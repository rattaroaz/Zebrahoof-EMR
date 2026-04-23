using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zebrahoof_EMR.Data;

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
        [FromServices] ApplicationDbContext db,
        int id)
    {
        return await StreamDocumentAsync(db, id, asAttachment: false);
    }

    private static async Task<IResult> GetDocumentDownload(
        [FromServices] ApplicationDbContext db,
        int id)
    {
        return await StreamDocumentAsync(db, id, asAttachment: true);
    }

    private static async Task<IResult> StreamDocumentAsync(
        ApplicationDbContext db,
        int id,
        bool asAttachment)
    {
        var document = await db.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document is null)
        {
            return Results.NotFound(new { Message = $"Document {id} not found" });
        }

        var displayName = !string.IsNullOrWhiteSpace(document.FileName)
            ? document.FileName!
            : BuildFallbackFileName(document.Name, document.Type);

        var contentType = ResolveContentType(displayName, document.Type);

        if (document.Content is { Length: > 0 })
        {
            return Results.File(
                document.Content,
                contentType,
                fileDownloadName: asAttachment ? displayName : null,
                enableRangeProcessing: true);
        }

        // Fall back to the extracted text when no original bytes are stored.
        if (!string.IsNullOrEmpty(document.ExtractedText))
        {
            var textBytes = System.Text.Encoding.UTF8.GetBytes(document.ExtractedText);
            var textName = System.IO.Path.ChangeExtension(displayName, ".txt");
            return Results.File(
                textBytes,
                "text/plain; charset=utf-8",
                fileDownloadName: asAttachment ? textName : null);
        }

        return Results.NotFound(new { Message = "Document has no stored content" });
    }

    private static async Task<IResult> RenameDocument(
        [FromServices] ApplicationDbContext db,
        int id,
        [FromBody] RenameDocumentRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { Message = "Name is required" });
        }

        var trimmed = request.Name.Trim();
        if (trimmed.Length > 200)
        {
            return Results.BadRequest(new { Message = "Name must be 200 characters or fewer" });
        }

        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == id);
        if (document is null)
        {
            return Results.NotFound(new { Message = $"Document {id} not found" });
        }

        document.Name = trimmed;
        await db.SaveChangesAsync();

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
