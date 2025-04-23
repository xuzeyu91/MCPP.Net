using System;
using System.Collections.Generic;
using System.Linq;
using OrderManagement.Models;

namespace OrderManagement.Services
{
    /// <summary>
    /// 订单服务接口，定义订单相关操作
    /// </summary>
    public interface IOrderService
    {
        List<Order> GetOrders(int pageIndex, int pageSize, string status = null);
        Order GetOrderById(Guid id);
        Order CreateOrder(Order order);
        bool UpdateOrder(Order order);
        bool DeleteOrder(Guid id);
        bool ChangeOrderStatus(Guid id, string newStatus, string operatorName);
        List<OrderStatusHistory> GetOrderStatusHistory(Guid orderId);
        bool AddRemark(Guid id, string remark);
    }

    /// <summary>
    /// 订单服务实现，数据保存在内存中
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly List<Order> _orders;
        private readonly List<OrderStatusHistory> _statusHistories;

        public OrderService()
        {
            _orders = new List<Order>();
            _statusHistories = new List<OrderStatusHistory>();
            InitSampleData();
        }

        private void InitSampleData()
        {
            var order1 = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD20240423001",
                CreatedAt = DateTime.Now.AddDays(-2),
                Status = "Created",
                Remark = "首单赠品已发放",
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = Guid.NewGuid(), ProductName = "商品A", Quantity = 2, UnitPrice = 99.9m },
                    new OrderItem { ProductId = Guid.NewGuid(), ProductName = "商品B", Quantity = 1, UnitPrice = 199.0m }
                }
            };
            var order2 = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = "ORD20240423002",
                CreatedAt = DateTime.Now.AddDays(-1),
                Status = "Paid",
                Remark = "等待发货",
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = Guid.NewGuid(), ProductName = "商品C", Quantity = 3, UnitPrice = 59.9m }
                }
            };
            _orders.Add(order1);
            _orders.Add(order2);
            _statusHistories.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order1.Id,
                FromStatus = null,
                ToStatus = "Created",
                ChangedAt = order1.CreatedAt,
                Operator = "系统"
            });
            _statusHistories.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order2.Id,
                FromStatus = null,
                ToStatus = "Created",
                ChangedAt = order2.CreatedAt,
                Operator = "系统"
            });
            _statusHistories.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order2.Id,
                FromStatus = "Created",
                ToStatus = "Paid",
                ChangedAt = order2.CreatedAt.AddHours(2),
                Operator = "用户A"
            });
        }

        public List<Order> GetOrders(int pageIndex, int pageSize, string status = null)
        {
            var query = _orders.AsQueryable();
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }
            return query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        }

        public Order GetOrderById(Guid id)
        {
            return _orders.FirstOrDefault(o => o.Id == id);
        }

        public Order CreateOrder(Order order)
        {
            order.Id = Guid.NewGuid();
            order.CreatedAt = DateTime.Now;
            order.Status = "Created";
            _orders.Add(order);
            _statusHistories.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                FromStatus = null,
                ToStatus = "Created",
                ChangedAt = order.CreatedAt,
                Operator = "系统"
            });
            return order;
        }

        public bool UpdateOrder(Order order)
        {
            var existing = _orders.FirstOrDefault(o => o.Id == order.Id);
            if (existing == null) return false;
            existing.OrderNumber = order.OrderNumber;
            existing.Items = order.Items;
            existing.Remark = order.Remark;
            // 不允许直接修改状态和创建时间
            return true;
        }

        public bool DeleteOrder(Guid id)
        {
            var order = _orders.FirstOrDefault(o => o.Id == id);
            if (order != null)
            {
                _orders.Remove(order);
                return true;
            }
            return false;
        }

        public bool ChangeOrderStatus(Guid id, string newStatus, string operatorName)
        {
            var order = _orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return false;
            var oldStatus = order.Status;
            order.Status = newStatus;
            _statusHistories.Add(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                OrderId = id,
                FromStatus = oldStatus,
                ToStatus = newStatus,
                ChangedAt = DateTime.Now,
                Operator = operatorName
            });
            return true;
        }

        public List<OrderStatusHistory> GetOrderStatusHistory(Guid orderId)
        {
            return _statusHistories.Where(h => h.OrderId == orderId).OrderBy(h => h.ChangedAt).ToList();
        }

        public bool AddRemark(Guid id, string remark)
        {
            var order = _orders.FirstOrDefault(o => o.Id == id);
            if (order == null) return false;
            order.Remark = remark;
            return true;
        }
    }
}
