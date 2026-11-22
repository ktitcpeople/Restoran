using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ochag.Validation;

// Keep the client and API validation rules identical. Each application is self-contained.
public static class InputRules
{
    public static string Digits(string? value) => new((value ?? "").Where(c => c is >= '0' and <= '9').Take(11).ToArray());
    public static bool ValidPhone(string? value) => Regex.IsMatch(value ?? "", @"^[78][0-9]{10}$");
    public static string? DeliveryError(string? address, string? phone)
        => string.IsNullOrWhiteSpace(address) || address.Trim().Length < 8 || address.Length > 300
            ? "Укажите адрес доставки: от 8 до 300 символов, включая улицу и дом."
            : !Regex.IsMatch(address, @"\p{L}") || !Regex.IsMatch(address, "[0-9]")
            ? "Укажите улицу и номер дома в адресе доставки."
            : !ValidPhone(phone) ? "Телефон: 11 цифр, начиная с 7 или 8. Например, 79991234567." : null;

    public static Dictionary<string, string> Validate(string path, JsonElement body)
    {
        var errors = new Dictionary<string, string>();
        path = path.ToLowerInvariant().TrimEnd('/');
        JsonElement Get(string key) => body.ValueKind == JsonValueKind.Object
            ? body.EnumerateObject().FirstOrDefault(p => p.Name.Equals(key, StringComparison.OrdinalIgnoreCase)).Value : default;
        string Text(string key) => Get(key).ValueKind == JsonValueKind.String ? Get(key).GetString() ?? "" : "";
        decimal? Number(string key) => Get(key).ValueKind == JsonValueKind.Number && Get(key).TryGetDecimal(out var n) ? n : null;
        void Fail(string key, string message) => errors.TryAdd(key, message);
        void Required(string key, string label, int min = 1, int max = 120)
        {
            var text = Text(key);
            if (text.Trim().Length < min || text.Length > max || text.Any(c => char.IsControl(c) && c != '\n' && c != '\r' && c != '\t'))
                Fail(key, $"{label}: от {min} до {max} символов.");
        }
        void Range(string key, string label, decimal min, decimal max, bool integer = false)
        {
            var n = Number(key);
            if (n == null || n < min || n > max || integer && n != decimal.Truncate(n.Value))
                Fail(key, $"{label}: {(integer ? "целое число " : "число ")}от {min} до {max}.");
        }
        void Id(string key, string label) => Range(key, label, 1, int.MaxValue, true);
        void Phone(string key)
        {
            if (!ValidPhone(Text(key))) Fail(key, "Телефон: 11 цифр, начиная с 7 или 8. Например, 79991234567.");
        }
        void Choice(string key, params string[] values)
        {
            if (!values.Contains(Text(key))) Fail(key, "Выберите допустимое значение из списка.");
        }
        void Lines(string key, int max, Action<JsonElement, int> validate)
        {
            var list = Get(key);
            if (list.ValueKind != JsonValueKind.Array || list.GetArrayLength() < 1 || list.GetArrayLength() > max)
            { Fail(key, $"Список должен содержать от 1 до {max} записей."); return; }
            var index = 0;
            foreach (var row in list.EnumerateArray()) validate(row, index++);
        }
        void Child(JsonElement row, string childPath, int index)
        {
            foreach (var e in Validate(childPath, row)) Fail($"Items[{index}].{e.Key}", e.Value);
        }

        if (path is "/auth/login" or "/auth/register" or "/auth/users")
        {
            Required("Login", "Логин", 1, 64);
            Required("Password", "Пароль", path == "/auth/login" ? 1 : 6, 128);
            if (path != "/auth/login")
            {
                if (!Regex.IsMatch(Text("Login"), @"^[\p{L}\p{N}_.@-]{3,64}$")) Fail("Login", "Логин: 3–64 символа; буквы, цифры, точка, @, дефис или подчёркивание.");
                Required("UserName", "Имя", 2, 80);
                if (!Regex.IsMatch(Text("UserName").Trim(), @"^[\p{L}][\p{L}\p{M} .'-]*$")) Fail("UserName", "Имя должно содержать буквы; допустимы пробел, дефис, точка и апостроф.");
                if (path == "/auth/users") Range("Role", "Роль", 1, 7, true);
            }
        }
        else if (path.StartsWith("/auth/users/") && path.EndsWith("/role"))
        {
            if (body.ValueKind != JsonValueKind.Number || !body.TryGetInt32(out var role) || role is < 1 or > 7) Fail("Role", "Выберите существующую роль.");
        }
        else if (path is "/menu/create" || path.StartsWith("/menu/edit/"))
        {
            Required("Name", "Название блюда", 2);
            Range("Price", "Цена", .01m, 1000000);
            Id("MenuCategoryID", "Категория");
            Required("KitchenStation", "Кухонная станция", 2, 80);
            if (Text("Description").Length > 2000) Fail("Description", "Описание: не более 2000 символов.");
            var url = Text("ImageUrl");
            if (url.Length > 2048 || url.Length > 0 && (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
                Fail("ImageUrl", "Укажите ссылку на изображение, начинающуюся с http:// или https:// (до 2048 символов).");
            foreach (var nutrient in new[] { "Protein", "Fat", "Carbohydrates" }) Range(nutrient, "БЖУ, граммы", 0, 10000);
        }
        else if (path == "/menu/categories") Required("Name", "Название категории", 2, 80);
        else if (path == "/tables/create")
        {
            Range("Number", "Номер стола", 1, 9999, true);
            Range("Capacity", "Вместимость", 1, 30, true);
            Range("PositionX", "Колонка", 1, 6, true);
            Range("PositionY", "Ряд", 1, 8, true);
        }
        else if (path == "/tables/reserve")
        {
            Id("TableID", "Стол"); Range("Persons", "Количество гостей", 1, 30, true);
            Required("GuestName", "Имя гостя", 2, 80); Phone("GuestPhone");
            if (!Regex.IsMatch(Text("GuestName").Trim(), @"^[\p{L}][\p{L}\p{M} .'-]*$")) Fail("GuestName", "Укажите имя буквами.");
            if (!System.DateTime.TryParse(Text("DateTime"), null, System.Globalization.DateTimeStyles.RoundtripKind, out var date)
                || date < (date.Kind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now) || date > DateTime.Now.AddYears(1))
                Fail("DateTime", "Выберите будущее время бронирования в пределах года.");
        }
        else if (path == "/orders/create")
        {
            Choice("Type", "DineIn", "Delivery", "Takeaway");
            if (Text("Type") == "DineIn") Id("TableID", "Стол");
            Range("Priority", "Приоритет", 0, 1, true);
            if (Text("PickupAddress").Length > 300) Fail("PickupAddress", "Адрес филиала: не более 300 символов.");
            Lines("Items", 100, (row, i) => Child(row, "/validation/order-item", i));
        }
        else if (path == "/validation/order-item") { Id("MenuItemID", "Блюдо"); Range("Quantity", "Количество порций", 1, 99, true); }
        else if (path == "/orders/status") { Id("OrderID", "Заказ"); Choice("Status", "New", "Preparing", "Ready", "Paid", "Cancelled"); }
        else if (path == "/delivery/create")
        {
            Id("OrderID", "Заказ"); Required("Address", "Адрес с улицей и домом", 8, 300); Phone("ClientPhone");
            if (!Regex.IsMatch(Text("Address"), @"\p{L}") || !Regex.IsMatch(Text("Address"), "[0-9]")) Fail("Address", "Укажите улицу и номер дома в адресе доставки.");
        }
        else if (path == "/delivery/assign") { Id("DeliveryID", "Доставка"); Id("CourierID", "Курьер"); }
        else if (path == "/delivery/status")
        {
            Id("DeliveryID", "Доставка"); Choice("Status", "Pending", "Assigned", "Accepted", "InTransit", "Delivered", "Cancelled");
            if (Text("Status") == "InTransit" || Get("EstimatedMinutes").ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)) Range("EstimatedMinutes", "Время прибытия, минуты", 1, 180, true);
        }
        else if (path == "/delivery/route-plan")
        {
            var ids = new HashSet<int>();
            Lines("DeliveryIDs", 200, (row, _) => { if (row.ValueKind != JsonValueKind.Number || !row.TryGetInt32(out var id) || id <= 0 || !ids.Add(id)) Fail("DeliveryIDs", "Доставки должны быть выбраны без повторений."); });
        }
        else if (path == "/inventory/ingredients/create")
        {
            Required("Name", "Название ингредиента", 2);
            Choice("Unit", "кг", "г", "л", "мл", "шт", "уп", "бутылка");
            Range("Stock", "Остаток", 0, 1000000); Range("PricePerUnit", "Цена за единицу", 0, 1000000);
        }
        else if (path == "/inventory/ingredients/stock") { Id("IngredientID", "Ингредиент"); Range("Amount", "Количество", .001m, 1000000); }
        else if (path == "/inventory/techcards") { Id("MenuItemID", "Блюдо"); Id("IngredientID", "Ингредиент"); Range("Quantity", "Количество", .001m, 1000000); }
        else if (path == "/inventory/supplies") { Id("IngredientID", "Ингредиент"); Id("SupplierUserID", "Поставщик"); Range("Quantity", "Количество", .001m, 1000000); }
        else if (path == "/inventory/stocktakes")
        {
            if (Text("Notes").Length > 1000) Fail("Notes", "Комментарий: не более 1000 символов.");
            var ids = new HashSet<int>();
            Lines("Items", 2000, (row, i) =>
            {
                Child(row, "/validation/stocktake-item", i);
                if (row.ValueKind == JsonValueKind.Object)
                {
                    var id = row.EnumerateObject().FirstOrDefault(p => p.Name.Equals("IngredientID", StringComparison.OrdinalIgnoreCase)).Value;
                    if (id.ValueKind == JsonValueKind.Number && id.TryGetInt32(out var n) && !ids.Add(n)) Fail("Items", "Ингредиент не должен повторяться.");
                }
            });
        }
        else if (path == "/validation/stocktake-item") { Id("IngredientID", "Ингредиент"); Range("CountedStock", "Фактический остаток", 0, 1000000); }
        else if (path == "/support/chat" || path.StartsWith("/support/conversations/") && path.EndsWith("/reply"))
        {
            if (!(Get("Resolve").ValueKind == JsonValueKind.True && string.IsNullOrWhiteSpace(Text("Message")))) Required("Message", "Сообщение", 1, 4000);
        }
        else if (path == "/guest/call-waiter") { Id("TableID", "Стол"); if (Text("Reason").Length > 500) Fail("Reason", "Причина вызова: до 500 символов."); }
        else if (path == "/guest/request-bill") Id("OrderID", "Заказ");
        else if (path is "/payments/request" or "/payments/online/create") { Id("OrderID", "Заказ"); Choice("Method", "Cash", "CardTerminal"); }
        return errors;
    }
}
