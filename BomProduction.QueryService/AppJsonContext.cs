using System.Text.Json.Serialization;
using BomProduction.Shared.Models;

namespace BomProduction.QueryService;

// Le avisamos qué clases/records va a recibir o devolver el API
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<Product>))]
public partial class AppJsonContext : JsonSerializerContext
{
}