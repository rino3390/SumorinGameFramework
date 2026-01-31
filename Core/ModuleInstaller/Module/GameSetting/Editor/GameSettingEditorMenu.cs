using System;
using System.Collections.Generic;
using Rino.GameFramework.GameManagerBase;
using Rino.GameFramework.RinoUtility.Editor;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace Rino.GameFramework.GameSetting
{
	/// <summary>
	/// 遊戲設定編輯器選單
	/// </summary>
	public class GameSettingEditorMenu : GameEditorMenuBase
	{
		public override string TabName => "遊戲設定";

		private GameSettingConfig config;
		private readonly Dictionary<Type, GameEditorMenuBase> editorCache = new();

		protected override OdinMenuTree BuildMenuTree()
		{
			var tree = SetTree(iconSize: 20);
			config = GetOrCreateConfig();

			foreach (var item in config.Settings)
			{
				var editor = GetOrCreateEditor(item.SettingEditorType);
				if (editor == null) continue;

				editor.EnsureInitialized();
				tree.Add(editor.TabName, editor, item.Icon);
			}

			return tree;
		}

		private GameEditorMenuBase GetOrCreateEditor(Type editorType)
		{
			if (editorType == null) return null;

			if(editorCache.TryGetValue(editorType, out var cachedEditor)) return cachedEditor;

			cachedEditor = Activator.CreateInstance(editorType) as GameEditorMenuBase;
			editorCache[editorType] = cachedEditor;

			return cachedEditor;
		}

		private GameSettingConfig GetOrCreateConfig()
		{
			var data = RinoEditorUtility.FindAsset<GameSettingConfig>();
			if (data != null) return data;

			data = ScriptableObject.CreateInstance<GameSettingConfig>();
			RinoEditorUtility.CreateSOData(data, "Data/GameManager/GameSettingConfig");
			return data;
		}
	}
}
