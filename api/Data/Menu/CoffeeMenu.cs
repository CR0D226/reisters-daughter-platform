using ReistersDaughter.Api.Models;

namespace ReistersDaughter.Api.Data.Menu;

public static class CoffeeMenu
{
    public static List<MenuItem> Items =>
    [
        // Coffee
        new MenuItem
        {
            Id = 1001,
            Name = "Drip Coffee",
            Price = 2.95m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Coffee"
        },

        new MenuItem
        {
            Id = 1002,
            Name = "Iced Coffee",
            Description = "If wanting decaf, please select the decaf option.",
            Price = 2.85m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Coffee"
        },

        new MenuItem
        {
            Id = 1003,
            Name = "Cold Brew",
            Price = 4.14m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Coffee"
        },

        new MenuItem
        {
            Id = 1004,
            Name = "Hot Red Eye",
            Description = "12oz coffee with espresso (double shot).",
            Price = 4.50m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Coffee"
        },

        new MenuItem
        {
            Id = 1005,
            Name = "Iced Red Eye",
            Price = 4.50m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Coffee"
        },

        new MenuItem
        {
            Id = 1006,
            Name = "Apple Cider Iced Coffee",
            Description = "A brisk, spiced iced coffee with apple notes.",
            Price = 3.45m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Coffee"
        },

        new MenuItem
        {
            Id = 1007,
            Name = "Coco Cold Brew",
            Description = "Our smooth house-made cold brew with toasted coconut syrup and a refreshing splash of coconut milk.",
            Price = 4.90m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Coffee",
        },

        new MenuItem
        {
            Id = 1008,
            Name = "96oz Coffee",
            Description = "We ask that you please provide 30 minutes notice prior to ordering.",
            Price = 18.00m,
            Category = "Drinks",
            Subcategory = "Coffee"
        },

        new MenuItem
        {
            Id = 1009,
            Name = "Extra Shot",
            Description = "Please note, all shots are double shots.",
            Price = 1.75m,
            Category = "Drinks",
            Subcategory = "Coffee"
        },

        new MenuItem
        {
            Id = 1010,
            Name = "Nitro",
            Price = 4.75m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Coffee"
        },

        // Espresso
        new MenuItem
        {
            Id = 1101,
            Name = "Cappuccino",
            Description = "Made with a double shot of espresso and 4oz steamed milk.",
            Price = 4.25m,
            Category = "Drinks",
            Subcategory = "Espresso"
        },

        new MenuItem
        {
            Id = 1102,
            Name = "Latte",
            Price = 4.75m,
            Category = "Drinks",
            Subcategory = "Espresso"
        },

        new MenuItem
        {
            Id = 1103,
            Name = "Americano Hot",
            Description = "12oz espresso with water (double shot). 16oz espresso with water (3 shots).",
            Price = 3.25m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Espresso"
        },

        new MenuItem
        {
            Id = 1104,
            Name = "Americano Iced",
            Description = "16oz double shot of espresso over water and ice. 24oz quad shot of espresso over water and ice.",
            Price = 3.25m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Espresso"
        },

        new MenuItem
        {
            Id = 1105,
            Name = "Sparkling Americano",
            Description = "A traditional Americano made with sparkling water for a bubbly coffee soda.",
            Price = 4.95m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Espresso"
        },

        // Specialty Lattes
        new MenuItem
        {
            Id = 1201,
            Name = "Apple Brown Sugar Latte",
            Description = "A fall twist on our classic brown sugar latte.",
            Price = 5.45m,
            Category = "Drinks",
            Subcategory = "Specialty Lattes"
        },

        new MenuItem
        {
            Id = 1202,
            Name = "Biscoff Latte",
            Description = "Sweetened with a warm, Biscoff cookie-inspired spiced syrup.",
            Price = 5.50m,
            Category = "Drinks",
            Subcategory = "Specialty Lattes",
        },

        new MenuItem
        {
            Id = 1203,
            Name = "Blueberry Waffle Latte",
            Description = "Made with pure maple syrup, house-made blueberry fruit syrup, milk of choice and topped with sweet cream and a graham cracker crumble.",
            Price = 6.55m,
            Category = "Drinks",
            Subcategory = "Specialty Lattes"
        },

        new MenuItem
        {
            Id = 1204,
            Name = "Bridgerton Latte",
            Description = "Oat milk, honey lavender latte. Best iced!",
            Price = 6.50m,
            Category = "Drinks",
            Subcategory = "Specialty Lattes",
        },

        new MenuItem
        {
            Id = 1205,
            Name = "Brown Sugar Latte",
            Description = "Brown sugar vanilla syrup.",
            Price = 5.35m,
            Category = "Drinks",
            Subcategory = "Specialty Lattes",
            Featured = true
        }
    ];
}