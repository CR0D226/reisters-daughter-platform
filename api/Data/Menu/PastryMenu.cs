using ReistersDaughter.Api.Models;

namespace ReistersDaughter.Api.Data.Menu;

public static class PastryMenu
{
    public static List<MenuItem> Items =>
    [
        new MenuItem
        {
            Id = 4001,
            Name = "Apple Crumble",
            Description = "GF/Vegan",
            Price = 4.25m,
            Category = "Food",
            Subcategory = "Pastries",
            Vegan = true,
            GlutenFree = true
        },

        new MenuItem
        {
            Id = 4002,
            Name = "Blueberry Muffin",
            Price = 3.75m,
            Category = "Food",
            Subcategory = "Pastries"
        },

        new MenuItem
        {
            Id = 4003,
            Name = "Chocolate Croissant",
            Price = 4.25m,
            Category = "Food",
            Subcategory = "Pastries"
        },

        new MenuItem
        {
            Id = 4004,
            Name = "Butter Croissant",
            Price = 3.75m,
            Category = "Food",
            Subcategory = "Pastries"
        },

        new MenuItem
        {
            Id = 4005,
            Name = "Cinnamon Roll",
            Price = 4.50m,
            Category = "Food",
            Subcategory = "Pastries"
        },

        new MenuItem
        {
            Id = 4006,
            Name = "Scone",
            Price = 3.75m,
            Category = "Food",
            Subcategory = "Pastries"
        }
    ];
}