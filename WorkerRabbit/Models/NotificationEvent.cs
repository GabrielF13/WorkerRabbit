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

        public bool Sent { get; set; }

        public string ErrorMessage { get; set; }
    }
}
