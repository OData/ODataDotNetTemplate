using Microsoft.OData.Edm;
#if(ODataVersion7x)
using Microsoft.AspNet.OData.Builder;
#else
using Microsoft.OData.ModelBuilder;
using ODataWebApi.WebApplication1.Models;
#endif

namespace ODataWebApi.WebApplication1
{
    public class EdmModelBuilder
    {
        // Learn more about OData Model Builder: https://learn.microsoft.com/odata/webapi/model-builder-abstract
        public static IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.Namespace = "NS";
            builder.EntitySet<Customer>("Customers");
            builder.EntitySet<Order>("Orders");

            var customerType = builder.EntityType<Customer>();

            // Define the Bound function to a single entity
            customerType
                .Function("GetCustomerOrdersTotalAmount")
                .Returns<int>();

            // Define theBound function to collection
            customerType
                .Collection
                .Function("GetCustomerByName")
                .ReturnsFromEntitySet<Customer>("Customers")
                .Parameter<string>("name");

            return builder.GetEdmModel();
        }
    }
}