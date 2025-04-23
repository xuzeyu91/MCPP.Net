using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using LibraryManagement.Models;
using LibraryManagement.Services;

namespace LibraryManagement.Controllers
{
    /// <summary>
    /// 图书管理控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class BookController : ControllerBase
    {
        private readonly IBookService _bookService;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="bookService">图书服务</param>
        public BookController(IBookService bookService)
        {
            _bookService = bookService;
        }

        /// <summary>
        /// 获取所有图书
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// GET /api/book
        /// </remarks>
        /// <returns>所有图书列表</returns>
        /// <response code="200">返回所有图书</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<Book>>), StatusCodes.Status200OK)]
        public IActionResult GetAllBooks()
        {
            var books = _bookService.GetAllBooks();
            return Ok(ApiResponse<List<Book>>.CreateSuccess(books));
        }

        /// <summary>
        /// 根据ID获取图书
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// GET /api/book/1
        /// </remarks>
        /// <param name="id">图书ID</param>
        /// <returns>图书信息</returns>
        /// <response code="200">返回找到的图书</response>
        /// <response code="404">未找到指定ID的图书</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<Book>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public IActionResult GetBookById([FromRoute] int id)
        {
            var book = _bookService.GetBookById(id);
            if (book == null)
            {
                return NotFound(ApiResponse<object>.CreateFailed($"未找到ID为{id}的图书", 404));
            }

            return Ok(ApiResponse<Book>.CreateSuccess(book));
        }

        /// <summary>
        /// 根据ISBN获取图书
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// GET /api/book/isbn/9787302164340
        /// </remarks>
        /// <param name="isbn">ISBN编号</param>
        /// <returns>图书信息</returns>
        /// <response code="200">返回找到的图书</response>
        /// <response code="404">未找到指定ISBN的图书</response>
        [HttpGet("isbn/{isbn}")]
        [ProducesResponseType(typeof(ApiResponse<Book>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public IActionResult GetBookByISBN([FromRoute] string isbn)
        {
            var book = _bookService.GetBookByISBN(isbn);
            if (book == null)
            {
                return NotFound(ApiResponse<object>.CreateFailed($"未找到ISBN为{isbn}的图书", 404));
            }

            return Ok(ApiResponse<Book>.CreateSuccess(book));
        }

        /// <summary>
        /// 分页查询图书
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// GET /api/book/query?title=ASP.NET&amp;category=Web开发&amp;pageNumber=1&amp;pageSize=10
        /// </remarks>
        /// <param name="title">图书标题</param>
        /// <param name="author">图书作者</param>
        /// <param name="category">图书分类</param>
        /// <param name="minPrice">最低价格</param>
        /// <param name="maxPrice">最高价格</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页记录数</param>
        /// <returns>分页图书列表</returns>
        /// <response code="200">返回分页图书列表</response>
        [HttpGet("query")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<Book>>), StatusCodes.Status200OK)]
        public IActionResult QueryBooks(
            [FromQuery] string? title,
            [FromQuery] string? author,
            [FromQuery] string? category,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new BookQueryDto
            {
                Title = title,
                Author = author,
                Category = category,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                PageNumber = pageNumber < 1 ? 1 : pageNumber,
                PageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize)
            };

            var result = _bookService.QueryBooks(query);
            return Ok(ApiResponse<PagedResult<Book>>.CreateSuccess(result));
        }

        /// <summary>
        /// 创建图书
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// 
        /// POST /api/book
        /// Content-Type: application/json
        /// 
        /// {
        ///   "isbn": "9787111636663",
        ///   "title": "深入理解计算机系统",
        ///   "author": "Randal E. Bryant",
        ///   "publishDate": "2020-01-01T00:00:00",
        ///   "category": "计算机科学",
        ///   "price": 139.00,
        ///   "stockQuantity": 50,
        ///   "description": "本书是计算机科学领域的经典教材"
        /// }
        /// </remarks>
        /// <param name="bookDto">图书信息</param>
        /// <returns>创建的图书</returns>
        /// <response code="201">返回创建的图书</response>
        /// <response code="400">请求数据验证失败</response>
        /// <response code="409">ISBN已存在</response>
        [HttpPost]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(ApiResponse<Book>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public IActionResult CreateBook([FromBody] CreateBookDto bookDto)
        {
            try
            {
                var book = _bookService.CreateBook(bookDto);
                return CreatedAtAction(nameof(GetBookById), new { id = book.Id }, ApiResponse<Book>.CreateSuccess(book, "图书创建成功"));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse<object>.CreateFailed(ex.Message, 409));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.CreateFailed(ex.Message));
            }
        }

        /// <summary>
        /// 通过表单创建图书
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// 
        /// POST /api/book/form
        /// Content-Type: application/x-www-form-urlencoded
        /// 
        /// isbn=9787111636663&amp;title=深入理解计算机系统&amp;author=Randal E. Bryant&amp;publishDate=2020-01-01&amp;category=计算机科学&amp;price=139.00&amp;stockQuantity=50&amp;description=本书是计算机科学领域的经典教材
        /// </remarks>
        /// <param name="isbn">ISBN编号</param>
        /// <param name="title">图书标题</param>
        /// <param name="author">图书作者</param>
        /// <param name="publishDate">出版日期</param>
        /// <param name="category">图书分类</param>
        /// <param name="price">图书价格</param>
        /// <param name="stockQuantity">库存数量</param>
        /// <param name="description">图书简介</param>
        /// <returns>创建的图书</returns>
        /// <response code="201">返回创建的图书</response>
        /// <response code="400">请求数据验证失败</response>
        /// <response code="409">ISBN已存在</response>
        [HttpPost("form")]
        [Consumes("application/x-www-form-urlencoded")]
        [ProducesResponseType(typeof(ApiResponse<Book>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
        public IActionResult CreateBookFromForm(
            [FromForm][Required] string isbn,
            [FromForm][Required] string title,
            [FromForm][Required] string author,
            [FromForm][Required] DateTime publishDate,
            [FromForm][Required] string category,
            [FromForm][Required][Range(0.01, 10000)] decimal price,
            [FromForm][Required][Range(0, 10000)] int stockQuantity,
            [FromForm] string? description)
        {
            try
            {
                var bookDto = new CreateBookDto
                {
                    ISBN = isbn,
                    Title = title,
                    Author = author,
                    PublishDate = publishDate,
                    Category = category,
                    Price = price,
                    StockQuantity = stockQuantity,
                    Description = description ?? string.Empty
                };

                var book = _bookService.CreateBook(bookDto);
                return CreatedAtAction(nameof(GetBookById), new { id = book.Id }, ApiResponse<Book>.CreateSuccess(book, "图书创建成功"));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ApiResponse<object>.CreateFailed(ex.Message, 409));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.CreateFailed(ex.Message));
            }
        }

        /// <summary>
        /// 更新图书信息
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// 
        /// PUT /api/book/1
        /// Content-Type: application/json
        /// 
        /// {
        ///   "title": "算法导论（第三版）",
        ///   "price": 99.00,
        ///   "stockQuantity": 120
        /// }
        /// </remarks>
        /// <param name="id">图书ID</param>
        /// <param name="bookDto">更新的图书信息</param>
        /// <returns>更新后的图书</returns>
        /// <response code="200">返回更新后的图书</response>
        /// <response code="400">请求数据验证失败</response>
        /// <response code="404">未找到指定ID的图书</response>
        [HttpPut("{id}")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(ApiResponse<Book>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public IActionResult UpdateBook([FromRoute] int id, [FromBody] UpdateBookDto bookDto)
        {
            try
            {
                var book = _bookService.UpdateBook(id, bookDto);
                if (book == null)
                {
                    return NotFound(ApiResponse<object>.CreateFailed($"未找到ID为{id}的图书", 404));
                }

                return Ok(ApiResponse<Book>.CreateSuccess(book, "图书更新成功"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.CreateFailed(ex.Message));
            }
        }

        /// <summary>
        /// 通过表单更新图书信息
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// 
        /// PUT /api/book/1/form
        /// Content-Type: application/x-www-form-urlencoded
        /// 
        /// title=算法导论（第三版）&amp;price=99.00&amp;stockQuantity=120
        /// </remarks>
        /// <param name="id">图书ID</param>
        /// <param name="title">图书标题</param>
        /// <param name="author">图书作者</param>
        /// <param name="category">图书分类</param>
        /// <param name="price">图书价格</param>
        /// <param name="stockQuantity">库存数量</param>
        /// <param name="description">图书简介</param>
        /// <returns>更新后的图书</returns>
        /// <response code="200">返回更新后的图书</response>
        /// <response code="400">请求数据验证失败</response>
        /// <response code="404">未找到指定ID的图书</response>
        [HttpPut("{id}/form")]
        [Consumes("application/x-www-form-urlencoded")]
        [ProducesResponseType(typeof(ApiResponse<Book>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public IActionResult UpdateBookFromForm(
            [FromRoute] int id,
            [FromForm] string? title,
            [FromForm] string? author,
            [FromForm] string? category,
            [FromForm] decimal? price,
            [FromForm] int? stockQuantity,
            [FromForm] string? description)
        {
            try
            {
                var bookDto = new UpdateBookDto
                {
                    Title = title,
                    Author = author,
                    Category = category,
                    Price = price,
                    StockQuantity = stockQuantity,
                    Description = description
                };

                var book = _bookService.UpdateBook(id, bookDto);
                if (book == null)
                {
                    return NotFound(ApiResponse<object>.CreateFailed($"未找到ID为{id}的图书", 404));
                }

                return Ok(ApiResponse<Book>.CreateSuccess(book, "图书更新成功"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.CreateFailed(ex.Message));
            }
        }

        /// <summary>
        /// 部分更新图书信息
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// 
        /// PATCH /api/book/1
        /// Content-Type: application/json
        /// 
        /// {
        ///   "title": "算法导论（第四版）",
        ///   "price": 109.00
        /// }
        /// </remarks>
        /// <param name="id">图书ID</param>
        /// <param name="bookDto">部分更新的图书信息</param>
        /// <returns>更新后的图书</returns>
        /// <response code="200">返回更新后的图书</response>
        /// <response code="400">请求数据验证失败</response>
        /// <response code="404">未找到指定ID的图书</response>
        [HttpPatch("{id}")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(ApiResponse<Book>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public IActionResult PatchBook([FromRoute] int id, [FromBody] UpdateBookDto bookDto)
        {
            try
            {
                var book = _bookService.UpdateBook(id, bookDto);
                if (book == null)
                {
                    return NotFound(ApiResponse<object>.CreateFailed($"未找到ID为{id}的图书", 404));
                }

                return Ok(ApiResponse<Book>.CreateSuccess(book, "图书更新成功"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.CreateFailed(ex.Message));
            }
        }

        /// <summary>
        /// 更新图书库存
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// 
        /// PATCH /api/book/1/stock
        /// Content-Type: application/json
        /// 
        /// {
        ///   "quantityChange": 10,
        ///   "reason": "采购入库"
        /// }
        /// </remarks>
        /// <param name="id">图书ID</param>
        /// <param name="stockUpdate">库存更新信息</param>
        /// <returns>更新后的图书</returns>
        /// <response code="200">返回更新后的图书</response>
        /// <response code="400">请求数据验证失败</response>
        /// <response code="404">未找到指定ID的图书</response>
        [HttpPatch("{id}/stock")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(ApiResponse<Book>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public IActionResult UpdateBookStock([FromRoute] int id, [FromBody] BookStockUpdateDto stockUpdate)
        {
            try
            {
                var book = _bookService.UpdateBookStock(id, stockUpdate);
                if (book == null)
                {
                    return NotFound(ApiResponse<object>.CreateFailed($"未找到ID为{id}的图书", 404));
                }

                return Ok(ApiResponse<Book>.CreateSuccess(book, "图书库存更新成功"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.CreateFailed(ex.Message));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.CreateFailed(ex.Message));
            }
        }

        /// <summary>
        /// 删除图书
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// DELETE /api/book/1
        /// </remarks>
        /// <param name="id">图书ID</param>
        /// <returns>无内容</returns>
        /// <response code="204">删除成功</response>
        /// <response code="404">未找到指定ID的图书</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public IActionResult DeleteBook([FromRoute] int id)
        {
            var success = _bookService.DeleteBook(id);
            if (!success)
            {
                return NotFound(ApiResponse<object>.CreateFailed($"未找到ID为{id}的图书", 404));
            }

            return NoContent();
        }

        /// <summary>
        /// 批量删除图书
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// 
        /// DELETE /api/book/batch
        /// Content-Type: application/json
        /// 
        /// [1, 2, 3]
        /// </remarks>
        /// <param name="ids">图书ID列表</param>
        /// <returns>删除结果</returns>
        /// <response code="200">返回删除结果</response>
        /// <response code="400">请求数据验证失败</response>
        [HttpDelete("batch")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public IActionResult DeleteBooks([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest(ApiResponse<object>.CreateFailed("图书ID列表不能为空"));
            }

            var count = _bookService.DeleteBooks(ids);
            return Ok(ApiResponse<object>.CreateSuccess(new { DeletedCount = count, TotalCount = ids.Count }, $"成功删除{count}本图书"));
        }

        /// <summary>
        /// 检查图书是否存在
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// HEAD /api/book/1
        /// </remarks>
        /// <param name="id">图书ID</param>
        /// <returns>无内容</returns>
        /// <response code="200">图书存在</response>
        /// <response code="404">图书不存在</response>
        [HttpHead("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult CheckBookExists([FromRoute] int id)
        {
            var book = _bookService.GetBookById(id);
            if (book == null)
            {
                return NotFound();
            }

            return Ok();
        }

        /// <summary>
        /// 获取图书封面图片
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// GET /api/book/1/cover
        /// </remarks>
        /// <param name="id">图书ID</param>
        /// <returns>图书封面图片</returns>
        /// <response code="200">返回图书封面图片</response>
        /// <response code="404">未找到指定ID的图书</response>
        [HttpGet("{id}/cover")]
        [Produces("image/jpeg")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetBookCover([FromRoute] int id)
        {
            var book = _bookService.GetBookById(id);
            if (book == null)
            {
                return NotFound(ApiResponse<object>.CreateFailed($"未找到ID为{id}的图书", 404));
            }

            // 这里只是模拟返回图片，实际应该从文件系统或数据库中获取
            var placeholderImageBytes = new byte[1024]; // 模拟图片数据
            return File(placeholderImageBytes, "image/jpeg");
        }

        /// <summary>
        /// 通过请求头获取图书信息
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// 
        /// GET /api/book/by-header
        /// X-Book-ISBN: 9787302164340
        /// </remarks>
        /// <param name="isbn">ISBN编号</param>
        /// <returns>图书信息</returns>
        /// <response code="200">返回找到的图书</response>
        /// <response code="400">请求头缺失</response>
        /// <response code="404">未找到指定ISBN的图书</response>
        [HttpGet("by-header")]
        [ProducesResponseType(typeof(ApiResponse<Book>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public IActionResult GetBookByHeader([FromHeader(Name = "X-Book-ISBN")] string? isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
            {
                return BadRequest(ApiResponse<object>.CreateFailed("请求头 X-Book-ISBN 不能为空"));
            }

            var book = _bookService.GetBookByISBN(isbn);
            if (book == null)
            {
                return NotFound(ApiResponse<object>.CreateFailed($"未找到ISBN为{isbn}的图书", 404));
            }

            return Ok(ApiResponse<Book>.CreateSuccess(book));
        }

        /// <summary>
        /// 获取图书详细信息
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// 
        /// GET /api/book/1/details?format=full
        /// X-API-Version: 1.0
        /// </remarks>
        /// <param name="id">图书ID</param>
        /// <param name="format">返回格式</param>
        /// <param name="apiVersion">API版本</param>
        /// <returns>图书详细信息</returns>
        /// <response code="200">返回图书详细信息</response>
        /// <response code="404">未找到指定ID的图书</response>
        [HttpGet("{id}/details")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public IActionResult GetBookDetails(
            [FromRoute] int id,
            [FromQuery] string? format,
            [FromHeader(Name = "X-API-Version")] string? apiVersion)
        {
            var book = _bookService.GetBookById(id);
            if (book == null)
            {
                return NotFound(ApiResponse<object>.CreateFailed($"未找到ID为{id}的图书", 404));
            }

            // 根据format参数决定返回的数据格式
            var isFull = string.Equals(format, "full", StringComparison.OrdinalIgnoreCase);
            
            object result;
            if (isFull)
            {
                result = new
                {
                    book.Id,
                    book.ISBN,
                    book.Title,
                    book.Author,
                    book.PublishDate,
                    book.Category,
                    book.Price,
                    book.StockQuantity,
                    book.Description,
                    ApiVersion = apiVersion ?? "未指定"
                };
            }
            else
            {
                result = new
                {
                    book.Id,
                    book.Title,
                    book.Author,
                    ApiVersion = apiVersion ?? "未指定"
                };
            }

            return Ok(ApiResponse<object>.CreateSuccess(result));
        }

        /// <summary>
        /// 获取支持的图书分类
        /// </summary>
        /// <remarks>
        /// 示例请求:
        /// OPTIONS /api/book/categories
        /// </remarks>
        /// <returns>支持的操作和图书分类</returns>
        [HttpOptions("categories")]
        public IActionResult GetBookCategoriesOptions()
        {
            Response.Headers.Append("Allow", "GET, OPTIONS");
            
            var categories = new[]
            {
                "计算机科学",
                "编程语言",
                "Web开发",
                "数据库",
                "人工智能",
                "网络安全",
                "操作系统",
                "软件工程"
            };
            
            return Ok(ApiResponse<string[]>.CreateSuccess(categories));
        }
    }
}
