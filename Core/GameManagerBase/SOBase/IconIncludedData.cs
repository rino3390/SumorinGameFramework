using Sirenix.OdinInspector;
using UnityEngine;

namespace Sumorin.GameFramework.GameManagerBase
{
	/// <summary>
	/// 包含圖示的資料基底類別
	/// </summary>
	public abstract class IconIncludedData: SODataBase
	{
		/// <summary>
		/// 資料圖示
		/// </summary>
		[HideLabel, PreviewField(70, ObjectFieldAlignment.Center)]
		[HorizontalGroup(LayoutConst.TopInfoLayout, 200)]
		[PropertyOrder(-1), PropertySpace(20)]
		public Sprite Icon;
	}
}