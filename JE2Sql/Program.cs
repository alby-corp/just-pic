namespace JE2Sql
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using static System.Console;

    static class Program
    {
        static string TryGet(this IReadOnlyList<string> source, int index)
            => index < source.Count && !string.IsNullOrWhiteSpace(source[index]) ? source[index] : null;

        static string Capitalize(this string value)
        {
            var characters = value.ToCharArray();
            if (characters.Length > 0)
            {
                characters[0] = char.ToUpper(characters[0]);
            }
            
            return new string(characters);
        }

        static async Task<int> Main(string[] args)
        {
            var selection = args.TryGet(index: 0);

            if (selection == null)
            {
                Error.WriteLine("Missing parameter menuId or restaurant name.");
                return 1;
            }

            var excludedVariations = (args.TryGet(index: 1) ?? string.Empty)
                .Split(separator: ',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            using var api = new Api(new Uri("http://www.justeat.it"), ".\\cookies.bin");

            var menuId = int.TryParse(selection, out var id)
                ? id
                : await api.TryGetMenuId(selection);

            if (menuId == null)
            {
                Error.WriteLine($"Could not find menu id for {selection}.");
                return 2;
            }

            var menu = await api.GetMenu(menuId.Value);

            var defaultType = new Sql.Type
            {
                Id = Guid.NewGuid(),
                Description = "Unknown"
            };

            var types = menu.Categories.Select(category => new Sql.Type
                {
                    Id = Guid.NewGuid(),
                    Description = category.Name,
                    JEProductIds = category.Items.SelectMany(item => item.Products.Select(id => id.Value)).ToHashSet()
                })
                .ToArray();

            var ingredients = menu.Accessories.Select(accessory => new Sql.Ingredient
                {
                    Id = Guid.NewGuid(),
                    Name = accessory.Name,
                    Price = accessory.Price
                })
                .ToList();

            var foods = menu.Products
                .Where(product => !excludedVariations.Contains(product.Variation?.Trim()))
                .GroupBy(product => product.Name.Trim())
                .SelectMany(g => g.Select(product =>
                {
                    var variation = g.Count() > 1 && !string.IsNullOrEmpty(product.Variation)
                        ? $" ({product.Variation})"
                        : string.Empty;

                    return new Sql.Food
                    {
                        Id = Guid.NewGuid(),
                        Name = $"{g.Key}{variation}",
                        Visible = true,
                        Price = product.Price,
                        TypeId = (types.FirstOrDefault(type => type.JEProductIds.Contains(product.Id)) ?? defaultType).Id,
                        JEIngredientNames = (product.Description ?? "").Split(new[] { ",", " e " }, StringSplitOptions.RemoveEmptyEntries).Select(name => name.Trim()).ToArray()
                    };
                }))
                .ToArray();
            
            var foodIngredients = new List<Sql.FoodIngredient>();
            foreach (var food in foods)
            {
                foreach (var name in food.JEIngredientNames)
                {
                    var match = ingredients.FirstOrDefault(ingredient => string.Equals(ingredient.Name, name, StringComparison.InvariantCultureIgnoreCase));
                    if (match == null)
                    {
                        match = new Sql.Ingredient
                        {
                            Id = Guid.NewGuid(),
                            Name = name.Capitalize(),
                            Price = -1
                        };

                        ingredients.Add(match);
                    }
                    
                    foodIngredients.Add(new Sql.FoodIngredient
                    {
                        FoodId = food.Id,
                        IngredientId = match.Id
                    });
                }
            }
            

            WriteLine("// type");

            foreach (var type in types)
            {
                WriteLine(type.Render());
            }

            if (foods.Any(food => food.TypeId == defaultType.Id))
            {
                WriteLine(defaultType.Render());
            }

            WriteLine();
            WriteLine("// ingredient");

            foreach (var ingredient in ingredients)
            {
                WriteLine(ingredient.Render());
            }

            WriteLine();
            WriteLine("// food");

            foreach (var food in foods)
            {
                WriteLine(food.Render());
            }

            WriteLine();
            WriteLine("// food_ingredient");

            foreach (var foodIngredient in foodIngredients)
            {
                WriteLine(foodIngredient.Render());
            }

            return 0;
        }
    }
}