using System.Collections.Generic;

namespace ODataWebApi.WebApplication1.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public CustomerType? Type { get; set; }
        public List<Order> Orders { get; set; } = new List<Order>();
    }
}
