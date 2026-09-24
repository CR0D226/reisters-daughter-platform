using ReistersDaughter.Api.Models;
using ReistersDaughter.Api.Data.Menu;

namespace ReistersDaughter.Api.Data;

public static class MenuData
{
    public static List<MenuItem> GetMenuItems()
    {
        return
        [
            .. CoffeeMenu.Items,
            .. TeaMenu.Items,
            .. SmoothieMenu.Items,
            .. PastryMenu.Items
        ];
    }
}