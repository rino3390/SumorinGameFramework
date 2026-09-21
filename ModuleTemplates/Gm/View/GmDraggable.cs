using UnityEngine;
using UnityEngine.EventSystems;

namespace Sumorin.Gm
{
	/// <summary>
	///     讓畫布上錨點與軸心都在左上角的元素可以用滑鼠拖曳，位置夾在畫布內
	/// </summary>
	public class GmDraggable: MonoBehaviour, IBeginDragHandler, IDragHandler
	{
	#region IBeginDragHandler Members
		/// <inheritdoc />
		/// <remarks>
		///     輸入模組只在按下與拖曳的是不同物件時才取消點擊。掛在按鈕上時兩者是同一個，不自己取消的話拖完放開會被當成按了按鈕。
		/// </remarks>
		public void OnBeginDrag(PointerEventData eventData)
		{
			eventData.eligibleForClick = false;
		}
	#endregion

	#region IDragHandler Members
		/// <inheritdoc />
		public void OnDrag(PointerEventData eventData)
		{
			var rect = (RectTransform)transform;
			var canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
			var position = rect.anchoredPosition + eventData.delta / canvas.scaleFactor;
			rect.anchoredPosition = GmScreenFit.ClampTopLeft(position, rect.rect.size, ((RectTransform)canvas.transform).rect.size);
		}
	#endregion
	}
}