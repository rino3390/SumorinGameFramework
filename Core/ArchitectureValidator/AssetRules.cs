#if ODIN_VALIDATOR
	using System.Collections.Generic;
	using System.Linq;
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
					if(string.IsNullOrWhiteSpace(data.Id))
					{
						yield return new ArchitectureViolation($"{data.name} 的 Id 是空的。DataScript 的 Id 是 Domain 引用它的唯一依據", data);

						continue;
					}

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

					var names = string.Join("、", pair.Value.Select(data => data.name));

					// 每一份都回報一次，才能從 Validator 視窗逐一點過去修
					foreach(var duplicate in pair.Value)
					{
						yield return new ArchitectureViolation($"Id «{pair.Key}» 重複於 {pair.Value.Count} 份資產：{names}", duplicate);
					}
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
						yield return new ArchitectureViolation($"{data.name}：{message}", data);
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