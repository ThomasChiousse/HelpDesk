using HelpDesk.Domain;
namespace HelpDesk.Tests
{
    public class CommentTests
    {
        private static Comment CreateComment(User user)
        {
            return new(10, user, "comment1");
        }

        private static User CreateUser()
        {
            return new(12, "Thomas", "Banana", "email@domain.com", UserRole.Technician);
        }

        [Fact]
        public void NewComment_WithValidData_ShouldSetProperties()
        {
            int id = 1; User author = CreateUser(); string content = "content1";

            var comment = new Comment(id, author, content);

            Assert.Equal(id, comment.Id);
            Assert.Equal(author, comment.Author);
            Assert.Equal(content, comment.Content);
        }

        [Fact]
        public void NewComment_WithNullUser_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => CreateComment(null!));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-45)]
        public void NewComment_WithInvalidId_ShouldThrowArgumentException(int id)
        {
            var author = CreateUser();
            Assert.Throws<ArgumentException>(() => new Comment(id, author, "comment1"));
        }

        [Theory]
        [InlineData("   ")]
        [InlineData("")]
        [InlineData(null)]
        public void NewComment_WithInvalidContent_ShouldThrowArgumentException(string? content)
        {
            var author = CreateUser();
            Assert.Throws<ArgumentException>(() => new Comment(1, author, content));
        }

        [Fact]
        public void NewComment_ShouldSetCreationDate()
        {
            //Arrange
            var expectedDate = new DateTimeOffset(
                    2026, 9, 13,
                    10, 0, 0,
                    TimeSpan.Zero);
            var clock = new TestTimeProvider(expectedDate);
            var author = CreateUser();
            var comment = new Comment(1, author, "comment text", clock);

            Assert.Equal(expectedDate.UtcDateTime, comment.CreationDate);
        }


    }
}
