#if ODIN_VALIDATOR
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;
	using Sumorin.Presentation;
	using UnityEditor;
	using UnityEngine;

	namespace Sumorin.ArchitectureValidator
	{
		/// <summary>
		///     ActionHandler 的序列化欄位只掛自己面板的 UI 元件，持有 View 型別即違規
		/// </summary>
		public class ActionHandlerMustNotHoldViewRule: IArchitectureRule
		{
			/// <inheritdoc />
			public IEnumerable<ArchitectureViolation> FindViolations()
			{
				foreach(var type in SumorinArchitecture.Types)
				{
					if(SumorinArchitecture.LayerOf(type) != SumorinLayer.ActionHandler) continue;

					foreach(var field in SerializedFields(type))
					{
						var fieldType = ElementTypeOf(field.FieldType);
						if(SumorinArchitecture.LayerOf(fieldType) != SumorinLayer.View) continue;

						yield return new ArchitectureViolation(
							$"{type.FullName}.{field.Name} 序列化了 View 型別 «{fieldType.Name}»。ActionHandler 只認識自己面板的 UI 元件，意圖一律推給 Flow",
							SumorinArchitecture.ScriptOf(type),
							0,
							field.Name
						);
					}
				}
			}

			// 走到 MonoBehaviour 為止，中間的自訂基底（例如 SerializedMonoBehaviour 的子類）也要掃
			private static IEnumerable<FieldInfo> SerializedFields(Type type)
			{
				const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

				for(var current = type; current != null && current != typeof(MonoBehaviour); current = current.BaseType)
				{
					foreach(var field in current.GetFields(All))
					{
						if(field.IsDefined(typeof(NonSerializedAttribute), false)) continue;

						if(field.IsPublic || field.IsDefined(typeof(SerializeField), false) || IsOdinSerialized(field))
						{
							yield return field;
						}
					}
				}
			}

			// 比對名稱而不引用 Sirenix.Serialization，讓本 assembly 不因 Odin 的 assembly 佈局變動而斷掉
			private static bool IsOdinSerialized(FieldInfo field)
			{
				return field.CustomAttributes.Any(attribute => attribute.AttributeType.Name == "OdinSerializeAttribute");
			}

			// 陣列與 List 也算持有，拆到元素型別再判層級
			private static Type ElementTypeOf(Type type)
			{
				if(type.IsArray) return type.GetElementType();

				if(type.IsGenericType && type.GetGenericArguments().Length == 1)
				{
					return type.GetGenericArguments()[0];
				}

				return type;
			}
		}

		/// <summary>
		///     prefab 根物件上的 View 一律實作 IBindableView，讓 IViewProvider 在回收前清得掉訂閱
		/// </summary>
		public class ViewMustImplementBindableViewRule: IArchitectureRule
		{
			/// <inheritdoc />
			public IEnumerable<ArchitectureViolation> FindViolations()
			{
				var suspects = SumorinArchitecture.Types.Where(type => !type.IsAbstract && !type.IsInterface)
												  .Where(type => SumorinArchitecture.LayerOf(type) == SumorinLayer.View)
												  .Where(type => typeof(MonoBehaviour).IsAssignableFrom(type))
												  .Where(type => !typeof(IBindableView).IsAssignableFrom(type))
												  .ToHashSet();

				// 先問「有沒有可能違規的型別」再開 prefab。載入全專案 prefab 是整組規則裡最重的操作，
				// 合規專案在這裡就直接返回，一個資產都不用碰
				if(suspects.Count == 0) yield break;

				// 只看 prefab 根物件的元件，那才是會被 IViewProvider 具現化的對象。
				// 掛在別人身上、隨宿主一起銷毀的純值監聽子元件不實作 IBindableView 是合法寫法，不能一起擋
				foreach(var guid in AssetDatabase.FindAssets("t:Prefab"))
				{
					var path = AssetDatabase.GUIDToAssetPath(guid);
					var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
					if(prefab == null) continue;

					foreach(var component in prefab.GetComponents<MonoBehaviour>())
					{
						if(component == null) continue;
						if(!suspects.Contains(component.GetType())) continue;

						yield return new ArchitectureViolation(
							$"{path} 的根物件掛著 View «{component.GetType().Name}» 卻沒有實作 IBindableView。進 ViewRegistry 的 View 必須能被 Unbind，Unbind 內用 CompositeDisposable.Clear()",
							prefab
						);
					}
				}
			}
		}
	}
#endif