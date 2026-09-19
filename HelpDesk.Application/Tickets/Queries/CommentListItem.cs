namespace HelpDesk.Application.Tickets.Queries;

public sealed record CommentListItem(
    int Id,
    string Content,
    DateTime CreationDate,
    CommentAuthorItem Author);
