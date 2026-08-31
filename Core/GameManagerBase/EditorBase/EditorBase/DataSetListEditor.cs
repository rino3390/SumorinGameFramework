using System;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sumorin.SumorinUtility.Editor;
using UnityEngine;

namespace Sumorin.GameManagerBase
{
	/// <summary>
	/// 清單式資料編輯器，把整份 <see cref="DataSet{T}" /> 攤在同一頁編輯
	/// </summary>
	/// <typeparam name="T">繼承自 SODataBase 的資料類型</typeparam>
	/// <remarks>
	/// 與 <see cref="DynamicDataEditor{T}" /> 是同一份資料的兩種編輯模式。
	/// 逐筆模式在左側選單點選單一資產，本模式在單一清單內直接增修。
	/// 清單的新增按鈕會建立一份新資產，資料仍是一筆一個 SO。
	/// 配置取自 T 的 <see cref="DataEditorConfigAttribute" />。
	/// </remarks>
	public class DataSetListEditor<T>: GameEditorMenuBase where T: SODataBase
	{
		private readonly DataEditorConfigAttribute config;

		public override string TabName => config.TabName;

		[Required("尚未建立資料集合")]
		[ShowInInspector]
		[InlineEditor(InlineEditorObjectFieldModes.Hidden)]
		[HideLabel]
		private DataSet<T> dataSet;

		/// <summary>
		/// 初始化編輯器，從 T 的 DataEditorConfigAttribute 讀取配置
		/// </summary>
		public DataSetListEditor()
		{
			config = typeof(T).GetCustomAttribute<DataEditorConfigAttribute>();

			if(config == null)
			{
				throw new InvalidOperationException($"Type {typeof(T).Name} does not have DataEditorConfigAttribute.");
			}
		}

		/// <inheritdoc />
		protected override void OnInitialize()
		{
			dataSet = SumorinEditorUtility.FindAssetWithInheritance<DataSet<T>>() ?? CreateDataSet();
		}

		/// <inheritdoc />
		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = SetTree();
			tree.AddSelfMenu(this, TabName);

			return tree;
		}

		private DataSet<T> CreateDataSet()
		{
			var dataSetType = SumorinEditorUtility.GetDerivedClasses<DataSet<T>>().FirstOrDefault();

			if(dataSetType == null) return null;

			var created = (DataSet<T>)ScriptableObject.CreateInstance(dataSetType);
			SumorinEditorUtility.CreateSOData(created, "Data/Set/" + dataSetType.Name);

			return created;
		}
	}
}