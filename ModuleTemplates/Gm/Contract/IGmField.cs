using System;
using UnityEngine;

namespace Sumorin.Gm
{
	/// <summary>
	///     面板上一個參數的輸入欄位，提供當前值
	/// </summary>
	public interface IGmField
	{
		/// <summary>
		///     欄位的當前值，型別與對應參數相同
		/// </summary>
		object Value { get; }
	}

	/// <summary>
	///     建立某個型別的輸入欄位
	/// </summary>
	/// <param name="parent">欄位要掛上去的操作列</param>
	/// <param name="label">欄位標籤，即參數名稱</param>
	/// <param name="valueType">參數型別，型別家族的建構器靠它決定實際要產生哪種值</param>
	/// <returns>建立好的欄位</returns>
	public delegate IGmField GmFieldBuilder(Transform parent, string label, Type valueType);
}