namespace SettlementManager.Models
{
    public class Resource
    {
        public int Id { get; set; }
        public ResourceType Type { get; set; } // Тип ресурса
        public int Amount { get; set; }
        public string Name { get; set; } // Название ресурс
        public bool IsTool { get; set; } = false; // Для определения, является ли это инструментом
        public int SettlementId { get; set; }
    }

    public enum ResourceType
    {
        Ordinary, // Обычный
        Special,  // Специальный
        Rare,     // Редкий
        Unique,   // Уникальный
        Mystical, // Мистический
        Tool      // Инструмент
    }

    public static class ResourceTypeExtensions
    {
        public static string ToRussian(this ResourceType type)
        {
            return type switch
            {
                ResourceType.Ordinary => "Обычный",
                ResourceType.Special => "Специальный",
                ResourceType.Rare => "Редкий",
                ResourceType.Unique => "Уникальный",
                ResourceType.Mystical => "Мистический",
                ResourceType.Tool => "Инструмент",
                _ => "Неизвестный"
            };
        }
        public static string ToRussian2(this ResourceType type)
        {
            return type switch
            {
                ResourceType.Ordinary => "Обычный",
                ResourceType.Special => "Специальный",
                ResourceType.Rare => "Редкий",
                ResourceType.Unique => "Уникальный",
                ResourceType.Mystical => "Мистический",
                ResourceType.Tool => "Инструмент",
                _ => "Неизвестный"
            };

        }

    }


}