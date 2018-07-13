using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ep24.web.Models;
using ep24.web.Repositories;

namespace ep24.web.Controllers
{
    [Route("api/[controller]/[action]")]
    public class OrderController : Controller
    {
        private IProductRepository productRepo;
        private IOrderRepository orderRepo;

        public OrderController(IProductRepository productRepo, IOrderRepository orderRepo)
        {
            this.productRepo = productRepo;
            this.orderRepo = orderRepo;
        }

        [HttpGet("{referenceCode}")]
        public Order GetByReferenceCode(string referenceCode)
        {
            return orderRepo.Get(o => o.ReferenceCode == referenceCode);
        }

        [HttpGet("{username}")]
        public IEnumerable<Order> ListByUsername(string username)
        {
            return orderRepo.List(o => o.Username == username );
        }
        [HttpGet]
        public IEnumerable<Order> ListOrdering()
        {
            return orderRepo.List(o => !o.PaidDate.HasValue);
        }
        [HttpGet]
        public IEnumerable<Order> ListHistory()
        {
            //TODO: implement scenario ขอรายการสั่งซื้อที่ยืนยันรายการสั่งซื้อแล้ว;
            return orderRepo.List(o => o.PaidDate.HasValue);
        }
        [HttpPost("{id}")]
        public void AcceptOrder(String id)
        {
            if(String.IsNullOrWhiteSpace(id)) throw new NotImplementedException("ไม่พบรายการสั่งซื้อ");
            var order = orderRepo.Get(o => o.Id == id);
            if(order == null) throw new NotImplementedException("ไม่พบรายการสั่งซื้อ");

            order.PaidDate = DateTime.UtcNow;
            orderRepo.Update(order);
        }

        [HttpPost]
        public OrderProductResponse OrderProduct([FromBody]OrderProductRequest request)
        {
            //TODO: implement scenario ไม่มีข้อมูล หรือไม่เลือกสินค้าที่จะสั่ง ให้แจ้งกลับว่า 'ไม่พบเมนูที่จะสั่ง' และไม่บันทึกข้อมูล
            if(request == null || request.OrderedProducts == null || !request.OrderedProducts.Any())
            {
                return new OrderProductResponse{Message = "ไม่พบเมนูที่จะสั่ง", };
            }
            var productIds = request.OrderedProducts.Select(p => p.Key);
            var products = productRepo.GetAllProducts();
            var filteredProducts = products.Where(p => productIds.Contains(p.Id)).ToList();

            if (filteredProducts.Count() != productIds.Count())
            {
                return new OrderProductResponse { Message = "ไม่พบสินค้าบางรายการ กรุณาสั่งใหม่อีกครั้ง", };
            }

            if (filteredProducts.Any(p => p.HasStock && p.Stock < request.OrderedProducts.First(op => op.Key == p.Id).Value))
            {
                return new OrderProductResponse { Message = "สินค้าบางรายการมีไม่พอ กรุณาสั่งใหม่อีกครั้ง", };
            }
            var id = Guid.NewGuid().ToString();
            var order = new Order
            {
                Id = id,
                OrderedProducts = filteredProducts,
                ReferenceCode = id.Substring(0, 5),
                OrderDate = DateTime.UtcNow,
                Username = request.Username,
            };
            orderRepo.Create(order);
            
            return new OrderProductResponse { Message = "สั่งเมนูสำเร็จ กรุณาชำระเงินที่เค้าเตอร์", ReferenceCode = order.ReferenceCode, };
        }
    }
}
