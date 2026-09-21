using UnityEngine;
using UnityEngine.UI;

namespace Sumorin.Gm
{
	/// <summary>
	///     由左到右排列子項，放不下就換到下一行，高度隨行數撐開
	/// </summary>
	/// <remarks>
	///     子項一律用偏好尺寸，不伸縮。
	/// </remarks>
	public class GmFlowLayout: LayoutGroup
	{
		/// <summary>
		///     同一行子項之間的間距
		/// </summary>
		public float Spacing { get; set; } = 6f;

		/// <summary>
		///     行與行之間的間距
		/// </summary>
		public float LineSpacing { get; set; } = 6f;

		/// <inheritdoc />
		/// <remarks>
		///     最小寬度是最寬的子項，偏好寬度是全部排成一行的寬度。
		/// </remarks>
		public override void CalculateLayoutInputHorizontal()
		{
			base.CalculateLayoutInputHorizontal();

			var widest = 0f;
			var total = 0f;

			foreach(var child in rectChildren)
			{
				var width = LayoutUtility.GetPreferredWidth(child);
				widest = Mathf.Max(widest, width);
				total += width;
			}

			total += Spacing * Mathf.Max(0, rectChildren.Count - 1);
			SetLayoutInputForAxis(widest + padding.horizontal, total + padding.horizontal, -1f, 0);
		}

		/// <inheritdoc />
		/// <remarks>
		///     排版系統先算完水平才算垂直，這時自己的寬度已經定了，才算得出要換幾行。
		/// </remarks>
		public override void CalculateLayoutInputVertical()
		{
			var height = Arrange(false);
			SetLayoutInputForAxis(height, height, -1f, 1);
		}

		/// <inheritdoc />
		public override void SetLayoutHorizontal() => Arrange(true);

		/// <inheritdoc />
		public override void SetLayoutVertical() => Arrange(true);

		// 回傳排完的總高度，apply 為 true 時才真的擺放子項
		private float Arrange(bool apply)
		{
			var right = rectTransform.rect.width - padding.right;
			var x = (float)padding.left;
			var y = (float)padding.top;
			var lineHeight = 0f;

			foreach(var child in rectChildren)
			{
				var width = LayoutUtility.GetPreferredWidth(child);
				var height = LayoutUtility.GetPreferredHeight(child);

				// 行首的子項就算比容器寬也不換行，否則會多出一行空行
				if(x + width > right && x > padding.left)
				{
					x = padding.left;
					y += lineHeight + LineSpacing;
					lineHeight = 0f;
				}

				if(apply)
				{
					SetChildAlongAxis(child, 0, x, width);
					SetChildAlongAxis(child, 1, y, height);
				}

				x += width + Spacing;
				lineHeight = Mathf.Max(lineHeight, height);
			}

			return y + lineHeight + padding.bottom;
		}
	}
}