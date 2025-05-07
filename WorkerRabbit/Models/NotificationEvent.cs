using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace WorkerRabbit.Models
{
    public class NotificationEvent
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }

        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public NotificationType Type { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }

        [BsonElement("data")]
        public Dictionary<string, string> Data { get; set; }

        // Propriedade para rastreamento de tentativas de envio (útil para retentativas)
        public int RetryCount { get; set; }

        // Propriedade para registrar status de envio
        public bool Sent { get; set; }

        // Propriedade para registrar erro, se houver
        public string ErrorMessage { get; set; }
    }
}
