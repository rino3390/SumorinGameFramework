using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Sumorin.Gm
{
	/// <summary>
	///     GM 面板的 uGUI 生成工具，遊戲側自訂欄位也用這組方法建構件
	/// </summary>
	/// <remarks>
	///     一律走 <see cref="DefaultControls" />，Unity 自己就會把 InputField 的子物件與 Dropdown 的樣板接好。
	///     傳空的 Resources 代表不指定圖，Image 沒有 sprite 時畫成純色方塊，GM 面板不需要美術資產。
	/// </remarks>
	public static class GmUi
	{
		private static readonly DefaultControls.Resources BlankResources = new();

		/// <summary>
		///     建立直向排列的容器
		/// </summary>
		/// <param name="parent">父物件</param>
		/// <param name="name">物件名稱</param>
		/// <param name="spacing">子項間距</param>
		/// <returns>容器物件</returns>
		public static GameObject Column(Transform parent, string name, float spacing = 4f)
		{
			var container = CreateContainer(parent, name);
			var layout = container.AddComponent<VerticalLayoutGroup>();
			ConfigureLayout(layout, spacing);
			return container;
		}

		/// <summary>
		///     建立橫向排列的容器
		/// </summary>
		/// <param name="parent">父物件</param>
		/// <param name="name">物件名稱</param>
		/// <param name="spacing">子項間距</param>
		/// <returns>容器物件</returns>
		public static GameObject Row(Transform parent, string name, float spacing = 6f)
		{
			var container = CreateContainer(parent, name);
			var layout = container.AddComponent<HorizontalLayoutGroup>();
			ConfigureLayout(layout, spacing);
			container.AddComponent<LayoutElement>().preferredHeight = 26f;
			return container;
		}

		/// <summary>
		///     建立文字
		/// </summary>
		/// <param name="parent">父物件</param>
		/// <param name="text">文字內容</param>
		/// <param name="preferredWidth">偏好寬度，0 表示不限制</param>
		/// <returns>文字元件</returns>
		public static Text Label(Transform parent, string text, float preferredWidth = 0f)
		{
			var label = Attach(DefaultControls.CreateText(BlankResources), parent).GetComponent<Text>();
			label.text = text;
			label.alignment = TextAnchor.MiddleLeft;

			if(preferredWidth > 0f)
			{
				Size(label.gameObject, preferredWidth);
			}

			return label;
		}

		/// <summary>
		///     建立按鈕
		/// </summary>
		/// <param name="parent">父物件</param>
		/// <param name="label">按鈕文字</param>
		/// <param name="onClick">點擊後要做的事</param>
		/// <param name="preferredWidth">偏好寬度</param>
		/// <returns>按鈕元件</returns>
		public static Button Button(Transform parent, string label, UnityAction onClick, float preferredWidth = 64f)
		{
			var button = Attach(DefaultControls.CreateButton(BlankResources), parent).GetComponent<Button>();
			var text = button.GetComponentInChildren<Text>();
			text.text = label;
			button.onClick.AddListener(onClick);
			Size(button.gameObject, preferredWidth);
			return button;
		}

		/// <summary>
		///     建立輸入框
		/// </summary>
		/// <param name="parent">父物件</param>
		/// <param name="contentType">輸入限制</param>
		/// <param name="preferredWidth">偏好寬度</param>
		/// <returns>輸入框元件</returns>
		public static InputField Input(Transform parent, InputField.ContentType contentType, float preferredWidth = 110f)
		{
			var input = Attach(DefaultControls.CreateInputField(BlankResources), parent).GetComponent<InputField>();
			input.contentType = contentType;
			input.text = string.Empty;
			Size(input.gameObject, preferredWidth);
			return input;
		}

		/// <summary>
		///     建立勾選框
		/// </summary>
		/// <param name="parent">父物件</param>
		/// <returns>勾選框元件</returns>
		public static Toggle Toggle(Transform parent)
		{
			var toggle = Attach(DefaultControls.CreateToggle(BlankResources), parent).GetComponent<Toggle>();
			toggle.isOn = false;

			// 勾勾沒有指定圖時畫成白色方塊，疊在白色底上看不出來，改成深色才分得出勾沒勾
			if(toggle.graphic is Image checkmark)
			{
				checkmark.color = new(0.15f, 0.15f, 0.15f, 1f);
			}

			Size(toggle.gameObject, 30f);
			return toggle;
		}

		/// <summary>
		///     建立下拉選單
		/// </summary>
		/// <param name="parent">父物件</param>
		/// <param name="options">候選項目，可以是空的</param>
		/// <param name="preferredWidth">偏好寬度</param>
		/// <returns>下拉選單元件</returns>
		public static Dropdown Dropdown(Transform parent, IReadOnlyList<string> options, float preferredWidth = 150f)
		{
			var dropdown = Attach(DefaultControls.CreateDropdown(BlankResources), parent).GetComponent<Dropdown>();
			dropdown.ClearOptions();
			dropdown.AddOptions(new List<string>(options));
			dropdown.value = 0;
			dropdown.RefreshShownValue();
			Size(dropdown.gameObject, preferredWidth);
			return dropdown;
		}

		/// <summary>
		///     鋪一層背景色
		/// </summary>
		/// <param name="target">要加背景的物件</param>
		/// <param name="color">背景色</param>
		public static void Background(GameObject target, Color color)
		{
			target.AddComponent<Image>().color = color;
		}

		private static GameObject CreateContainer(Transform parent, string name)
		{
			var container = new GameObject(name, typeof(RectTransform));
			container.transform.SetParent(parent, false);
			return container;
		}

		private static void ConfigureLayout(HorizontalOrVerticalLayoutGroup layout, float spacing)
		{
			layout.spacing = spacing;
			layout.padding = new(6, 6, 6, 6);
			layout.childForceExpandWidth = false;
			layout.childForceExpandHeight = false;
			layout.childControlWidth = true;
			layout.childControlHeight = true;
			layout.childAlignment = TextAnchor.MiddleLeft;
		}

		private static GameObject Attach(GameObject element, Transform parent)
		{
			element.transform.SetParent(parent, false);
			return element;
		}

		private static void Size(GameObject element, float preferredWidth)
		{
			var layoutElement = element.AddComponent<LayoutElement>();
			layoutElement.preferredWidth = preferredWidth;
			layoutElement.preferredHeight = 24f;
		}
	}
}