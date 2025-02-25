using System;

namespace ODataWebApi.WebApplication1.Models
{
    [Flags]
    public enum CustomerType
    {
        None = 1,
        Premium = 2,
        VIP = 4
    }
}
