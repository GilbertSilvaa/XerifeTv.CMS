using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace XerifeTv.CMS.Modules.ContentCalendar;

/// <summary>
/// MongoDB.Driver 2.28 does not ship a DateOnly serializer. Dates are stored as
/// BSON DateTime at UTC midnight, which keeps range queries and sorting correct.
/// </summary>
public sealed class DateOnlyBsonSerializer : SerializerBase<DateOnly>
{
    public override DateOnly Deserialize(
        BsonDeserializationContext context,
        BsonDeserializationArgs args)
    {
        var milliseconds = context.Reader.ReadDateTime();
        return DateOnly.FromDateTime(
            DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime);
    }

    public override void Serialize(
        BsonSerializationContext context,
        BsonSerializationArgs args,
        DateOnly value)
    {
        var utc = new DateTimeOffset(value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        context.Writer.WriteDateTime(utc.ToUnixTimeMilliseconds());
    }
}
