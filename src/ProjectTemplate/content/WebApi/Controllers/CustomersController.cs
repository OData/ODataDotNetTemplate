using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Deltas;

#if (ODataVersion7x)
using Microsoft.AspNet.OData;
#else
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
#endif
using Microsoft.Extensions.Logging;
using ODataWebApi.WebApplication1.Models;

namespace ODataWebApi.WebApplication1.Controllers
{
    [ApiController]
    public class CustomersController : ODataController
    {
        private readonly ILogger<CustomersController> _logger;

        public CustomersController(ILogger<CustomersController> logger)
        {
            _logger = logger;
        }

        // GET: odata/Customers
        [EnableQuery(PageSize = 10)]
        [HttpGet("odata/Customers")]
#if (EnableOpenAPI)
        [ApiConventionMethod(typeof(DefaultApiConventions),
                     nameof(DefaultApiConventions.Get))]
#endif
        public IActionResult AllCustomers()
        {
            return Ok(_customers);
        }

        // GET: odata/Customers(1)
        [EnableQuery]
        [HttpGet("odata/Customers({key})")]
#if (EnableOpenAPI)
        [ApiConventionMethod(typeof(DefaultApiConventions),
                     nameof(DefaultApiConventions.Get))]
#endif
        public IActionResult SingleCustomer([FromRoute] int key)
        {
            var customer = _customers.FirstOrDefault(c => c.Id == key);
            if (customer == null)
            {
                return NotFound("Customer not found.");
            }

            return Ok(customer);
        }

        // GET: odata/Customers(1)/GetCustomerOrdersTotalAmount
        [EnableQuery]
        [HttpGet("odata/Customers({key})/GetCustomerOrdersTotalAmount")]
#if (EnableOpenAPI)
        [ApiConventionMethod(typeof(DefaultApiConventions),
                     nameof(DefaultApiConventions.Get))]
#endif
        public IActionResult CalculateCustomerOrdersTotalAmount([FromRoute] int key)
        {
            var customer = _customers.FirstOrDefault(c => c.Id == key);
            if (customer == null)
            {
                return NotFound("Customer not found.");
            }

            return Ok(customer.Orders.Sum(o => o.Amount));
        }

        // GET: odata/Customers/GetCustomerByName(name='Customer1')
        [EnableQuery]
        [HttpGet("odata/Customers/GetCustomerByName(name={name})")]
#if (EnableOpenAPI)
        [ApiConventionMethod(typeof(DefaultApiConventions),
                     nameof(DefaultApiConventions.Get))]
#endif
        public IActionResult FetchCustomerByName([FromRoute] string name)
        {
            var customer = _customers.FirstOrDefault(c => c.Name == name);
            if (customer == null)
            {
                return NotFound("Customer not found.");
            }

            return Ok(customer);
        }

        // POST: odata/Customers
        [HttpPost("odata/Customers")]
#if (EnableOpenAPI)
        [ApiConventionMethod(typeof(DefaultApiConventions),
                     nameof(DefaultApiConventions.Post))]
#endif
        public IActionResult AddCustomer([FromBody] Customer customer)
        {
            if (!ModelState.IsValid && _customers.Any(c => c.Id == customer.Id))
            {
                return BadRequest(ModelState);
            }

            if (customer.Id <= 0)
            {
                customer.Id = _customers.Max(c => c.Id) + 1;
            }

            if (customer.Orders == null || customer.Orders.Count() == 0)
            {
                customer.Orders = new List<Order>
                {
                    new Order { Id = _customers.SelectMany(c => c.Orders).Max(o => o.Id) + 1, Amount = 300 },
                    new Order { Id = _customers.SelectMany(c => c.Orders).Max(o => o.Id) + 2, Amount = 400 }
                };
            }

            _customers.Add(customer);

            return Created(customer);
        }

        // PATCH odata/Customers/1
        [HttpPatch("odata/Customers({key})")]
#if (EnableOpenAPI)
        [ApiConventionMethod(typeof(DefaultApiConventions),
                     nameof(DefaultApiConventions.Update))]
#endif
        public IActionResult UpdateCustomer([FromRoute] int key, [FromBody] Delta<Customer> delta)
        {
            if (!ModelState.IsValid && _customers.Any(c => c.Id != key))
            {
                return BadRequest(ModelState);
            }

            var customer = _customers.FirstOrDefault(c => c.Id == key);
            if (customer == null)
            {
                return NotFound("Customer not found.");
            }

            var updatedCustomer = delta.Patch(customer);

            return Ok(updatedCustomer);
        }

        private static List<Customer> _customers = new List<Customer>
        {
            new Customer
            {
                Id = 1,
                Name = "Customer1",
                Type = CustomerType.None,
                Orders = new List<Order> { new Order { Id = 1, Amount = 100 } }
            },
            new Customer
            {
                Id = 2,
                Name = "Customer2",
                Type = CustomerType.Premium | CustomerType.VIP,
                Orders = new List<Order> { new Order { Id = 2, Amount = 500 } }
            },
            new Customer
            {
                Id = 3,
                Name = "Customer3",
                Type = CustomerType.Premium,
                Orders = new List<Order> { new Order { Id = 3, Amount = 400 } }
            }
        };
    }
}
