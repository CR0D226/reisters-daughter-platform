using ReistersDaughter.Api.Models;

namespace ReistersDaughter.Api.Data.Menu;

public static class SmoothieMenu
{
    public static List<MenuItem> Items =>
    [
        new MenuItem
        {
            Id = 3001,
            Name = "Very Berry Smoothie",
            Description = "Mixed berries, pineapple & orange juice.",
            Price = 5.95m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Smoothies"
        },

        new MenuItem
        {
            Id = 3002,
            Name = "Tropical Smoothie",
            Description = "Mango, pineapple, banana & orange juice.",
            Price = 5.95m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Smoothies"
        },

        new MenuItem
        {
            Id = 3003,
            Name = "Strawberry Banana Smoothie",
            Description = "Strawberries & bananas blended with your choice of milk.",
            Price = 5.75m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Smoothies"
        },

        new MenuItem
        {
            Id = 3004,
            Name = "BCP Smoothie",
            Description = "Banana, chocolate & peanut butter blended with your choice of milk.",
            Price = 5.75m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Smoothies"
        }
    ];
}