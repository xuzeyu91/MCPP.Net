using LibraryManagement.Models;

namespace LibraryManagement.Services
{
    /// <summary>
    /// 图书服务接口
    /// </summary>
    public interface IBookService
    {
        /// <summary>
        /// 获取所有图书
        /// </summary>
        /// <returns>图书列表</returns>
        List<Book> GetAllBooks();

        /// <summary>
        /// 根据ID获取图书
        /// </summary>
        /// <param name="id">图书ID</param>
        /// <returns>图书信息，如果未找到则返回null</returns>
        Book? GetBookById(int id);

        /// <summary>
        /// 根据ISBN获取图书
        /// </summary>
        /// <param name="isbn">ISBN编号</param>
        /// <returns>图书信息，如果未找到则返回null</returns>
        Book? GetBookByISBN(string isbn);

        /// <summary>
        /// 分页查询图书
        /// </summary>
        /// <param name="query">查询参数</param>
        /// <returns>分页结果</returns>
        PagedResult<Book> QueryBooks(BookQueryDto query);

        /// <summary>
        /// 创建图书
        /// </summary>
        /// <param name="bookDto">图书信息</param>
        /// <returns>创建的图书</returns>
        Book CreateBook(CreateBookDto bookDto);

        /// <summary>
        /// 更新图书信息
        /// </summary>
        /// <param name="id">图书ID</param>
        /// <param name="bookDto">更新的图书信息</param>
        /// <returns>更新后的图书，如果未找到则返回null</returns>
        Book? UpdateBook(int id, UpdateBookDto bookDto);

        /// <summary>
        /// 更新图书库存
        /// </summary>
        /// <param name="id">图书ID</param>
        /// <param name="stockUpdate">库存更新信息</param>
        /// <returns>更新后的图书，如果未找到则返回null</returns>
        Book? UpdateBookStock(int id, BookStockUpdateDto stockUpdate);

        /// <summary>
        /// 删除图书
        /// </summary>
        /// <param name="id">图书ID</param>
        /// <returns>是否删除成功</returns>
        bool DeleteBook(int id);

        /// <summary>
        /// 批量删除图书
        /// </summary>
        /// <param name="ids">图书ID列表</param>
        /// <returns>成功删除的图书数量</returns>
        int DeleteBooks(List<int> ids);
    }

    /// <summary>
    /// 图书服务实现
    /// </summary>
    public class BookService : IBookService
    {
        // 模拟数据库
        private static readonly List<Book> _books = new List<Book>
        {
            new Book
            {
                Id = 1,
                ISBN = "9787302164340",
                Title = "算法导论",
                Author = "Thomas H. Cormen",
                PublishDate = new DateTime(2009, 1, 1),
                Category = "计算机科学",
                Price = 85.00m,
                StockQuantity = 100,
                Description = "《算法导论》是一本算法教材，全面介绍了算法设计与分析。"
            },
            new Book
            {
                Id = 2,
                ISBN = "9787115546081",
                Title = "C# 10.0本质论",
                Author = "Mark Michaelis",
                PublishDate = new DateTime(2022, 3, 1),
                Category = "编程语言",
                Price = 119.00m,
                StockQuantity = 50,
                Description = "《C# 10.0本质论》是一本全面介绍C#编程语言的书籍。"
            },
            new Book
            {
                Id = 3,
                ISBN = "9787115583864",
                Title = "ASP.NET Core微服务实战",
                Author = "Kevin Hoffman",
                PublishDate = new DateTime(2021, 5, 1),
                Category = "Web开发",
                Price = 79.00m,
                StockQuantity = 30,
                Description = "本书介绍如何使用ASP.NET Core构建微服务架构。"
            }
        };

        // 用于生成自增ID
        private static int _nextId = _books.Count > 0 ? _books.Max(b => b.Id) + 1 : 1;

        /// <inheritdoc />
        public List<Book> GetAllBooks()
        {
            return _books.ToList();
        }

        /// <inheritdoc />
        public Book? GetBookById(int id)
        {
            return _books.FirstOrDefault(b => b.Id == id);
        }

        /// <inheritdoc />
        public Book? GetBookByISBN(string isbn)
        {
            return _books.FirstOrDefault(b => b.ISBN == isbn);
        }

        /// <inheritdoc />
        public PagedResult<Book> QueryBooks(BookQueryDto query)
        {
            var filteredBooks = _books.AsQueryable();

            // 应用过滤条件
            if (!string.IsNullOrWhiteSpace(query.Title))
            {
                filteredBooks = filteredBooks.Where(b => b.Title.Contains(query.Title, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(query.Author))
            {
                filteredBooks = filteredBooks.Where(b => b.Author.Contains(query.Author, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(query.Category))
            {
                filteredBooks = filteredBooks.Where(b => b.Category == query.Category);
            }

            if (query.MinPrice.HasValue)
            {
                filteredBooks = filteredBooks.Where(b => b.Price >= query.MinPrice.Value);
            }

            if (query.MaxPrice.HasValue)
            {
                filteredBooks = filteredBooks.Where(b => b.Price <= query.MaxPrice.Value);
            }

            // 计算总记录数
            var totalCount = filteredBooks.Count();

            // 应用分页
            var pagedBooks = filteredBooks
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            // 构建分页结果
            return new PagedResult<Book>
            {
                Items = pagedBooks,
                TotalCount = totalCount,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
        }

        /// <inheritdoc />
        public Book CreateBook(CreateBookDto bookDto)
        {
            // 检查ISBN是否已存在
            if (_books.Any(b => b.ISBN == bookDto.ISBN))
            {
                throw new InvalidOperationException($"ISBN '{bookDto.ISBN}' 已存在");
            }

            var book = new Book
            {
                Id = _nextId++,
                ISBN = bookDto.ISBN,
                Title = bookDto.Title,
                Author = bookDto.Author,
                PublishDate = bookDto.PublishDate,
                Category = bookDto.Category,
                Price = bookDto.Price,
                StockQuantity = bookDto.StockQuantity,
                Description = bookDto.Description
            };

            _books.Add(book);
            return book;
        }

        /// <inheritdoc />
        public Book? UpdateBook(int id, UpdateBookDto bookDto)
        {
            var book = _books.FirstOrDefault(b => b.Id == id);
            if (book == null)
            {
                return null;
            }

            // 更新非空属性
            if (!string.IsNullOrWhiteSpace(bookDto.Title))
            {
                book.Title = bookDto.Title;
            }

            if (!string.IsNullOrWhiteSpace(bookDto.Author))
            {
                book.Author = bookDto.Author;
            }

            if (!string.IsNullOrWhiteSpace(bookDto.Category))
            {
                book.Category = bookDto.Category;
            }

            if (bookDto.Price.HasValue)
            {
                book.Price = bookDto.Price.Value;
            }

            if (bookDto.StockQuantity.HasValue)
            {
                book.StockQuantity = bookDto.StockQuantity.Value;
            }

            if (!string.IsNullOrWhiteSpace(bookDto.Description))
            {
                book.Description = bookDto.Description;
            }

            return book;
        }

        /// <inheritdoc />
        public Book? UpdateBookStock(int id, BookStockUpdateDto stockUpdate)
        {
            var book = _books.FirstOrDefault(b => b.Id == id);
            if (book == null)
            {
                return null;
            }

            // 更新库存
            var newStock = book.StockQuantity + stockUpdate.QuantityChange;
            if (newStock < 0)
            {
                throw new InvalidOperationException("库存不足，无法减少库存");
            }

            book.StockQuantity = newStock;
            return book;
        }

        /// <inheritdoc />
        public bool DeleteBook(int id)
        {
            var book = _books.FirstOrDefault(b => b.Id == id);
            if (book == null)
            {
                return false;
            }

            return _books.Remove(book);
        }

        /// <inheritdoc />
        public int DeleteBooks(List<int> ids)
        {
            var count = 0;
            foreach (var id in ids)
            {
                if (DeleteBook(id))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
