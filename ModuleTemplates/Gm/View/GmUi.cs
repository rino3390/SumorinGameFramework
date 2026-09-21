using System;
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
	///     底圖不用 DefaultControls 的資源，改用程式畫出的圓角貼圖，GM 面板不需要美術資產。
	/// </remarks>
	public static class GmUi
	{
		/// <summary>
		///     面板底色
		/// </summary>
		/// <remarks>
		///     Linear 色彩空間的半透明混色比 sRGB 淡，0.85 在實機上看起來約等於一般圖片工具裡的 0.6。
		/// </remarks>
		public static readonly Color PanelColor = new(0f, 0f, 0f, 0.85f);

		// DefaultControls 的輸入框與下拉把文字上下各內縮 6 與 7，控制項矮於 30 時 14 號字放不進文字框。
		// Text 預設 verticalOverflow 為 Truncate，放不下的那一行會整行不畫
		private const float ControlHeight = 30f;

		private static readonly DefaultControls.Resources BlankResources = new();
		private static readonly Color ControlColor = new(0.2f, 0.2f, 0.2f, 0.9f);
		private static readonly Color TextColor = new(0.95f, 0.95f, 0.95f, 1f);
		private static readonly Color DimTextColor = new(0.95f, 0.95f, 0.95f, 0.4f);

		// 按鈕平常會被 Skin 的 normalColor 壓成 0.85 倍，貼圖顏色是反推回去的，平常看到的才是炭灰底與 0.85 白框
		private static readonly Sprite ButtonSprite = RoundedSprite(new(0.2f, 0.2f, 0.22f, 1f), Color.white, 7f, 0.18f);
		private static readonly Sprite SelectedButtonSprite = RoundedSprite(new(0.42f, 0.42f, 0.46f, 1f), Color.white, 7f, 0.28f);
		private static readonly Sprite FieldSprite = RoundedSprite(new(0f, 0f, 0f, 0.65f), new(1f, 1f, 1f, 0.55f), 5f);
		private static readonly Sprite BoxSprite = RoundedSprite(Color.black, new(1f, 1f, 1f, 0.4f), 10f);

		// 區塊底比面板暗，分區靠白框。白色半透明在 Linear 色彩空間看起來比數值亮很多，0.025 就會把區塊墊亮、吃掉淺色字的對比
		private static readonly Sprite CardSprite = RoundedSprite(new(0f, 0f, 0f, 0.35f), new(1f, 1f, 1f, 0.25f), 7f);
		private static readonly Sprite MarkSprite = RoundedSprite(Color.white, Color.white, 2f);
		private static readonly Sprite ArrowSprite = DownArrowSprite();

		/// <summary>
		///     建立直向排列的容器，子項寬度撐滿容器
		/// </summary>
		/// <param name="parent">父物件</param>
		/// <param name="name">物件名稱</param>
		/// <param name="spacing">子項間距</param>
		/// <returns>容器物件</returns>
		public static GameObject Column(Transform parent, string name, float spacing = 4f)
		{
			var container = CreateContainer(parent, name);
			var layout = container.AddComponent<VerticalLayoutGroup>();
			ConfigureLayout(layout, spacing, 6, TextAnchor.UpperLeft);

			// 子項撐滿寬度，Flow 才知道自己有多寬、該在哪裡換行
			layout.childForceExpandWidth = true;
			return container;
		}

		/// <summary>
		///     建立橫向排列的容器，高度依子項撐開
		/// </summary>
		/// <remarks>
		///     不加內距。欄位本身也是一列，巢狀時內距會一層層吃掉控制項的高度。
		/// </remarks>
		/// <param name="parent">父物件</param>
		/// <param name="name">物件名稱</param>
		/// <param name="spacing">子項間距</param>
		/// <returns>容器物件</returns>
		public static GameObject Row(Transform parent, string name, float spacing = 6f)
		{
			var container = CreateContainer(parent, name);
			var layout = container.AddComponent<HorizontalLayoutGroup>();
			ConfigureLayout(layout, spacing, 0, TextAnchor.MiddleLeft);
			return container;
		}

		/// <summary>
		///     建立會自動換行的容器，子項由左到右排，寬度不夠就換到下一行
		/// </summary>
		/// <param name="parent">父物件</param>
		/// <param name="name">物件名稱</param>
		/// <param name="spacing">同一行子項之間的間距</param>
		/// <param name="lineSpacing">行與行之間的間距</param>
		/// <returns>容器物件</returns>
		public static GameObject Flow(Transform parent, string name, float spacing = 6f, float lineSpacing = 6f)
		{
			var container = CreateContainer(parent, name);
			var layout = container.AddComponent<GmFlowLayout>();
			layout.Spacing = spacing;
			layout.LineSpacing = lineSpacing;
			return container;
		}

		/// <summary>
		///     建立「標籤 + 控制項」的欄位列，標籤依文字寬度，控制項接著生在同一列
		/// </summary>
		/// <remarks>
		///     自訂欄位也用它建列，外觀才會和內建欄位一致。
		/// </remarks>
		/// <param name="parent">欄位建構器收到的父物件</param>
		/// <param name="label">欄位標籤</param>
		/// <returns>欄位列</returns>
		public static GameObject FieldRow(Transform parent, string label)
		{
			var row = Row(parent, $"Field ({label})");
			Label(row.transform, label);
			return row;
		}

		/// <summary>
		///     建立有淡色圓角底的直向區塊，把同一組元件框在一起
		/// </summary>
		/// <param name="parent">父物件</param>
		/// <param name="name">物件名稱</param>
		/// <returns>區塊物件，子項直向排列並撐滿寬度</returns>
		public static GameObject Card(Transform parent, string name)
		{
			var card = Column(parent, name);
			card.GetComponent<VerticalLayoutGroup>().padding = new(10, 10, 8, 8);

			var image = card.AddComponent<Image>();
			image.sprite = CardSprite;
			image.type = Image.Type.Sliced;
			return card;
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
			label.color = TextColor;
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
			Skin(button, ButtonSprite);
			var text = button.GetComponentInChildren<Text>();
			text.text = label;
			text.color = TextColor;
			button.onClick.AddListener(onClick);
			Size(button.gameObject, preferredWidth);
			return button;
		}

		/// <summary>
		///     切換按鈕的選中外觀，分頁用它標出目前顯示的是哪一頁
		/// </summary>
		/// <param name="button">由 <see cref="Button(Transform,string,UnityAction,float)" /> 建立的按鈕</param>
		/// <param name="selected">是否選中</param>
		public static void MarkSelected(Button button, bool selected)
		{
			button.image.sprite = selected ? SelectedButtonSprite : ButtonSprite;
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
			Skin(input, FieldSprite);
			input.textComponent.color = TextColor;
			((Text)input.placeholder).text = string.Empty;
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
			Skin(toggle, FieldSprite);

			// DefaultControls 把方框釘在左上角，放進比它高的列會往上偏
			var box = toggle.targetGraphic.rectTransform;
			box.anchorMin = new(0.5f, 0.5f);
			box.anchorMax = new(0.5f, 0.5f);
			box.anchoredPosition = Vector2.zero;

			var mark = (Image)toggle.graphic;
			mark.sprite = MarkSprite;
			mark.type = Image.Type.Sliced;
			mark.color = TextColor;
			mark.rectTransform.sizeDelta = new(10f, 10f);

			// 附帶的「Toggle」標籤在 30 寬的勾選框裡只剩 2 寬，留著只會擠出殘字
			toggle.GetComponentInChildren<Text>().gameObject.SetActive(false);

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
			Skin(dropdown, FieldSprite);
			dropdown.captionText.color = TextColor;
			dropdown.itemText.color = TextColor;

			var arrow = dropdown.transform.Find("Arrow").GetComponent<Image>();
			arrow.sprite = ArrowSprite;
			arrow.color = TextColor;
			arrow.rectTransform.sizeDelta = new(10f, 10f);

			var template = dropdown.template.GetComponent<Image>();
			template.sprite = FieldSprite;
			template.type = Image.Type.Sliced;
			template.color = Color.white;
			dropdown.template.GetComponentInChildren<Toggle>(true).targetGraphic.color = ControlColor;

			var scrollbar = dropdown.template.GetComponentInChildren<Scrollbar>(true);
			scrollbar.GetComponent<Image>().color = ControlColor;
			scrollbar.targetGraphic.color = DimTextColor;

			dropdown.ClearOptions();
			dropdown.AddOptions(new List<string>(options));
			dropdown.value = 0;
			dropdown.RefreshShownValue();
			Size(dropdown.gameObject, preferredWidth);
			return dropdown;
		}

		/// <summary>
		///     建立填滿剩餘空間的垂直捲動區域，內容超出時用滑鼠滾輪或拖曳空白處捲動
		/// </summary>
		/// <param name="parent">父物件，需為排版容器</param>
		/// <param name="name">物件名稱</param>
		/// <returns>內容容器，子項直向排列，寬度跟著捲動區、高度隨內容撐開</returns>
		public static Transform ScrollArea(Transform parent, string name)
		{
			var viewport = CreateContainer(parent, name);
			viewport.AddComponent<RectMask2D>();

			// 透明底圖只為了接收射線，空白處才滾得動、拖得動
			viewport.AddComponent<Image>().color = Color.clear;

			var layoutElement = viewport.AddComponent<LayoutElement>();
			layoutElement.flexibleWidth = 1f;
			layoutElement.flexibleHeight = 1f;

			var content = Column(viewport.transform, "Content");
			var contentRect = (RectTransform)content.transform;
			contentRect.anchorMin = new(0f, 1f);
			contentRect.anchorMax = new(1f, 1f);
			contentRect.pivot = new(0.5f, 1f);
			contentRect.sizeDelta = Vector2.zero;
			content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			// 內容寬度固定等於捲動區，放不下的欄位靠 Flow 換行，不做水平捲動
			var scroll = viewport.AddComponent<ScrollRect>();
			scroll.horizontal = false;
			scroll.viewport = (RectTransform)viewport.transform;
			scroll.content = contentRect;
			scroll.movementType = ScrollRect.MovementType.Clamped;
			scroll.scrollSensitivity = 30f;
			return content.transform;
		}

		/// <summary>
		///     鋪一層圓角白邊的背景
		/// </summary>
		/// <param name="target">要加背景的物件</param>
		/// <param name="color">背景色</param>
		public static void Background(GameObject target, Color color)
		{
			var image = target.AddComponent<Image>();
			image.sprite = BoxSprite;
			image.type = Image.Type.Sliced;
			image.color = color;
		}

		private static GameObject CreateContainer(Transform parent, string name)
		{
			var container = new GameObject(name, typeof(RectTransform));
			container.transform.SetParent(parent, false);
			return container;
		}

		private static void ConfigureLayout(HorizontalOrVerticalLayoutGroup layout, float spacing, int padding, TextAnchor alignment)
		{
			layout.spacing = spacing;
			layout.padding = new(padding, padding, padding, padding);
			layout.childForceExpandWidth = false;
			layout.childForceExpandHeight = false;
			layout.childControlWidth = true;
			layout.childControlHeight = true;
			layout.childAlignment = alignment;
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
			layoutElement.preferredHeight = ControlHeight;
		}

		private static void Skin(Selectable selectable, Sprite sprite)
		{
			var image = (Image)selectable.targetGraphic;
			image.sprite = sprite;
			image.type = Image.Type.Sliced;
			image.color = Color.white;

			// 平常壓暗一點，滑過回到貼圖原色，才有變亮的回饋
			var colors = selectable.colors;
			colors.normalColor = new(0.85f, 0.85f, 0.85f, 1f);
			colors.selectedColor = colors.normalColor;
			colors.highlightedColor = Color.white;
			colors.pressedColor = new(0.65f, 0.65f, 0.65f, 1f);
			selectable.colors = colors;
		}

		// 圓角 9-slice 貼圖：外圈一單位是邊框色，內部單色。
		// shine 大於 0 時，框線內側上緣那一圈往白色提亮，做出凸起的立體感
		private static Sprite RoundedSprite(Color fill, Color edge, float radius, float shine = 0f)
		{
			const float size = 32f;

			return PaintSprite(
				size,
				radius + 1f,
				point =>
				{
					// 圓心夾在內縮 radius 的矩形內，直邊上的點算出來的距離就是到邊的距離，四角則是到圓角圓心的距離
					var center = new Vector2(Mathf.Clamp(point.x, radius, size - radius), Mathf.Clamp(point.y, radius, size - radius));
					var distance = Vector2.Distance(point, center);
					if(distance > radius) return Color.clear;
					if(distance > radius - 1f) return edge;
					if(distance > radius - 2f && point.y > size - radius) return Color.Lerp(fill, new(1f, 1f, 1f, fill.a), shine);

					return fill;
				}
			);
		}

		// 尖端朝下的三角形，底邊在 y = 12、尖端在 y = 4
		private static Sprite DownArrowSprite()
		{
			return PaintSprite(
				16f,
				0f,
				point =>
				{
					var inside = point.y is >= 4f and <= 12f && Mathf.Abs(point.x - 8f) <= (point.y - 4f) * 0.75f;
					return inside ? Color.white : Color.clear;
				}
			);
		}

		// paint 以介面單位描述形狀，只回傳硬邊的顏色。
		// 貼圖用 2 倍密度畫，每個貼圖像素取 4 × 4 個樣本平均出覆蓋率，圓角內外兩側都有反鋸齒。
		// 1080p 時剛好 2:1 對齊縮小，4K 時 1:1，兩種都不會糊
		private static Sprite PaintSprite(float size, float border, Func<Vector2, Color> paint)
		{
			const int density = 2;
			const int samples = 4;

			var pixels = (int)size * density;
			var texture = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false)
			{
				wrapMode = TextureWrapMode.Clamp,
				hideFlags = HideFlags.HideAndDontSave
			};

			for(var y = 0; y < pixels; y++)
			{
				for(var x = 0; x < pixels; x++)
				{
					// 顏色依覆蓋率加權平均，透明樣本才不會把邊緣染黑
					var color = Vector3.zero;
					var coverage = 0f;

					for(var sampleY = 0; sampleY < samples; sampleY++)
					{
						for(var sampleX = 0; sampleX < samples; sampleX++)
						{
							var point = new Vector2(x + (sampleX + 0.5f) / samples, y + (sampleY + 0.5f) / samples) / density;
							var sample = paint(point);
							color += new Vector3(sample.r, sample.g, sample.b) * sample.a;
							coverage += sample.a;
						}
					}

					texture.SetPixel(
						x,
						y,
						coverage > 0f ? new(color.x / coverage, color.y / coverage, color.z / coverage, coverage / (samples * samples)) : Color.clear
					);
				}
			}

			texture.Apply();

			var sprite = Sprite.Create(
				texture,
				new(0f, 0f, pixels, pixels),
				new(0.5f, 0.5f),
				100f * density,
				0,
				SpriteMeshType.FullRect,
				Vector4.one * (border * density)
			);
			sprite.hideFlags = HideFlags.HideAndDontSave;
			return sprite;
		}
	}
}