using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgriIDMS.Domain.Enums;

namespace AgriIDMS.Application.Serialization
{
    /// <summary>API JSON: chấp nhận "COD" (legacy) như <see cref="PaymentMethod.Cash"/> vì cùng giá trị int 0 trong DB.</summary>
    public sealed class PaymentMethodCompatJsonConverter : JsonConverter<PaymentMethod>
    {
        public override PaymentMethod Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (string.IsNullOrWhiteSpace(s))
                    throw new JsonException("PaymentMethod không được để trống.");

                if (string.Equals(s, "COD", StringComparison.OrdinalIgnoreCase))
                    return PaymentMethod.Cash;

                if (Enum.TryParse<PaymentMethod>(s, ignoreCase: true, out var parsed))
                    return parsed;

                throw new JsonException($"PaymentMethod không hợp lệ: {s}");
            }

            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var n))
                return (PaymentMethod)n;

            throw new JsonException("PaymentMethod phải là chuỗi hoặc số.");
        }

        public override void Write(Utf8JsonWriter writer, PaymentMethod value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }
}
