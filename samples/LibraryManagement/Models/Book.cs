using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models
{
    /// <summary>
    /// 图书实体模型
    /// </summary>
    public class Book
    {
        /// <summary>
        /// 图书ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 图书ISBN编号
        /// </summary>
        public string ISBN { get; set; } = string.Empty;

        /// <summary>
        /// 图书标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 图书作者
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// 出版日期
        /// </summary>
        public DateTime PublishDate { get; set; }

        /// <summary>
        /// 图书分类
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 图书价格
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 库存数量
        /// </summary>
        public int StockQuantity { get; set; }

        /// <summary>
        /// 图书简介
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}
