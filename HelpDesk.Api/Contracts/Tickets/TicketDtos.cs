using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Tickets
{
    public record UserResponse(
    int Id,
    string Firstname,
    string Lastname);

    public record CommentResponse(
        int Id,
        string Content,
        DateTime CreationDate,
        UserResponse Author);

    public record TicketDetailsResponse(
        int Id,
        string Title,
        string Description,
        string Priority,
        string Status,
        DateTime CreationDate,
        UserResponse? AssignedUser);

    public record TicketListItemResponse(
    int Id,
    string Title,
    string Priority,
    string Status,
    DateTime CreationDate);

    public record PagedResponse<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

    public record TicketStatusResponse(string Status);

    public class CreateCommentRequest
    {
        [Required]
        [MaxLength(4000)]
        public string Content { get; init; } = "";
    }

    public class CreateTicketRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; init; } = string.Empty;

        [MaxLength(4000)]
        public string Description { get; init; } = string.Empty;

        [Required]
        public string Priority { get; init; } = string.Empty;
    }

    public class UpdateTicketRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; init; } = string.Empty;

        [Required]
        [MaxLength(4000)]
        public string Description { get; init; } = string.Empty;

        [Required]
        public string Priority { get; init; } = string.Empty;
    }

    public class PatchTicketRequest
    {
        [MaxLength(200)]
        public string? Title { get; init; }

        [MaxLength(4000)]
        public string? Description { get; init; }

        public string? Priority { get; init; }
    }

    public class GetTicketsRequest
    {
        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;
        [Range(1, 100)]
        public int PageSize { get; init; } = 20;
        public string? Search { get; init; }
        public string? Status { get; init; }
        public string? Priority { get; init; }
        [Range(1, int.MaxValue)]
        public int? AssignedUserId { get; init; }
        public bool? HasAssignee { get; init; }
        public string? SortBy { get; init; }
        public string? SortDirection { get; init; }

    }

    public class GetCommentsRequest
    {
        [Range(1, int.MaxValue)]
        public int Page { get; init; } = 1;

        [Range(1, 100)]
        public int PageSize { get; init; } = 20;
    }
}
