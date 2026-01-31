using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Rino.GameFramework.RinoUtility.Editor
{
	/// <summary>
	/// Unity Editor 工具類別，提供資產搜尋、建立等常用功能
	/// </summary>
	public class RinoEditorUtility
	{
		/// <summary>
		/// 搜尋專案中所有指定類型的資產
		/// </summary>
		/// <typeparam name="T">資產類型</typeparam>
		/// <returns>找到的資產清單</returns>
		public static List<T> FindAssets<T>() where T: Object
		{
			var data = AssetDatabase.FindAssets($"t:{typeof(T).Name}")
									.Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
									.ToList();
			return data;
		}

		/// <summary>
		/// 搜尋專案中第一個指定類型的資產
		/// </summary>
		/// <typeparam name="T">資產類型</typeparam>
		/// <returns>找到的第一個資產，若無則回傳 null</returns>
		public static T FindAsset<T>() where T: Object
		{
			return FindAssets<T>().FirstOrDefault();
		}

		/// <summary>
		/// 搜尋專案中所有繼承指定類型的資產
		/// </summary>
		/// <typeparam name="T">基底類型</typeparam>
		/// <returns>找到的資產清單</returns>
		public static List<T> FindAssetsWithInheritance<T>() where T: Object
		{
			var type = GetDerivedClasses<T>().FirstOrDefault();
			var data = AssetDatabase.FindAssets($"t:{type}").Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid))).ToList();
			return data;
		}

		/// <summary>
		/// 搜尋專案中第一個繼承指定類型的資產
		/// </summary>
		/// <typeparam name="T">基底類型</typeparam>
		/// <returns>找到的第一個資產，若無則回傳 null</returns>
		public static T FindAssetWithInheritance<T>() where T: Object
		{
			return FindAssetsWithInheritance<T>().FirstOrDefault();
		}

		/// <summary>
		/// 儲存 ScriptableObject 資料
		/// </summary>
		/// <param name="serializedObject">要儲存的物件</param>
		public static void SaveSOData(Object serializedObject)
		{
			EditorUtility.SetDirty(serializedObject);
			AssetDatabase.SaveAssets();
		}

		/// <summary>
		/// 建立 ScriptableObject 資產檔案
		/// </summary>
		/// <param name="data">要建立的 ScriptableObject</param>
		/// <param name="path">儲存路徑（相對於 Assets 資料夾，不含副檔名）</param>
		public static void CreateSOData(ScriptableObject data, string path)
		{
			var dir = "Assets/" + path;
			CreateDirectoryIfNotExist(dir);
			AssetDatabase.CreateAsset(data, dir + ".asset");
			AssetDatabase.SaveAssets();
		}

		/// <summary>
		/// 若目錄不存在則建立目錄
		/// </summary>
		/// <param name="dir">目錄路徑</param>
		public static void CreateDirectoryIfNotExist(string dir)
		{
			var directoryName = Path.GetDirectoryName(dir);

			if(!Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName!);
			}
		}

		/// <summary>
		/// 取得所有繼承指定類型的類別
		/// </summary>
		/// <typeparam name="T">基底類型</typeparam>
		/// <param name="searchAbstract">是否包含抽象類別</param>
		/// <param name="searchGeneric">是否包含泛型類別</param>
		/// <param name="excludeGenericBase">要排除的泛型基類（可選）</param>
		/// <returns>繼承指定類型的類別清單</returns>
		public static List<Type> GetDerivedClasses<T>(bool searchAbstract = false, bool searchGeneric = false, Type excludeGenericBase = null)
		{
			var query = AppDomain.CurrentDomain.GetAssemblies()
								 .SelectMany(s => s.GetTypes())
								 .Where(x => searchAbstract || !x.IsAbstract)
								 .Where(x => searchGeneric || !x.IsGenericTypeDefinition)
								 .Where(x => typeof(T).IsAssignableFrom(x));

			if(excludeGenericBase != null) query = query.Where(x => !IsSubclassOfGeneric(x, excludeGenericBase));

			return query.ToList();
		}

		/// <summary>
		/// 取得所有標記特定 Attribute 且繼承指定類型的類別
		/// </summary>
		/// <typeparam name="TBase">基底類型</typeparam>
		/// <typeparam name="TAttribute">Attribute 類型</typeparam>
		/// <returns>符合條件的類別清單</returns>
		public static List<Type> GetTypesWithAttribute<TBase, TAttribute>() where TAttribute: Attribute
		{
			return AppDomain.CurrentDomain.GetAssemblies()
							.SelectMany(s => s.GetTypes())
							.Where(x => !x.IsAbstract)
							.Where(x => typeof(TBase).IsAssignableFrom(x))
							.Where(x => x.GetCustomAttributes(typeof(TAttribute), false).Length > 0)
							.ToList();
		}

		/// <summary>
		/// 檢查類型是否繼承自特定泛型基類
		/// </summary>
		/// <param name="type">要檢查的類型</param>
		/// <param name="genericBase">泛型基類定義</param>
		/// <returns>是否繼承自該泛型基類</returns>
		public static bool IsSubclassOfGeneric(Type type, Type genericBase)
		{
			var baseType = type.BaseType;

			while(baseType != null)
			{
				if(baseType.IsGenericType && baseType.GetGenericTypeDefinition() == genericBase) return true;

				baseType = baseType.BaseType;
			}

			return false;
		}
	}
}