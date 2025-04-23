using System;
using System.Collections.Generic;

namespace OrderManagement.Models
{
    /// <summary>
    /// 订单主数据
    /// </summary>
    public class Order
    {
        /// <summary>
        /// 订单唯一标识
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 订单编号
        /// </summary>
        public string OrderNumber { get; set; }

        /// <summary>
        /// 订单创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 当前订单状态
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// 订单明细列表
        /// </summary>
        public List<OrderItem> Items { get; set; }

        /// <summary>
        /// 订单备注
        /// </summary>
        public string Remark { get; set; }
    }

    /// <summary>
    /// 订单明细
    /// </summary>
    public class OrderItem
    {
        /// <summary>
        /// 商品唯一标识
        /// </summary>
        public Guid ProductId { get; set; }

        /// <summary>
        /// 商品名称
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// 商品数量
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// 单价
        /// </summary>
        public decimal UnitPrice { get; set; }
    }

    /// <summary>
    /// 订单状态变更历史
    /// </summary>
    public class OrderStatusHistory
    {
        /// <summary>
        /// 记录唯一标识
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 订单Id
        /// </summary>
        public Guid OrderId { get; set; }

        /// <summary>
        /// 变更前状态
        /// </summary>
        public string FromStatus { get; set; }

        /// <summary>
        /// 变更后状态
        /// </summary>
        public string ToStatus { get; set; }

        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime ChangedAt { get; set; }

        /// <summary>
        /// 操作人
        /// </summary>
        public string Operator { get; set; }
    }
}
