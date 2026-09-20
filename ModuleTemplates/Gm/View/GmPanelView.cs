using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sumorin.Gm
{
	/// <summary>
	///     GM 面板本體，含常駐小按鈕、分類分頁、操作列與結果訊息列
	/// </summary>
	/// <remarks>
	///     全部由程式生成，沒有 prefab 與資產。
	///     欄位輸入值存在生成出來的欄位上，關閉面板只是停用物件，值因此會留到下次開啟。
	/// </remarks>
	public class GmPanelView: MonoBehaviour
	{
		private const int SortingOrder = 30000;
		private const float PanelWidth = 760f;

		private readonly Dictionary<string, GameObject> pages = new();

		private GameObject panel;
		private Transform tabBar;
		private Transform pageArea;
		private Text resultText;
		private string firstCategory;

		/// <summary>
		///     依操作列生成整個面板
		/// </summary>
		/// <param name="layouts">每條操作與其參數挑好的欄位建構器與執行方式</param>
		public void Build(IReadOnlyList<GmOperationLayout> layouts)
		{
			EnsureEventSystem();
			CreateCanvas();
			CreateToggleButton();
			CreatePanel();

			foreach(var layout in layouts)
			{
				CreateOperationRow(PageFor(layout.Operation.Category).transform, layout);
			}

			if(firstCategory != null)
			{
				ShowPage(firstCategory);
			}
		}

		// EventSystem 沒有就補一個。輸入模組挑哪個由專案裝了哪套輸入決定，
		// Input System 套件在就用它的模組，只有舊版 Input 的專案才退回 StandaloneInputModule
		private static void EnsureEventSystem()
		{
			if(EventSystem.current != null) return;

			var host = new GameObject("GM EventSystem", typeof(EventSystem));
			var moduleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

			if(moduleType != null)
			{
				host.AddComponent(moduleType);
			}
			else
			{
				host.AddComponent<StandaloneInputModule>();
			}
		}

		// 面板只佔畫面左側一條，右邊留給遊戲。鋪滿的話背景會吃掉整個畫面的點擊，面板一開就點不到遊戲
		private static void DockLeft(RectTransform rect, float width, float top, float bottom)
		{
			rect.anchorMin = new(0f, 0f);
			rect.anchorMax = new(0f, 1f);
			rect.pivot = new(0f, 0.5f);
			rect.offsetMin = new(12f, bottom);
			rect.offsetMax = new(12f + width, -top);
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

		private void CreateCanvas()
		{
			var canvas = gameObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = SortingOrder;

			var scaler = gameObject.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new(1920f, 1080f);

			gameObject.AddComponent<GraphicRaycaster>();
		}

		private void CreateToggleButton()
		{
			var button = GmUi.Button(transform, "GM", Toggle, 60f);
			var rect = button.GetComponent<RectTransform>();
			rect.anchorMin = new(0f, 1f);
			rect.anchorMax = new(0f, 1f);
			rect.pivot = new(0f, 1f);
			rect.anchoredPosition = new(12f, -12f);
			rect.sizeDelta = new(60f, 28f);
		}

		private void CreatePanel()
		{
			panel = GmUi.Column(transform, "GM Panel");
			GmUi.Background(panel, new(0.86f, 0.86f, 0.86f, 0.95f));
			DockLeft(panel.GetComponent<RectTransform>(), PanelWidth, 48f, 12f);

			tabBar = GmUi.Row(panel.transform, "Tabs").transform;

			var pageHost = GmUi.Column(panel.transform, "Pages");
			pageHost.AddComponent<LayoutElement>().flexibleHeight = 1f;
			pageArea = pageHost.transform;

			resultText = GmUi.Label(panel.transform, string.Empty);
			resultText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

			panel.SetActive(false);
		}

		private void Toggle() => panel.SetActive(!panel.activeSelf);

		private GameObject PageFor(string category)
		{
			if(pages.TryGetValue(category, out var existing)) return existing;

			var page = GmUi.Column(pageArea, $"Page ({category})");
			pages.Add(category, page);
			firstCategory ??= category;
			GmUi.Button(tabBar, category, () => ShowPage(category), 100f);
			return page;
		}

		private void ShowPage(string category)
		{
			foreach(var page in pages)
			{
				page.Value.SetActive(page.Key == category);
			}
		}

		// ponytail: 操作列直接排在面板上，沒有捲動。操作多到排不下時要把 Pages 換成 ScrollView
		private void CreateOperationRow(Transform page, GmOperationLayout layout)
		{
			var operation = layout.Operation;
			var row = GmUi.Row(page, $"Operation ({operation.Name})");
			GmUi.Label(row.transform, operation.Name, 160f);

			var fields = new List<IGmField>(operation.Parameters.Count);

			for(var index = 0; index < operation.Parameters.Count; index++)
			{
				var parameter = operation.Parameters[index];
				fields.Add(layout.FieldBuilders[index](row.transform, parameter.Name, parameter.Type));
			}

			GmUi.Button(row.transform, "執行", () => resultText.text = layout.Execute(ValuesOf(fields)));
		}
	}
}