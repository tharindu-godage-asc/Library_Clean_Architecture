using System;
using System.Collections.Generic;
using System.Text;

namespace Library.Application.Books.CreateBook;

public class CreateBookCommand
{
    public string Title { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string Isbn { get; set; } = string.Empty;

    public int PublishedYear { get; set; }

    public int TotalCopies { get; set; }
}
