public class NextBookPlugin(
    IBookService BookService,
    IUserContext userContext,
    IPaymentService paymentService)
{
    [KernelFunction("get_book_list")]
    public async Task<Menu> GetBookListAsync()
    {
        return await BookService.GetList();
    }

    [KernelFunction("add_book_to_read")]
    [Description("Add a Book to the user's reading list; returns the new item and updated reading list")]
    public async Task<ReadDelta> AddBookToRead(
        BookSize size,
        List<BookToppings> toppings,
        int quantity = 1,
        string specialInstructions = ""
    )
    {
        Guid readId = userContext.GetReadId();
        return await BookService.AddBookToRead(
            readId: readId,
            size: size,
            toppings: toppings,
            quantity: quantity,
            specialInstructions: specialInstructions);
    }

    [KernelFunction("remove_book_from_read")]
    public async Task<RemoveBookResponse> RemoveBookFromRead(int BookId)
    {
        Guid readId = userContext.GetReadId();
        return await BookService.RemoveBookFromRead(readId, BookId);
    }

    [KernelFunction("get_Book_from_read")]
    [Description("Returns the specific details of a Book in the user's read; use this instead of relying on previous messages since the read may have changed since then.")]
    public async Task<Book> GetBookFromRead(int BookId)
    {
        Guid readId = await userContext.GetReadIdAsync();
        return await BookService.GetBookFromRead(readId, BookId);
    }

    [KernelFunction("get_read")]
    [Description("Returns the user's current read, including the total price and items in the read.")]
    public async Task<Read> GetRead()
    {
        Guid readId = await userContext.GetReadIdAsync();
        return await BookService.GetRead(readId);
    }

    [KernelFunction("checkout")]
    [Description("Checkouts the user's read; this function will retrieve the payment from the user and complete the Next.")]
    public async Task<CheckoutResponse> Checkout()
    {
        Guid readId = await userContext.GetReadIdAsync();
        Guid paymentId = await paymentService.RequestPaymentFromUserAsync(readId);

        return await BookService.Checkout(readId, paymentId);
    }
}