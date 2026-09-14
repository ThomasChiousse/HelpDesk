namespace HelpDesk.Domain
{
    public class Comment
    {
        public int Id { get; private set; }
        public User Author { get; private set; } = null!;
        public string Content { get; private set; } = null!;
        public DateTime CreationDate { get; private set; }
        public Comment(int id, User author, string content, TimeProvider? timeProvider = null)
        {
            if (id <= 0) throw new ArgumentException("id should be a positive integer", nameof(id));
            ArgumentNullException.ThrowIfNull(author);
            if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("content cannot be null or whitespace", nameof(content));
            Id = id;
            Author = author;
            Content = content;
            var clock = timeProvider ?? TimeProvider.System;
            CreationDate = clock.GetUtcNow().UtcDateTime;
        }
        public Comment(User author, string content, TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(author);
            if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("content cannot be null or whitespace", nameof(content));
            Author = author;
            Content = content;
            var clock = timeProvider ?? TimeProvider.System;
            CreationDate = clock.GetUtcNow().UtcDateTime;
        }

        private Comment()
        {

        }
    }
}
