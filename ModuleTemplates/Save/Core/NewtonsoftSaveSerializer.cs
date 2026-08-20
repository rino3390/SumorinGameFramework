using System;
using System.Linq;
using Newtonsoft.Json;
using UniRx;

namespace Sumorin.Save
{
	/// <summary>
	///     序列化器的預設實作，走 Newtonsoft.Json
	/// </summary>
	/// <remarks>
	///     值面型別的轉換器已內建，值面欄位落地為純值。
	///     實體靠建構子參數對應屬性名還原，唯讀屬性因此不會遺失。
	/// </remarks>
	public class NewtonsoftSaveSerializer: ISerializer
	{
		private readonly JsonSerializerSettings settings;

		/// <summary>
		///     建立預設序列化器
		/// </summary>
		public NewtonsoftSaveSerializer()
		{
			settings = new()
			{
				Converters = { new ValueFacetConverter() }
			};
		}

	#region ISerializer Members
		/// <inheritdoc />
		public string Serialize<T>(T target) => JsonConvert.SerializeObject(target, settings);

		/// <inheritdoc />
		public T Deserialize<T>(string data)
		{
			if(string.IsNullOrEmpty(data)) return default;

			try
			{
				return JsonConvert.DeserializeObject<T>(data, settings);
			}
			catch(JsonException)
			{
				return default;
			}
		}
	#endregion
	}

	/// <summary>
	///     值面型別的轉換器，讓 <see cref="IReadOnlyReactiveProperty{T}" /> 欄位落地為純值
	/// </summary>
	internal class ValueFacetConverter: JsonConverter
	{
		/// <inheritdoc />
		public override bool CanConvert(Type objectType) => TryGetValueType(objectType, out _);

		/// <inheritdoc />
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if(value == null || !TryGetValueType(value.GetType(), out var valueType))
			{
				writer.WriteNull();
				return;
			}

			var facetType = typeof(IReadOnlyReactiveProperty<>).MakeGenericType(valueType);
			serializer.Serialize(writer, facetType.GetProperty("Value")?.GetValue(value), valueType);
		}

		/// <inheritdoc />
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if(!TryGetValueType(objectType, out var valueType)) return null;

			var value = serializer.Deserialize(reader, valueType);
			return Activator.CreateInstance(typeof(ReactiveProperty<>).MakeGenericType(valueType), value);
		}

		private static bool TryGetValueType(Type type, out Type valueType)
		{
			valueType = null;
			if(type == null) return false;

			var facet = IsValueFacet(type) ? type : type.GetInterfaces().FirstOrDefault(IsValueFacet);
			if(facet == null) return false;

			valueType = facet.GetGenericArguments()[0];
			return true;
		}

		private static bool IsValueFacet(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyReactiveProperty<>);
	}
}