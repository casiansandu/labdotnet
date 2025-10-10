
List<string> books = new List<string>();

void Display(Object obj)
{
    switch (obj)
    {
        case Book book:
            Console.WriteLine($"Title: {book.Title} Year: {book.YearPublished}");
            break;
        case Borrower borrower:
            Console.WriteLine($"Name: {borrower.Name} Number of books borrowed: {borrower.BorrowedBooks.Count}");
            break;
        default:
            Console.WriteLine($"Unknown type");
            break;
    }
}

while (true)
{
    Console.WriteLine("Enter new book's name(or 'exit' to quit):");
    
    string? bookname = Console.ReadLine();
    
    if (bookname == "exit" || bookname is null)
        break;
    
    books.Add(bookname);
    Console.WriteLine($"Entered book {bookname}");
}

Console.WriteLine("\nBooks entered:");
foreach (var book in books)
{
    Console.WriteLine($"{book}");
}
Console.WriteLine();

Book book1 = new Book("Scufita Rosie", "J.K. Rowling", 1590);
Book book2 = new Book("Lord of the Rings", "George R.R. Martin", 1331);
Book book3 = new Book("Iarna pe val", "Ion Creanga", 2139);
Book book4 = new Book("Game of thrones", "J.R.R. Tolkien", 3000);

        
Borrower borrower1 = new Borrower(0, "Marian", new List<Book>());
Borrower borrower2 = new Borrower(1, "Ioan", new List<Book>());
        
borrower1.BorrowedBooks.Add(book1);
borrower1.BorrowedBooks.Add(book3);
borrower1.BorrowedBooks.Add(book4);
borrower1.BorrowedBooks.Add(book2);


int dummy = 5;
Console.WriteLine($"Display example for obj of type book");
Display(book1);
Console.WriteLine($"Display example for obj of type borrower");
Display(borrower1);
Console.WriteLine($"Display example for obj of type that is not book/borrower");
Display(dummy);
Console.WriteLine();

Borrower borrower3 = borrower1 with
{
    BorrowedBooks = new List<Book>(borrower1.BorrowedBooks) {book2}
};

borrower1.BorrowedBooks.FindAll(book => book.YearPublished > 2010).ForEach(book => Console.WriteLine($"{book}"));




public record Book(string Title, string Author, int YearPublished);
public record Borrower(int ID, string Name, List<Book> BorrowedBooks);

public class Librarian
{
    private string Name {get; init; }
    private string Email {get; init; }
    private string LibrarySection {get; init; }
}