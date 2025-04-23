using Microsoft.AspNetCore.Mvc;
using OrderManagement.Models;
using OrderManagement.Services;
using System;
using System.Collections.Generic;

namespace OrderManagement.Controllers
{
    /// <summary>
    /// 订单管理 Controller，提供订单相关的所有操作
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// 获取订单列表（支持分页和按状态筛选）
        /// </summary>
        /// <param name="pageIndex">页码，从0开始</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="status">（可选）订单状态过滤</param>
        /// <returns>订单列表</returns>
        [HttpGet]
        public ActionResult<List<Order>> GetOrders([FromQuery] int pageIndex = 0, [FromQuery] int pageSize = 10, [FromQuery] string status = null)
        {
            var result = _orderService.GetOrders(pageIndex, pageSize, status);
            return Ok(result);
        }

        /// <summary>
        /// 获取指定订单详情
        /// </summary>
        /// <param name="id">订单Id</param>
        /// <returns>订单详情</returns>
        [HttpGet("{id}")]
        public ActionResult<Order> GetOrderById(Guid id)
        {
            var order = _orderService.GetOrderById(id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        /// <summary>
        /// 创建新订单
        /// </summary>
        /// <param name="order">订单信息（无需填写Id、状态、创建时间）</param>
        /// <returns>新建订单</returns>
        [HttpPost]
        public ActionResult<Order> CreateOrder([FromBody] Order order)
        {
            var created = _orderService.CreateOrder(order);
            return CreatedAtAction(nameof(GetOrderById), new { id = created.Id }, created);
        }

        /// <summary>
        /// 更新订单信息（不允许修改状态和创建时间）
        /// </summary>
        /// <param name="order">订单信息（需包含Id）</param>
        /// <returns>是否成功</returns>
        [HttpPut]
        public ActionResult<bool> UpdateOrder([FromBody] Order order)
        {
            var result = _orderService.UpdateOrder(order);
            if (!result) return NotFound();
            return Ok(true);
        }

        /// <summary>
        /// 删除订单
        /// </summary>
        /// <param name="id">订单Id</param>
        /// <returns>是否成功</returns>
        [HttpDelete("{id}")]
        public ActionResult<bool> DeleteOrder(Guid id)
        {
            var result = _orderService.DeleteOrder(id);
            if (!result) return NotFound();
            return Ok(true);
        }

        /// <summary>
        /// 修改订单状态（如支付、发货、取消、完成等）
        /// </summary>
        /// <param name="id">订单Id</param>
        /// <param name="newStatus">新状态</param>
        /// <param name="operatorName">操作人</param>
        /// <returns>是否成功</returns>
        [HttpPost("{id}/status")]
        public ActionResult<bool> ChangeOrderStatus(Guid id, [FromQuery] string newStatus, [FromQuery] string operatorName)
        {
            var result = _orderService.ChangeOrderStatus(id, newStatus, operatorName);
            if (!result) return NotFound();
            return Ok(true);
        }

        /// <summary>
        /// 查询订单状态变更历史
        /// </summary>
        /// <param name="id">订单Id</param>
        /// <returns>状态流转历史</returns>
        [HttpGet("{id}/status-history")]
        public ActionResult<List<OrderStatusHistory>> GetOrderStatusHistory(Guid id)
        {
            var result = _orderService.GetOrderStatusHistory(id);
            return Ok(result);
        }

        /// <summary>
        /// 添加或修改订单备注
        /// </summary>
        /// <param name="id">订单Id</param>
        /// <param name="remark">备注内容</param>
        /// <returns>是否成功</returns>
        [HttpPost("{id}/remark")]
        public ActionResult<bool> AddRemark(Guid id, [FromQuery] string remark)
        {
            var result = _orderService.AddRemark(id, remark);
            if (!result) return NotFound();
            return Ok(true);
        }
    }
}
