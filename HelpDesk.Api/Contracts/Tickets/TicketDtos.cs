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
        UserResponse? AssignedUser,
        IReadOnlyCollection<CommentResponse> Comments);

    public class CreateCommentRequest
    {
        [Range(1, int.MaxValue)]
        public int AuthorId { get; init; }

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
}
