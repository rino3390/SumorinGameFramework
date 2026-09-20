using System;
using System.Collections.Generic;
using Sumorin.SumorinUtility;
using UnityEngine;
using UnityEngine.UI;

namespace Sumorin.Gm
{
	/// <summary>
	///     框架內建的參數欄位，涵蓋整數、浮點數、布林、字串、列舉與配置 Id
	/// </summary>
	public static class GmBuiltInFields
	{
		/// <summary>整數欄位建構器</summary>
		public static readonly GmFieldBuilder Int = (parent, label, _) => Create<GmIntField>(parent, label).Bind();

		/// <summary>浮點數欄位建構器</summary>
		public static readonly GmFieldBuilder Float = (parent, label, _) => Create<GmFloatField>(parent, label).Bind();

		/// <summary>布林欄位建構器</summary>
		public static readonly GmFieldBuilder Bool = (parent, label, _) => Create<GmBoolField>(parent, label).Bind();

		/// <summary>字串欄位建構器</summary>
		public static readonly GmFieldBuilder String = (parent, label, _) => Create<GmStringField>(parent, label).Bind();

		/// <summary>列舉欄位建構器，涵蓋所有列舉型別</summary>
		public static readonly GmFieldBuilder Enum = (parent, label, valueType) => Create<GmEnumField>(parent, label).Bind(valueType);

		/// <summary>
		///     建立配置 Id 欄位建構器，候選清單取自 ConfigManager
		/// </summary>
		/// <param name="configs">配置查找入口</param>
		/// <returns>欄位建構器</returns>
		public static GmFieldBuilder CreateConfigId(ConfigManager configs) =>
			(parent, label, valueType) =>
				Create<GmConfigIdField>(parent, label).Bind(valueType, GmConfigIdLookup.CandidatesOf(valueType, configs));

		// 欄位本體掛在「標籤 + 控制項」的那一列上，控制項由各欄位自己生在同一列內
		private static TField Create<TField>(Transform parent, string label) where TField: MonoBehaviour
		{
			var container = GmUi.Row(parent, $"Field ({label})", 4f);
			GmUi.Label(container.transform, label, 90f);
			return container.AddComponent<TField>();
		}
	}

	/// <summary>
	///     整數輸入欄位
	/// </summary>
	public sealed class GmIntField: GmField<int>
	{
		/// <inheritdoc />
		public override int Value => int.TryParse(input.text, out var value) ? value : 0;

		private InputField input;

		/// <summary>
		///     生成控制項
		/// </summary>
		/// <remarks>
		///     由建構器明確呼叫，不靠 <c>Awake</c>。
		///     EditMode 下 Unity 不會呼叫 <c>Awake</c>，靠它生控制項的欄位在測試裡取值就會炸。
		/// </remarks>
		/// <returns>自己，供建構器串接</returns>
		public GmIntField Bind()
		{
			input = GmUi.Input(transform, InputField.ContentType.IntegerNumber);
			return this;
		}
	}

	/// <summary>
	///     浮點數輸入欄位
	/// </summary>
	public sealed class GmFloatField: GmField<float>
	{
		/// <inheritdoc />
		public override float Value => float.TryParse(input.text, out var value) ? value : 0f;

		private InputField input;

		/// <summary>
		///     生成控制項
		/// </summary>
		/// <remarks>
		///     由建構器明確呼叫，不靠 <c>Awake</c>。
		///     EditMode 下 Unity 不會呼叫 <c>Awake</c>，靠它生控制項的欄位在測試裡取值就會炸。
		/// </remarks>
		/// <returns>自己，供建構器串接</returns>
		public GmFloatField Bind()
		{
			input = GmUi.Input(transform, InputField.ContentType.DecimalNumber);
			return this;
		}
	}

	/// <summary>
	///     布林勾選欄位
	/// </summary>
	public sealed class GmBoolField: GmField<bool>
	{
		/// <inheritdoc />
		public override bool Value => toggle.isOn;

		private Toggle toggle;

		/// <summary>
		///     生成控制項
		/// </summary>
		/// <remarks>
		///     由建構器明確呼叫，不靠 <c>Awake</c>。
		///     EditMode 下 Unity 不會呼叫 <c>Awake</c>，靠它生控制項的欄位在測試裡取值就會炸。
		/// </remarks>
		/// <returns>自己，供建構器串接</returns>
		public GmBoolField Bind()
		{
			toggle = GmUi.Toggle(transform);
			return this;
		}
	}

	/// <summary>
	///     字串輸入欄位
	/// </summary>
	public sealed class GmStringField: GmField<string>
	{
		/// <inheritdoc />
		public override string Value => input.text;

		private InputField input;

		/// <summary>
		///     生成控制項
		/// </summary>
		/// <remarks>
		///     由建構器明確呼叫，不靠 <c>Awake</c>。
		///     EditMode 下 Unity 不會呼叫 <c>Awake</c>，靠它生控制項的欄位在測試裡取值就會炸。
		/// </remarks>
		/// <returns>自己，供建構器串接</returns>
		public GmStringField Bind()
		{
			input = GmUi.Input(transform, InputField.ContentType.Standard);
			return this;
		}
	}

	/// <summary>
	///     列舉下拉欄位，實際型別在建置面板時才決定
	/// </summary>
	public sealed class GmEnumField: GmField<object>
	{
		/// <inheritdoc />
		public override object Value => values.Length == 0 ? null : values.GetValue(dropdown.value);

		private Dropdown dropdown;
		private Array values;

		/// <summary>
		///     綁定列舉型別並生成下拉
		/// </summary>
		/// <param name="enumType">列舉型別</param>
		/// <returns>自己，供建構器串接</returns>
		public GmEnumField Bind(Type enumType)
		{
			values = System.Enum.GetValues(enumType);
			dropdown = GmUi.Dropdown(transform, System.Enum.GetNames(enumType));
			return this;
		}
	}

	/// <summary>
	///     配置 Id 下拉欄位，候選為該配置型別的全部 Id
	/// </summary>
	public sealed class GmConfigIdField: GmField<object>
	{
		/// <inheritdoc />
		public override object Value => GmConfigIdLookup.Wrap(configIdType, SelectedId);

		private string SelectedId => ids.Count == 0 ? null : ids[dropdown.value];

		private Dropdown dropdown;
		private Type configIdType;
		private IReadOnlyList<string> ids;

		/// <summary>
		///     綁定配置 Id 型別並生成下拉
		/// </summary>
		/// <param name="valueType">參數型別，即 <see cref="GmConfigId{TConfig}" /> 的封閉泛型</param>
		/// <param name="candidateIds">候選 Id，可以是空的</param>
		/// <returns>自己，供建構器串接</returns>
		public GmConfigIdField Bind(Type valueType, IReadOnlyList<string> candidateIds)
		{
			configIdType = valueType;
			ids = candidateIds;
			dropdown = GmUi.Dropdown(transform, ids);
			return this;
		}
	}
}