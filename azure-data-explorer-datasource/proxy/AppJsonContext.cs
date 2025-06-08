using System.Text.Json.Serialization;

[JsonSerializable(typeof(RequestPayload))]
public partial class AppJsonContext : JsonSerializerContext { }