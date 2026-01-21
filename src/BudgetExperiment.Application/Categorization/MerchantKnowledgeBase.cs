// <copyright file="MerchantKnowledgeBase.cs" company="BecauseImClever">
// Copyright (c) BecauseImClever. All rights reserved.
// </copyright>

using BudgetExperiment.Domain;

namespace BudgetExperiment.Application.Categorization;

/// <summary>
/// Default merchant-to-category mappings for common vendors.
/// Used as fallback when no learned mapping exists.
/// </summary>
public static class MerchantKnowledgeBase
{
    /// <summary>
    /// Default merchant pattern to category/icon mappings.
    /// Keys are lowercase for case-insensitive matching.
    /// </summary>
    private static readonly Dictionary<string, (string Category, string Icon)> DefaultMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Entertainment / Streaming
        { "netflix", ("Entertainment", "movie") },
        { "spotify", ("Entertainment", "music") },
        { "hulu", ("Entertainment", "movie") },
        { "disney+", ("Entertainment", "movie") },
        { "disneyplus", ("Entertainment", "movie") },
        { "hbo max", ("Entertainment", "movie") },
        { "hbo", ("Entertainment", "movie") },
        { "youtube", ("Entertainment", "movie") },
        { "amazon prime video", ("Entertainment", "movie") },
        { "apple tv", ("Entertainment", "movie") },
        { "twitch", ("Entertainment", "gaming") },
        { "playstation", ("Entertainment", "gaming") },
        { "xbox", ("Entertainment", "gaming") },
        { "steam", ("Entertainment", "gaming") },
        { "nintendo", ("Entertainment", "gaming") },
        { "peacock", ("Entertainment", "movie") },
        { "paramount+", ("Entertainment", "movie") },
        { "apple music", ("Entertainment", "music") },
        { "pandora", ("Entertainment", "music") },
        { "audible", ("Entertainment", "book") },

        // Dining / Restaurants
        { "mcdonald", ("Dining", "restaurant") },
        { "burger king", ("Dining", "restaurant") },
        { "wendy", ("Dining", "restaurant") },
        { "taco bell", ("Dining", "restaurant") },
        { "chipotle", ("Dining", "restaurant") },
        { "starbucks", ("Dining", "coffee") },
        { "dunkin", ("Dining", "coffee") },
        { "chick-fil-a", ("Dining", "restaurant") },
        { "subway", ("Dining", "restaurant") },
        { "domino", ("Dining", "restaurant") },
        { "pizza hut", ("Dining", "restaurant") },
        { "papa john", ("Dining", "restaurant") },
        { "grubhub", ("Dining", "restaurant") },
        { "doordash", ("Dining", "restaurant") },
        { "uber eats", ("Dining", "restaurant") },
        { "postmates", ("Dining", "restaurant") },
        { "panera", ("Dining", "restaurant") },
        { "panda express", ("Dining", "restaurant") },
        { "olive garden", ("Dining", "restaurant") },
        { "applebee", ("Dining", "restaurant") },
        { "buffalo wild wings", ("Dining", "restaurant") },
        { "ihop", ("Dining", "restaurant") },
        { "denny", ("Dining", "restaurant") },
        { "waffle house", ("Dining", "restaurant") },

        // Shopping
        { "amazon", ("Shopping", "shopping-cart") },
        { "amzn", ("Shopping", "shopping-cart") },
        { "walmart", ("Shopping", "shopping-cart") },
        { "target", ("Shopping", "shopping-cart") },
        { "costco", ("Shopping", "shopping-cart") },
        { "sam's club", ("Shopping", "shopping-cart") },
        { "ebay", ("Shopping", "shopping-cart") },
        { "etsy", ("Shopping", "shopping-cart") },
        { "best buy", ("Shopping", "shopping-cart") },
        { "home depot", ("Shopping", "tools") },
        { "lowes", ("Shopping", "tools") },
        { "lowe's", ("Shopping", "tools") },
        { "ikea", ("Shopping", "home") },
        { "wayfair", ("Shopping", "home") },
        { "bed bath", ("Shopping", "home") },
        { "macys", ("Shopping", "shopping-cart") },
        { "macy's", ("Shopping", "shopping-cart") },
        { "nordstrom", ("Shopping", "shopping-cart") },
        { "kohls", ("Shopping", "shopping-cart") },
        { "kohl's", ("Shopping", "shopping-cart") },
        { "jcpenney", ("Shopping", "shopping-cart") },
        { "old navy", ("Shopping", "shopping-cart") },
        { "gap", ("Shopping", "shopping-cart") },
        { "nike", ("Shopping", "shopping-cart") },
        { "adidas", ("Shopping", "shopping-cart") },
        { "dollar tree", ("Shopping", "shopping-cart") },
        { "dollar general", ("Shopping", "shopping-cart") },
        { "five below", ("Shopping", "shopping-cart") },

        // Groceries
        { "kroger", ("Groceries", "grocery") },
        { "safeway", ("Groceries", "grocery") },
        { "publix", ("Groceries", "grocery") },
        { "whole foods", ("Groceries", "grocery") },
        { "trader joe", ("Groceries", "grocery") },
        { "aldi", ("Groceries", "grocery") },
        { "wegmans", ("Groceries", "grocery") },
        { "heb", ("Groceries", "grocery") },
        { "h-e-b", ("Groceries", "grocery") },
        { "food lion", ("Groceries", "grocery") },
        { "giant", ("Groceries", "grocery") },
        { "stop & shop", ("Groceries", "grocery") },
        { "harris teeter", ("Groceries", "grocery") },
        { "meijer", ("Groceries", "grocery") },
        { "sprouts", ("Groceries", "grocery") },
        { "piggly wiggly", ("Groceries", "grocery") },
        { "winn-dixie", ("Groceries", "grocery") },
        { "jewel-osco", ("Groceries", "grocery") },
        { "albertsons", ("Groceries", "grocery") },

        // Gas / Fuel
        { "shell", ("Gas", "fuel") },
        { "exxon", ("Gas", "fuel") },
        { "chevron", ("Gas", "fuel") },
        { "bp gas", ("Gas", "fuel") },
        { "mobil", ("Gas", "fuel") },
        { "marathon", ("Gas", "fuel") },
        { "speedway", ("Gas", "fuel") },
        { "circle k", ("Gas", "fuel") },
        { "7-eleven", ("Gas", "fuel") },
        { "wawa", ("Gas", "fuel") },
        { "sheetz", ("Gas", "fuel") },
        { "quiktrip", ("Gas", "fuel") },
        { "racetrac", ("Gas", "fuel") },
        { "murphy", ("Gas", "fuel") },
        { "valero", ("Gas", "fuel") },
        { "sunoco", ("Gas", "fuel") },
        { "pilot", ("Gas", "fuel") },
        { "loves travel", ("Gas", "fuel") },
        { "casey", ("Gas", "fuel") },

        // Transportation
        { "uber", ("Transportation", "car") },
        { "lyft", ("Transportation", "car") },
        { "parking", ("Transportation", "car") },
        { "toll", ("Transportation", "car") },
        { "metro", ("Transportation", "train") },
        { "transit", ("Transportation", "train") },

        // Utilities
        { "electric", ("Utilities", "lightning") },
        { "water bill", ("Utilities", "water") },
        { "gas bill", ("Utilities", "flame") },
        { "internet", ("Utilities", "wifi") },
        { "comcast", ("Utilities", "wifi") },
        { "xfinity", ("Utilities", "wifi") },
        { "spectrum", ("Utilities", "wifi") },
        { "cox", ("Utilities", "wifi") },
        { "att", ("Utilities", "phone") },
        { "at&t", ("Utilities", "phone") },
        { "verizon", ("Utilities", "phone") },
        { "t-mobile", ("Utilities", "phone") },
        { "tmobile", ("Utilities", "phone") },
        { "sprint", ("Utilities", "phone") },

        // Subscriptions
        { "apple.com/bill", ("Subscriptions", "subscription") },
        { "google storage", ("Subscriptions", "cloud") },
        { "icloud", ("Subscriptions", "cloud") },
        { "dropbox", ("Subscriptions", "cloud") },
        { "microsoft 365", ("Subscriptions", "subscription") },
        { "office 365", ("Subscriptions", "subscription") },
        { "adobe", ("Subscriptions", "subscription") },
        { "github", ("Subscriptions", "subscription") },
        { "notion", ("Subscriptions", "subscription") },
        { "evernote", ("Subscriptions", "subscription") },
        { "1password", ("Subscriptions", "subscription") },
        { "lastpass", ("Subscriptions", "subscription") },
        { "nordvpn", ("Subscriptions", "subscription") },
        { "expressvpn", ("Subscriptions", "subscription") },

        // Health & Fitness
        { "gym", ("Health & Fitness", "fitness") },
        { "planet fitness", ("Health & Fitness", "fitness") },
        { "anytime fitness", ("Health & Fitness", "fitness") },
        { "la fitness", ("Health & Fitness", "fitness") },
        { "equinox", ("Health & Fitness", "fitness") },
        { "orangetheory", ("Health & Fitness", "fitness") },
        { "peloton", ("Health & Fitness", "fitness") },
        { "crossfit", ("Health & Fitness", "fitness") },
        { "ymca", ("Health & Fitness", "fitness") },

        // Healthcare
        { "pharmacy", ("Healthcare", "medical") },
        { "cvs", ("Healthcare", "medical") },
        { "walgreens", ("Healthcare", "medical") },
        { "rite aid", ("Healthcare", "medical") },
        { "doctor", ("Healthcare", "medical") },
        { "hospital", ("Healthcare", "medical") },
        { "clinic", ("Healthcare", "medical") },
        { "urgent care", ("Healthcare", "medical") },
        { "dental", ("Healthcare", "dental") },
        { "dentist", ("Healthcare", "dental") },
        { "optometrist", ("Healthcare", "medical") },
        { "vision", ("Healthcare", "medical") },
        { "lenscrafters", ("Healthcare", "medical") },
        { "kaiser", ("Healthcare", "medical") },
        { "united health", ("Healthcare", "medical") },
        { "cigna", ("Healthcare", "medical") },
        { "aetna", ("Healthcare", "medical") },
        { "blue cross", ("Healthcare", "medical") },

        // Travel
        { "airbnb", ("Travel", "bed") },
        { "vrbo", ("Travel", "bed") },
        { "hotel", ("Travel", "bed") },
        { "marriott", ("Travel", "bed") },
        { "hilton", ("Travel", "bed") },
        { "hyatt", ("Travel", "bed") },
        { "ihg", ("Travel", "bed") },
        { "holiday inn", ("Travel", "bed") },
        { "best western", ("Travel", "bed") },
        { "motel", ("Travel", "bed") },
        { "airline", ("Travel", "plane") },
        { "united air", ("Travel", "plane") },
        { "delta air", ("Travel", "plane") },
        { "southwest", ("Travel", "plane") },
        { "american air", ("Travel", "plane") },
        { "jetblue", ("Travel", "plane") },
        { "frontier", ("Travel", "plane") },
        { "spirit air", ("Travel", "plane") },
        { "alaska air", ("Travel", "plane") },
        { "expedia", ("Travel", "plane") },
        { "booking.com", ("Travel", "bed") },
        { "travelocity", ("Travel", "plane") },
        { "kayak", ("Travel", "plane") },
        { "priceline", ("Travel", "plane") },

        // Insurance
        { "geico", ("Insurance", "shield") },
        { "state farm", ("Insurance", "shield") },
        { "progressive", ("Insurance", "shield") },
        { "allstate", ("Insurance", "shield") },
        { "liberty mutual", ("Insurance", "shield") },
        { "farmers insurance", ("Insurance", "shield") },
        { "usaa", ("Insurance", "shield") },

        // Pets
        { "petco", ("Pets", "paw") },
        { "petsmart", ("Pets", "paw") },
        { "chewy", ("Pets", "paw") },
        { "vet", ("Pets", "paw") },
        { "veterinary", ("Pets", "paw") },

        // Education
        { "udemy", ("Education", "book") },
        { "coursera", ("Education", "book") },
        { "skillshare", ("Education", "book") },
        { "masterclass", ("Education", "book") },
        { "linkedin learning", ("Education", "book") },
        { "pluralsight", ("Education", "book") },
        { "codecademy", ("Education", "book") },

        // Charity
        { "donation", ("Charity", "heart") },
        { "charity", ("Charity", "heart") },
        { "gofundme", ("Charity", "heart") },
        { "red cross", ("Charity", "heart") },
    };

    /// <summary>
    /// Category name to CategoryType mapping.
    /// </summary>
    private static readonly Dictionary<string, CategoryType> CategoryTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Income categories
        { "Paycheck", CategoryType.Income },
        { "Salary", CategoryType.Income },
        { "Income", CategoryType.Income },
        { "Interest", CategoryType.Income },
        { "Dividends", CategoryType.Income },
        { "Refund", CategoryType.Income },

        // Transfer categories
        { "Transfer", CategoryType.Transfer },
        { "Transfers", CategoryType.Transfer },

        // All others default to Expense
    };

    /// <summary>
    /// Tries to get a category mapping for a transaction description.
    /// </summary>
    /// <param name="description">The transaction description.</param>
    /// <param name="mapping">The matched mapping if found.</param>
    /// <returns>True if a mapping was found, false otherwise.</returns>
    public static bool TryGetMapping(string description, out (string Category, string Icon)? mapping)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            mapping = null;
            return false;
        }

        var normalizedDescription = description.ToUpperInvariant();

        foreach (var kvp in DefaultMappings)
        {
            if (normalizedDescription.Contains(kvp.Key.ToUpperInvariant(), StringComparison.Ordinal))
            {
                mapping = kvp.Value;
                return true;
            }
        }

        mapping = null;
        return false;
    }

    /// <summary>
    /// Gets the matched pattern for a transaction description.
    /// </summary>
    /// <param name="description">The transaction description.</param>
    /// <returns>The matched pattern or null.</returns>
    public static string? GetMatchedPattern(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalizedDescription = description.ToUpperInvariant();

        foreach (var kvp in DefaultMappings)
        {
            if (normalizedDescription.Contains(kvp.Key.ToUpperInvariant(), StringComparison.Ordinal))
            {
                return kvp.Key;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all default mappings.
    /// </summary>
    /// <returns>Read-only dictionary of all mappings.</returns>
    public static IReadOnlyDictionary<string, (string Category, string Icon)> GetAllMappings()
    {
        return DefaultMappings;
    }

    /// <summary>
    /// Gets the category type for a category name.
    /// </summary>
    /// <param name="categoryName">The category name.</param>
    /// <returns>The category type (defaults to Expense).</returns>
    public static CategoryType GetCategoryType(string categoryName)
    {
        return CategoryTypes.TryGetValue(categoryName, out var type) ? type : CategoryType.Expense;
    }
}
