using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Sumorin.Gm
{
	/// <summary>
	///     GM 面板本體，含常駐小按鈕、分類分頁、每條操作的區塊與執行結果的浮動提示
	/// </summary>
	/// <remarks>
	///     全部由程式生成，沒有 prefab 與資產。
	///     欄位輸入值存在生成出來的欄位上，關閉面板只是停用物件，值因此會留到下次開啟。
	/// </remarks>
	public class GmPanelView: MonoBehaviour
	{
		private const int SortingOrder = 30000;
		private const float ScreenMargin = 12f;
		private const float ToastMaxWidth = 360f;
		private const float ToastRise = 20f;
		private const float ToastSecondsPerCharacter = 0.05f;
		private const float ToastMinSeconds = 0.3f;
		private const float ToastMaxSeconds = 1.8f;
		private const float ToastFadeStart = 0.6f;
		private const float EventSystemCheckInterval = 0.5f;

		private static readonly Vector2 PreferredPanelSize = new(760f, 560f);

		private readonly Dictionary<string, GameObject> pages = new();
		private readonly Dictionary<string, Button> tabs = new();

		private Canvas canvas;
		private RectTransform gmButton;
		private GameObject panel;
		private Transform tabBar;
		private Transform pageArea;
		private RectTransform toast;
		private CanvasGroup toastGroup;
		private Text toastText;
		private LayoutElement toastTextSize;
		private Coroutine toastRoutine;
		private Action toastFinished;
		private EventSystem gmEventSystem;
		private float nextEventSystemCheck;
		private string firstCategory;
		private Vector2 fittedScreen;

		private void Update()
		{
			if(canvas == null) return;

			// 視窗大小隨時會變，每幀比一次螢幕尺寸，變了才重排
			var screen = new Vector2(Screen.width, Screen.height);

			if(screen != fittedScreen)
			{
				FitToScreen(screen);
			}

			if(Time.unscaledTime < nextEventSystemCheck) return;

			nextEventSystemCheck = Time.unscaledTime + EventSystemCheckInterval;
			KeepOneEventSystem();
		}

		/// <summary>
		///     依操作列生成整個面板
		/// </summary>
		/// <param name="layouts">每條操作與其參數挑好的欄位建構器與執行方式</param>
		/// <param name="onToastFinished">結果提示播完時呼叫。提示還沒播完就被新提示取代時不呼叫</param>
		public void Build(IReadOnlyList<GmOperationLayout> layouts, Action onToastFinished)
		{
			toastFinished = onToastFinished;
			KeepOneEventSystem();
			CreateCanvas();
			CreateToggleButton();
			CreatePanel();
			CreateToast();

			foreach(var layout in layouts)
			{
				CreateOperationRow(PageFor(layout.Operation.Category).transform, layout);
			}

			if(firstCategory != null)
			{
				ShowPage(firstCategory);
			}

			FitToScreen(new(Screen.width, Screen.height));
		}

		private static object[] ValuesOf(IReadOnlyList<IGmField> fields)
		{
			var values = new object[fields.Count];

			for(var index = 0; index < fields.Count; index++)
			{
				values[index] = fields[index].Value;
			}

			return values;
		}

		// 倍率由 FitToScreen 決定，不用 CanvasScaler：它沒有下限，小視窗會把 14 號字縮到讀不出來
		private void CreateCanvas()
		{
			canvas = gameObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = SortingOrder;

			// 置中的面板遇到奇數尺寸的畫面會落在半個像素上，1 像素的框線和文字會被攤到兩個像素而糊掉
			canvas.pixelPerfect = true;
			gameObject.AddComponent<GraphicRaycaster>();
		}

		private void CreateToggleButton()
		{
			var button = GmUi.Button(transform, "GM", Toggle, 60f);
			button.gameObject.AddComponent<GmDraggable>();

			gmButton = button.GetComponent<RectTransform>();
			gmButton.anchorMin = new(0f, 1f);
			gmButton.anchorMax = new(0f, 1f);
			gmButton.pivot = new(0f, 1f);
			gmButton.anchoredPosition = new(12f, -12f);
			gmButton.sizeDelta = new(60f, 28f);
		}

		private void CreatePanel()
		{
			panel = GmUi.Column(transform, "GM Panel");
			GmUi.Background(panel, GmUi.PanelColor);

			var rect = (RectTransform)panel.transform;
			rect.anchorMin = new(0.5f, 0.5f);
			rect.anchorMax = new(0.5f, 0.5f);
			rect.pivot = new(0.5f, 0.5f);
			rect.anchoredPosition = Vector2.zero;

			var header = GmUi.Row(panel.transform, "Header");
			var tabFlow = GmUi.Flow(header.transform, "Tabs");
			tabFlow.AddComponent<LayoutElement>().flexibleWidth = 1f;
			tabBar = tabFlow.transform;

			var close = GmUi.Button(header.transform, "×", () => panel.SetActive(false), 30f);
			close.GetComponentInChildren<Text>().fontSize = 22;

			pageArea = GmUi.ScrollArea(panel.transform, "Pages");
			panel.SetActive(false);
		}

		// 放在畫布最上層，平常隱藏，執行後從畫面中央浮現
		private void CreateToast()
		{
			var box = GmUi.Row(transform, "Toast");
			GmUi.Background(box, new(0f, 0f, 0f, 0.92f));
			box.GetComponent<HorizontalLayoutGroup>().padding = new(12, 12, 6, 6);

			var fitter = box.AddComponent<ContentSizeFitter>();
			fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			toastGroup = box.AddComponent<CanvasGroup>();
			toastGroup.blocksRaycasts = false;

			toast = (RectTransform)box.transform;
			toastText = GmUi.Label(box.transform, string.Empty);
			toastTextSize = toastText.gameObject.AddComponent<LayoutElement>();
			box.SetActive(false);
		}

		// 畫布小到放不下面板時縮面板、內容改用捲動，GM 按鈕也夾回畫面內
		private void FitToScreen(Vector2 screen)
		{
			fittedScreen = screen;
			canvas.scaleFactor = GmScreenFit.ScaleFactor(screen);

			var canvasSize = screen / canvas.scaleFactor;
			((RectTransform)panel.transform).sizeDelta = GmScreenFit.PanelSize(PreferredPanelSize, canvasSize, ScreenMargin);
			gmButton.anchoredPosition = GmScreenFit.ClampTopLeft(gmButton.anchoredPosition, gmButton.sizeDelta, canvasSize);
		}

		// GM 只補缺：畫面上沒有啟用中的 EventSystem 才建自己的，遊戲的一出現就刪掉自己的交還。
		// 同時有兩個時只有先啟用的會處理輸入，遊戲的 UI 會被 GM 的預設動作接管，Editor 還會每幀印警告
		private void KeepOneEventSystem()
		{
			if(gmEventSystem == null)
			{
				if(EventSystem.current != null) return;

				// 掛在面板底下跟著 GM 走。模組沒指定動作時會在 OnEnable 自己套用預設的 UI 動作
				var host = new GameObject("GM EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
				host.transform.SetParent(transform, false);
				gmEventSystem = host.GetComponent<EventSystem>();
				return;
			}

			foreach(var system in FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
			{
				if(system != gmEventSystem && system.isActiveAndEnabled)
				{
					Destroy(gmEventSystem.gameObject);
					gmEventSystem = null;
					return;
				}
			}
		}

		private void Toggle() => panel.SetActive(!panel.activeSelf);

		private GameObject PageFor(string category)
		{
			if(pages.TryGetValue(category, out var existing)) return existing;

			var page = GmUi.Column(pageArea, $"Page ({category})", 10f);
			pages.Add(category, page);
			firstCategory ??= category;
			tabs.Add(category, GmUi.Button(tabBar, category, () => ShowPage(category), 100f));
			return page;
		}

		private void ShowPage(string category)
		{
			foreach(var page in pages)
			{
				page.Value.SetActive(page.Key == category);
			}

			foreach(var tab in tabs)
			{
				GmUi.MarkSelected(tab.Value, tab.Key == category);
			}
		}

		// 每條操作自成一個區塊：上面是名稱，中間是欄位並依面板寬度自動換行，執行鈕在右下角，面板再窄也不用水平捲動
		private void CreateOperationRow(Transform page, GmOperationLayout layout)
		{
			var operation = layout.Operation;
			var card = GmUi.Card(page, $"Operation ({operation.Name})");
			GmUi.Label(card.transform, operation.Name);

			var fieldArea = GmUi.Flow(card.transform, "Fields", 18f).transform;
			var fields = new List<IGmField>(operation.Parameters.Count);

			for(var index = 0; index < operation.Parameters.Count; index++)
			{
				var parameter = operation.Parameters[index];
				fields.Add(layout.FieldBuilders[index](fieldArea, parameter.Name, parameter.Type));
			}

			var footer = GmUi.Row(card.transform, "Footer");
			footer.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleRight;

			GmUi.Button(footer.transform, "執行", () => ShowToast(layout.Execute(ValuesOf(fields))));
		}

		private void ShowToast(string message)
		{
			if(toastRoutine != null)
			{
				StopCoroutine(toastRoutine);
			}

			toastText.text = message;
			toastTextSize.preferredWidth = Mathf.Min(toastText.preferredWidth, ToastMaxWidth);

			toast.gameObject.SetActive(true);
			toast.anchoredPosition = Vector2.zero;

			// 「已執行」這類短訊息一閃而過，失敗原因較長時停久一點才讀得完
			var duration = Mathf.Clamp(message.Length * ToastSecondsPerCharacter, ToastMinSeconds, ToastMaxSeconds);
			toastRoutine = StartCoroutine(FloatAway(duration));
		}

		// 用不受 timeScale 影響的時間，GM 把遊戲暫停或調慢時提示照樣會消失
		private IEnumerator FloatAway(float duration)
		{
			var start = toast.anchoredPosition;

			for(var elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
			{
				var progress = elapsed / duration;
				toast.anchoredPosition = start + new Vector2(0f, ToastRise * progress);
				toastGroup.alpha = progress < ToastFadeStart ? 1f : 1f - (progress - ToastFadeStart) / (1f - ToastFadeStart);
				yield return null;
			}

			toast.gameObject.SetActive(false);
			toastRoutine = null;
			toastFinished?.Invoke();
		}
	}
}