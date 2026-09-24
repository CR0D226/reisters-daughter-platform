using ReistersDaughter.Api.Models;

namespace ReistersDaughter.Api.Data.Menu;

public static class TeaMenu
{
    public static List<MenuItem> Items =>
    [
        // =========================
        // TEA
        // =========================

        new MenuItem
        {
            Id = 2001,
            Name = "Chamomile Tea",
            Description = "Herbal tea - unsweetened unless noted.",
            Price = 2.75m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2002,
            Name = "Coastal Fog",
            Description = "A variation on a traditional London Fog, made with our Wakoucha Japanese black tea, vanilla sweetened steamed milk & cinnamon.",
            Price = 4.70m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2003,
            Name = "Cup of Calm",
            Description = "This tea blend is to help promote relaxation & can help de-stress during the day. Herbs include chamomile, lavender, skullcap, lemon balm, red clover, rose buds & stevia leaf.",
            Price = 3.25m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2004,
            Name = "Earl Grey",
            Description = "Made with organic loose leaf black tea, bergamot oil and cornflowers.",
            Price = 4.00m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2005,
            Name = "Genmaicha",
            Description = "Japanese green tea with toasted rice.",
            Price = 3.50m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2006,
            Name = "Gyokuro",
            Description = "A mellow Japanese green tea with rich umami flavor.",
            Price = 3.50m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2007,
            Name = "Hibiscus",
            Description = "Herbal tea - unsweetened unless noted.",
            Price = 2.75m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2008,
            Name = "Hojicha",
            Description = "A roasted Japanese green tea - perfect for cold days!",
            Price = 3.25m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2009,
            Name = "Hojicha Latte",
            Description = "Made with powdered hojicha tea and steamed milk. This drink is more earthy than a matcha latte. Comes unsweetened unless noted.",
            Price = 4.50m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2010,
            Name = "Iced London Fog",
            Description = "Black iced tea sweetened with lavender flavored milk.",
            Price = 4.50m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2011,
            Name = "Iced Tea",
            Description = "Teas are prepared unsweetened and can be sweetened to order. Teas with an * indicate caffeine-free.",
            Price = 2.85m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2012,
            Name = "Immuni-Tea",
            Description = "This tea blend is to help boost your immune system & can be a great addition during cold & flu season. Herbs include goldenrod, rose hips, marshmallow root, calendula & sage.",
            Price = 3.50m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2013,
            Name = "Into The Garden",
            Description = "A floral mix of white tea, chamomile, rose petals & lemon verbena. Naturally sweetened with dried stevia leaves.",
            Price = 3.25m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2014,
            Name = "Jane's Creation",
            Description = "Invigorating blend of peppermint, lemon balm & chamomile. Lightly sweetened with honey.",
            Price = 3.00m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2015,
            Name = "Kombucha On Tap",
            Description = "Tart Cherry Ginger - from Wild Bay.",
            Price = 4.75m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2016,
            Name = "Mickey Wight",
            Description = "House-made mix of lemonade & Tropical Cabana green tea from Wight Tea.",
            Price = 4.00m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2017,
            Name = "Milk & Honey Latte",
            Description = "Chamomile steeped with steamed milk & honey.",
            Price = 4.15m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2018,
            Name = "Pemberley Mist",
            Description = "Green tea with tulsi, lemon herbs, hibiscus, and butterfly pea flower for a misty purple hue.",
            Price = 3.80m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2019,
            Name = "Peppermint",
            Description = "Herbal tea - unsweetened unless noted.",
            Price = 2.75m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2020,
            Name = "Sencha",
            Description = "A mellow Japanese green tea with brighter notes.",
            Price = 3.50m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2021,
            Name = "Sunset Soirée",
            Description = "Black tea blend with lemon balm, hibiscus & dried orange peel, unsweetened.",
            Price = 3.25m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2022,
            Name = "Wakoucha - Japanese Black Tea",
            Description = "Japanese black tea.",
            Price = 3.85m,
            Category = "Drinks",
            Subcategory = "Tea"
        },

        new MenuItem
        {
            Id = 2023,
            Name = "White Peony",
            Description = "A robust white tea.",
            Price = 3.85m,
            Category = "Drinks",
            Subcategory = "Tea"
        },


        // =========================
        // MATCHA + HOJICHA
        // =========================

        new MenuItem
        {
            Id = 2101,
            Name = "Matcha Latte",
            Description = "Made with real matcha powder and milk. Comes unsweetened unless noted.",
            Price = 5.75m,
            Category = "Drinks",
            Subcategory = "Matcha"
        },

        new MenuItem
        {
            Id = 2102,
            Name = "Iced Matcha",
            Description = "Made with real matcha powder and milk. Comes unsweetened unless noted.",
            Price = 5.75m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Matcha"
        },

        new MenuItem
        {
            Id = 2103,
            Name = "Hojicha Latte",
            Description = "Made with powdered hojicha tea and steamed milk. This drink is more earthy than a matcha latte. Comes unsweetened unless noted.",
            Price = 4.50m,
            Category = "Drinks",
            Subcategory = "Matcha"
        },

        new MenuItem
        {
            Id = 2104,
            Name = "24oz Hojicha Latte",
            Description = "Made with powdered hojicha tea and steamed milk. This drink is more earthy than a matcha latte. Comes unsweetened unless noted.",
            Price = 5.00m,
            Category = "Drinks",
            Subcategory = "Matcha"
        },

        new MenuItem
        {
            Id = 2105,
            Name = "Iced Blueberry Waffle Matcha",
            Description = "Iced matcha sweetened with house-made blueberry syrup and lavender syrup. Topped with vanilla sweet cream (contains dairy).",
            Price = 6.75m,
            Category = "Drinks",
            Subcategory = "Matcha",
            Featured = true
        },


        // =========================
        // CHAI LATTES
        // =========================

        new MenuItem
        {
            Id = 2201,
            Name = "24oz Iced Golden Chai",
            Description = "Slightly less spicy than our regular chai, with turmeric & maca powder. Sweetened with house-made syrup.",
            Price = 4.95m,
            Category = "Drinks",
            Subcategory = "Chai Lattes"
        },

        new MenuItem
        {
            Id = 2202,
            Name = "Chai Latte (Hot)",
            Description = "Made with a custom spice blend, steamed milk & house-made syrup. Our chai is a bit spicier than your usual stuff!",
            Price = 4.50m,
            Category = "Drinks",
            Subcategory = "Chai Lattes"
        },

        new MenuItem
        {
            Id = 2203,
            Name = "Dirty Chai Hot",
            Description = "Chai + espresso.",
            Price = 5.00m,
            Category = "Drinks",
            Subcategory = "Chai Lattes"
        },

        new MenuItem
        {
            Id = 2204,
            Name = "Dirty Golden Chai",
            Description = "Chai + espresso.",
            Price = 5.00m,
            Category = "Drinks",
            Subcategory = "Chai Lattes"
        },

        new MenuItem
        {
            Id = 2205,
            Name = "Golden Chai Latte",
            Description = "Slightly less spicy than our regular chai, with turmeric & maca powder. Sweetened with house-made syrup.",
            Price = 4.50m,
            Category = "Drinks",
            Subcategory = "Chai Lattes"
        },

        new MenuItem
        {
            Id = 2206,
            Name = "Iced Chai",
            Description = "Made with a custom spice blend, milk & house-made syrup. Our chai is a bit spicier than your usual stuff!",
            Price = 4.50m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Chai Lattes"
        },

        new MenuItem
        {
            Id = 2207,
            Name = "Iced Dirty Chai",
            Description = "Chai + espresso.",
            Price = 5.00m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Chai Lattes"
        },

        new MenuItem
        {
            Id = 2208,
            Name = "Iced Golden Chai",
            Description = "Slightly less spicy than our regular chai, with turmeric & maca powder. Sweetened with house-made syrup.",
            Price = 4.50m,
            PriceVaries = true,
            Category = "Drinks",
            Subcategory = "Chai Lattes"
        },

        new MenuItem
        {
            Id = 2209,
            Name = "Pot of Gold Chai Latte",
            Description = "A Golden Chai Latte with our Signature Tuscan Sunrise Syrup.",
            Price = 4.85m,
            Category = "Drinks",
            Subcategory = "Chai Lattes"
        },

        new MenuItem
        {
            Id = 2210,
            Name = "Rooibos Chai Latte",
            Description = "Non-caffeinated chai made with rooibos. Sweetened with house-made syrup.",
            Price = 4.50m,
            Category = "Drinks",
            Subcategory = "Chai Lattes"
        }
    ];
}