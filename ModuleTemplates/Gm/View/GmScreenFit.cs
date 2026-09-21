using UnityEngine;

namespace Sumorin.Gm
{
	/// <summary>
	///     GM 面板在各種解析度下的縮放與擺放規則
	/// </summary>
	public static class GmScreenFit
	{
		/// <summary>
		///     畫布倍率的下限
		/// </summary>
		/// <remarks>
		///     14 號字縮到約 10.5 像素，再小中文就讀不出來。比這更小的螢幕改由面板縮小尺寸、內容捲動來容納。
		/// </remarks>
		public const float MinScale = 0.75f;

		/// <summary>
		///     介面尺寸的設計基準解析度
		/// </summary>
		public static readonly Vector2 ReferenceResolution = new(1280f, 720f);

		/// <summary>
		///     畫布倍率，依螢幕較窄的一邊相對基準解析度等比縮放，最低 <see cref="MinScale" />
		/// </summary>
		/// <param name="screenSize">螢幕像素尺寸</param>
		/// <returns>倍率</returns>
		public static float ScaleFactor(Vector2 screenSize) =>
			Mathf.Max(MinScale, Mathf.Min(screenSize.x / ReferenceResolution.x, screenSize.y / ReferenceResolution.y));

		/// <summary>
		///     面板尺寸，畫布放不下時縮到四邊都留 <paramref name="margin" /> 的大小
		/// </summary>
		/// <param name="preferred">畫布夠大時的尺寸</param>
		/// <param name="canvasSize">畫布尺寸</param>
		/// <param name="margin">面板與畫布邊緣至少要留的距離</param>
		/// <returns>面板尺寸</returns>
		public static Vector2 PanelSize(Vector2 preferred, Vector2 canvasSize, float margin) =>
			Vector2.Min(preferred, canvasSize - Vector2.one * (margin * 2f));

		/// <summary>
		///     把錨點與軸心都在左上角的元素夾在畫布內
		/// </summary>
		/// <param name="position">元素的 anchoredPosition</param>
		/// <param name="size">元素尺寸</param>
		/// <param name="canvasSize">畫布尺寸</param>
		/// <returns>夾過的 anchoredPosition</returns>
		public static Vector2 ClampTopLeft(Vector2 position, Vector2 size, Vector2 canvasSize) =>
			new(Mathf.Clamp(position.x, 0f, Mathf.Max(0f, canvasSize.x - size.x)), Mathf.Clamp(position.y, -Mathf.Max(0f, canvasSize.y - size.y), 0f));
	}
}