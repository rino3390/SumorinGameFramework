#if ODIN_VALIDATOR
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using Sumorin.GameManagerBase;
	using UnityEditor;
	using UnityEngine;

	namespace Sumorin.ArchitectureValidator
	{
		/// <summary>
		///     DataScript 的 Id 必須存在且全專案唯一。檢查綁在 SODataBase 上，新增 DataScript 型別自動涵蓋
		/// </summary>
		public class DataScriptIdRule: IArchitectureRule
		{
			/// <inheritdoc />
			public IEnumerable<ArchitectureViolation> FindViolations()
			{
				var byId = new Dictionary<string, List<SODataBase>>();

				foreach(var data in DataScriptCache.All)
				{
					// Id 本身是否合法由 DataScriptIdValidator 檢查，這裡只管跨資產的唯一性
					if(!data.IsIdLegal()) continue;

					if(!byId.TryGetValue(data.Id, out var owners))
					{
						owners = new List<SODataBase>();
						byId[data.Id] = owners;
					}

					owners.Add(data);
				}

				foreach(var pair in byId)
				{
					if(pair.Value.Count <= 1) continue;

					// 每一份都回報一次，才能從 Validator 視窗逐一點過去修。
					// 不列出其他資產的名稱，訊息會長到看不完，點擊跳轉本來就找得到
					foreach(var duplicate in pair.Value)
					{
						// 撞號來源不同，修法也不同。跨型別加前綴就能區分，同型別加前綴仍然一樣，只能編號
						var sameType = pair.Value.Count(other => other.GetType() == duplicate.GetType()) > 1;

						yield return sameType ?
										 new ArchitectureViolation(Message(pair), duplicate, "加上編號", Rename(duplicate, duplicate.Id)) :
										 new ArchitectureViolation(Message(pair), duplicate, "加上類別前綴", Rename(duplicate, duplicate.IdPrefix + "_" + duplicate.Id));
					}
				}
			}

			private static string Message(KeyValuePair<string, List<SODataBase>> pair)
			{
				return $"Id «{pair.Key}» 與另外 {pair.Value.Count - 1} 份資產重複";
			}

			private static Action Rename(SODataBase data, string wanted)
			{
				return () =>
				{
					data.Id = Vacant(wanted);
					EditorUtility.SetDirty(data);
					AssetDatabase.SaveAssets();
				};
			}

			// 想要的 Id 被佔用時往後編號。已用 Id 是有限集合，迴圈必然終止
			private static string Vacant(string wanted)
			{
				var taken = new HashSet<string>(DataScriptCache.All.Select(data => data.Id));

				if(!taken.Contains(wanted)) return wanted;

				for(var suffix = 2;; suffix++)
				{
					var candidate = wanted + "_" + suffix;

					if(!taken.Contains(candidate)) return candidate;
				}
			}
		}

		/// <summary>
		///     打點時間必須與動畫片段上的 Animation Event 對齊
		/// </summary>
		public class AnimationHitAlignmentRule: IArchitectureRule
		{
			/// <inheritdoc />
			public IEnumerable<ArchitectureViolation> FindViolations()
			{
				foreach(var data in DataScriptCache.All)
				{
					// ReSharper disable once SuspiciousTypeConversion.Global
					if(data is not IAnimationHitConfig config) continue;
					if(config.Clip == null) continue;

					foreach(var message in Misalignments(config))
					{
						yield return new ArchitectureViolation(message, data);
					}
				}
			}

			private static IEnumerable<string> Misalignments(IAnimationHitConfig config)
			{
				var hitTimes = (config.HitTimes ?? System.Array.Empty<float>()).OrderBy(time => time).ToArray();
				var eventTimes = AnimationUtility.GetAnimationEvents(config.Clip).Select(e => e.time).OrderBy(time => time).ToArray();

				if(hitTimes.Length != eventTimes.Length)
				{
					yield return $"打點數量不一致，HitTimes 有 {hitTimes.Length} 個，{config.Clip.name} 的 Animation Event 有 {eventTimes.Length} 個";

					yield break;
				}

				// 容差取半格，動畫 event 時間本來就對齊到影格，比死磕浮點相等實際
				var tolerance = config.Clip.frameRate > 0 ? 0.5f / config.Clip.frameRate : 0.001f;

				for(var i = 0; i < hitTimes.Length; i++)
				{
					if(Mathf.Abs(hitTimes[i] - eventTimes[i]) <= tolerance) continue;

					yield return $"第 {i + 1} 個打點對不上，HitTimes 是 {hitTimes[i]:F3}s，Animation Event 是 {eventTimes[i]:F3}s（容差 {tolerance:F3}s）";
				}
			}
		}

		/// <summary>
		///     有 <see cref="DataEditorConfigAttribute" /> 的 DataScript 資產必須被對應的 DataSet 收錄
		/// </summary>
		/// <remarks>
		///     DataSet 是資產的執行期容器，沒被收錄的資產不會被打包，Domain 查不到那份配置。
		///     從 GameManager 建立的資產會自動加入，本規則抓的是複製、匯入、外部刪除等繞過該流程的情況。
		/// </remarks>
		public class DataSetMembershipRule: IArchitectureRule
		{
			/// <inheritdoc />
			public IEnumerable<ArchitectureViolation> FindViolations()
			{
				var collected = new HashSet<SODataBase>();
				var setOfType = new Dictionary<Type, ScriptableObject>();

				foreach(var dataSet in LoadDataSets())
				{
					var dataType = DataTypeOf(dataSet.GetType());

					if(dataType != null) setOfType[dataType] = dataSet;

					var index = 0;

					foreach(var entry in Entries(dataSet))
					{
						index++;

						if(entry == null)
						{
							yield return new ArchitectureViolation($"第 {index} 筆是空的", dataSet, "清掉空項目", RemoveEmptyEntries(dataSet));

							continue;
						}

						collected.Add(entry);
					}
				}

				var managed = DataScriptCache.All.Where(data => data.GetType().GetCustomAttribute<DataEditorConfigAttribute>() != null).ToList();

				foreach(var group in managed.GroupBy(data => data.GetType()))
				{
					// 整個型別都沒有 DataSet 時只報一次，否則每份資產都會各報一次
					if(!setOfType.TryGetValue(group.Key, out var dataSet))
					{
						yield return new ArchitectureViolation($"找不到 {group.Key.Name} 的 DataSet，{group.Count()} 份資料執行期載不到", group.First());

						continue;
					}

					foreach(var data in group.Where(data => !collected.Contains(data)))
					{
						// 指向 DataSet 而非缺漏的資產。要修的是集合，點過去才是該編輯的對象
						yield return new ArchitectureViolation($"{dataSet.name} 有遺漏 Data，需要修復否則執行期讀取不到", dataSet, $"加入 {data.name}", Add(dataSet, data));
					}
				}
			}

			// 直接改清單欄位而不呼叫 DataSet 的方法，因為 DataSet<T> 是泛型，方法反射多一層對不上的風險
			private static Action Add(ScriptableObject dataSet, SODataBase data)
			{
				return () =>
				{
					if(ListOf(dataSet) is not { } list || list.Contains(data)) return;

					list.Add(data);
					Save(dataSet);
				};
			}

			private static Action RemoveEmptyEntries(ScriptableObject dataSet)
			{
				return () =>
				{
					if(ListOf(dataSet) is not { } list) return;

					// 清單裡的 null 是資產在編輯器外被刪掉留下的空位。
					for(var i = list.Count - 1; i >= 0; i--)
					{
						if(list[i] == null) list.RemoveAt(i);
					}

					Save(dataSet);
				};
			}

			private static IList ListOf(ScriptableObject dataSet)
			{
				return dataSet.GetType().GetField(nameof(DataSet<SODataBase>.Datas))?.GetValue(dataSet) as IList;
			}

			private static void Save(ScriptableObject dataSet)
			{
				EditorUtility.SetDirty(dataSet);
				AssetDatabase.SaveAssets();
			}

			private static IEnumerable<ScriptableObject> LoadDataSets()
			{
				return AssetDatabase.FindAssets($"t:{nameof(ScriptableObject)}")
									.Select(AssetDatabase.GUIDToAssetPath)
									.Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>)
									.Where(asset => asset != null && DataTypeOf(asset.GetType()) != null);
			}

			private static IEnumerable<SODataBase> Entries(ScriptableObject dataSet)
			{
				var field = dataSet.GetType().GetField(nameof(DataSet<SODataBase>.Datas));

				if(field?.GetValue(dataSet) is not IEnumerable list) yield break;

				foreach(var item in list)
				{
					yield return item as SODataBase;
				}
			}

			private static Type DataTypeOf(Type type)
			{
				for(var current = type; current != null; current = current.BaseType)
				{
					if(current.IsGenericType && current.GetGenericTypeDefinition() == typeof(DataSet<>)) return current.GetGenericArguments()[0];
				}

				return null;
			}
		}

		/// <summary>
		///     全專案 DataScript 資產的快取，資產變更時自動失效。
		///     Id 完整性與打點對齊共用同一份，只付一次載入成本
		/// </summary>
		public class DataScriptCache: AssetPostprocessor
		{
			private static SODataBase[] cached;

			/// <summary>
			///     全專案的 DataScript 資產
			/// </summary>
			public static IReadOnlyList<SODataBase> All
			{
				get
				{
					cached ??= Load();
					return cached;
				}
			}

			private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
			{
				cached = null;
			}

			private static SODataBase[] Load()
			{
				return AssetDatabase.FindAssets($"t:{nameof(SODataBase)}")
									.Select(AssetDatabase.GUIDToAssetPath)
									.Select(AssetDatabase.LoadAssetAtPath<SODataBase>)
									.Where(data => data != null)
									.ToArray();
			}
		}
	}
#endif