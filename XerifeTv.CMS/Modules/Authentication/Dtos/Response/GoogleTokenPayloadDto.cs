using System.Text.Json.Serialization;

namespace XerifeTv.CMS.Modules.Authentication.Dtos.Response;

public record GoogleTokenPayloadDto(
	[property: JsonPropertyName("aud")] string Aud,
	[property: JsonPropertyName("exp")] string Exp,
	[property: JsonPropertyName("email")] string Email);