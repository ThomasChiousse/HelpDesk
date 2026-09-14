using HelpDesk.Domain.Exceptions;

namespace HelpDesk.Domain
{

    public class Ticket
    {
        public int Id { get; private set; }
        public string Title { get; private set; } = null!;
        public string Description { get; private set; } = null!;
        public TicketPriority Priority { get; private set; }
        public TicketStatus Status { get; private set; }
        public DateTime CreationDate { get; private set; }
        public User? AssignedUser { get; private set; }
        private readonly List<Comment> _comments = [];
        public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

        public Ticket(int id, string title, string description, TicketPriority priority, User? affectedUser = null)
        {
            if (id <= 0) throw new ArgumentException("id should be a positive integer", nameof(id));
            if (!IsTitleValid(title)) throw new ArgumentException("Title cannot be null, empty or whitespace.", nameof(title));
            if (!Enum.IsDefined(priority)) throw new ArgumentException("Unknown ticket priority.", nameof(priority));
            Id = id;
            Title = title;
            Description = description;
            Priority = priority;
            Status = TicketStatus.Open;
            CreationDate = DateTime.UtcNow;
            AssignedUser = affectedUser;
        }

        public Ticket(string title, string description, TicketPriority priority, User? affectedUser = null)
        {

            if (!IsTitleValid(title)) throw new ArgumentException("Title cannot be null, empty or whitespace.", nameof(title));
            if (!Enum.IsDefined(priority)) throw new ArgumentException("Unknown ticket priority.", nameof(priority));
            Title = title;
            Description = description;
            Priority = priority;
            Status = TicketStatus.Open;
            CreationDate = DateTime.UtcNow;
            AssignedUser = affectedUser;
        }

        private Ticket()
        {

        }

        public TicketStatus AdvanceStatus()
        {
            Status = Status switch
            {
                TicketStatus.Open => TicketStatus.InProgress,
                TicketStatus.InProgress => TicketStatus.Resolved,
                TicketStatus.Resolved => TicketStatus.Closed,
                TicketStatus.Closed => throw new InvalidOperationException(
                    $"Ticket {Title} is already closed"),
                _ => throw new InvalidOperationException(
                    $"Unknown ticket status: {Status}")
            };

            return Status;
        }

        public void SetTitle(string newTitle)
        {
            if (!IsTitleValid(newTitle))
                throw new ArgumentException(
        "Title cannot be null, empty or whitespace.",
        nameof(newTitle));
            Title = newTitle;
        }

        public void SetDescription(string description)
        {
            Description = description;
        }

        public void AssignUser(User user)
        {
            ArgumentNullException.ThrowIfNull(user);
            AssignedUser = user;
        }

        public void UnassignUser(int userId)
        {
            if (AssignedUser is null)
                throw new InvalidOperationException(
                    "The ticket has no assigned user.");

            if (AssignedUser.Id != userId)
                throw new AssigneeMismatchException(userId, AssignedUser.Id);
            AssignedUser = null;
        }

        private static bool IsTitleValid(string title)
        {
            return !string.IsNullOrWhiteSpace(title);
        }

        public void AddComment(Comment comment)
        {
            ArgumentNullException.ThrowIfNull(comment);
            if (Status == TicketStatus.Closed) throw new InvalidOperationException("Cannot add a comment to a closed ticket");
            _comments.Add(comment);
        }

        public IReadOnlyCollection<Comment> GetCommentsByAuthor(int id)
        {
            return Comments.Where(c => c.Author.Id == id).ToList().AsReadOnly();
        }

        public Comment? GetLatestComment()
        {
            return Comments.OrderByDescending(c => c.CreationDate).FirstOrDefault();
        }

        public bool HasCommentsFrom(int userId)
        {
            return Comments.Any(c => c.Author.Id == userId);
        }

        public int GetCommentCountByAuthor(int userId)
        {
            return Comments.Count(c => c.Author.Id == userId);
        }

        public IReadOnlyCollection<User> GetCommentAuthors()
        {
            return Comments.Select(c => c.Author).DistinctBy(u => u.Id).ToList().AsReadOnly();
        }

    }
}


