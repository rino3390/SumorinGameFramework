using UnityEngine;

namespace Sumorin.Gm
{
	/// <summary>
	///     單一型別的輸入欄位
	/// </summary>
	/// <remarks>
	///     隨面板一起生成與銷毀，不進 ViewRegistry，也不經 <c>IViewProvider</c>。
	/// </remarks>
	/// <typeparam name="TValue">欄位提供的值型別。型別家族的欄位在執行期才知道實際型別，填 <see cref="object" /></typeparam>
	public abstract class GmField<TValue>: MonoBehaviour, IGmField
	{
		/// <summary>
		///     欄位的當前值
		/// </summary>
		public abstract TValue Value { get; }

	#region IGmField Members
		/// <inheritdoc />
		object IGmField.Value => Value;
	#endregion
	}
}