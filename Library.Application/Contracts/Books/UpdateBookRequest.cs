namespace Library.Application.Contracts.Books
{
    public class UpdateBookRequest
    {
        public string Title { get; set; } = string.Empty;

        public string Author { get; set; } = string.Empty;

        public string Isbn { get; set; } = string.Empty;

        public int PublishedYear { get; set; }

        public int TotalCopies { get; set; }
    }
}
