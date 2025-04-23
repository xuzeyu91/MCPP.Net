using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models
{
    /// <summary>
    /// 创建图书请求DTO
    /// </summary>
    public class CreateBookDto
    {
        /// <summary>
        /// 图书ISBN编号
        /// </summary>
        [Required(ErrorMessage = "ISBN编号不能为空")]
        [StringLength(20, ErrorMessage = "ISBN编号长度不能超过20个字符")]
        public string ISBN { get; set; } = string.Empty;

        /// <summary>
        /// 图书标题
        /// </summary>
        [Required(ErrorMessage = "图书标题不能为空")]
        [StringLength(100, ErrorMessage = "图书标题长度不能超过100个字符")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 图书作者
        /// </summary>
        [Required(ErrorMessage = "作者不能为空")]
        [StringLength(50, ErrorMessage = "作者名称长度不能超过50个字符")]
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// 出版日期
        /// </summary>
        [Required(ErrorMessage = "出版日期不能为空")]
        public DateTime PublishDate { get; set; }

        /// <summary>
        /// 图书分类
        /// </summary>
        [Required(ErrorMessage = "图书分类不能为空")]
        [StringLength(30, ErrorMessage = "分类名称长度不能超过30个字符")]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 图书价格
        /// </summary>
        [Required(ErrorMessage = "图书价格不能为空")]
        [Range(0.01, 10000, ErrorMessage = "图书价格必须在0.01到10000之间")]
        public decimal Price { get; set; }

        /// <summary>
        /// 库存数量
        /// </summary>
        [Required(ErrorMessage = "库存数量不能为空")]
        [Range(0, 10000, ErrorMessage = "库存数量必须在0到10000之间")]
        public int StockQuantity { get; set; }

        /// <summary>
        /// 图书简介
        /// </summary>
        [StringLength(1000, ErrorMessage = "图书简介长度不能超过1000个字符")]
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// 更新图书请求DTO
    /// </summary>
    public class UpdateBookDto
    {
        /// <summary>
        /// 图书标题
        /// </summary>
        [StringLength(100, ErrorMessage = "图书标题长度不能超过100个字符")]
        public string? Title { get; set; }

        /// <summary>
        /// 图书作者
        /// </summary>
        [StringLength(50, ErrorMessage = "作者名称长度不能超过50个字符")]
        public string? Author { get; set; }

        /// <summary>
        /// 图书分类
        /// </summary>
        [StringLength(30, ErrorMessage = "分类名称长度不能超过30个字符")]
        public string? Category { get; set; }

        /// <summary>
        /// 图书价格
        /// </summary>
        [Range(0.01, 10000, ErrorMessage = "图书价格必须在0.01到10000之间")]
        public decimal? Price { get; set; }

        /// <summary>
        /// 库存数量
        /// </summary>
        [Range(0, 10000, ErrorMessage = "库存数量必须在0到10000之间")]
        public int? StockQuantity { get; set; }

        /// <summary>
        /// 图书简介
        /// </summary>
        [StringLength(1000, ErrorMessage = "图书简介长度不能超过1000个字符")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// 图书库存更新DTO
    /// </summary>
    public class BookStockUpdateDto
    {
        /// <summary>
        /// 库存变更数量（正数为增加，负数为减少）
        /// </summary>
        [Required(ErrorMessage = "库存变更数量不能为空")]
        public int QuantityChange { get; set; }

        /// <summary>
        /// 变更原因
        /// </summary>
        [StringLength(200, ErrorMessage = "变更原因长度不能超过200个字符")]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 图书查询参数DTO
    /// </summary>
    public class BookQueryDto
    {
        /// <summary>
        /// 图书标题（模糊查询）
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// 图书作者（模糊查询）
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// 图书分类
        /// </summary>
        public string? Category { get; set; }

        /// <summary>
        /// 最低价格
        /// </summary>
        public decimal? MinPrice { get; set; }

        /// <summary>
        /// 最高价格
        /// </summary>
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// 页码（从1开始）
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// 每页记录数
        /// </summary>
        public int PageSize { get; set; } = 10;
    }
}
