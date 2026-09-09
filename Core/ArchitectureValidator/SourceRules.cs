#if ODIN_VALIDATOR
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Sumorin.DDDCore;

	namespace Sumorin.ArchitectureValidator
	{
		/// <summary>
		///     一個 DomainEvent 恰好一個 Flow 訂閱。零訂閱是死事實，多訂閱是流程散落，兩種都要修
		/// </summary>
		public class DomainEventMustHaveExactlyOneFlowRule: IArchitectureRule
		{
			/// <inheritdoc />
			public IEnumerable<ArchitectureViolation> FindViolations()
			{
				var flowAssemblies = SumorinArchitecture.Types.Where(type => SumorinArchitecture.LayerOf(type) == SumorinLayer.Flow)
														.Select(type => type.Assembly.Location)
														.Distinct()
														.ToList();

				// 專案還沒有 Flow 層時整條跳過，那代表接線根本還沒開始，不是漏接
				if(flowAssemblies.Count == 0) yield break;

				// 從 IL 數呼叫點：靠型別推導的 Subscribe(handler) 與藏在 lambda 裡的訂閱都算得到，掃原始碼抓不到那兩種寫法
				var subscriptions = flowAssemblies.SelectMany(IlScanner.SubscribeCalls).ToLookup(call => call.eventType);

				foreach(var domainEvent in DomainEvents())
				{
					var count = subscriptions[domainEvent.FullName].Count();

					// 條件是恰好 1，不是至多 1
					if(count == 1) continue;

					var reason = count == 0 ? "沒有任何 Flow 訂閱，這是死事實：不是漏接就是根本不該發" : $"有 {count} 個 Flow 訂閱，流程散落";

					yield return new(
						$"DomainEvent «{domainEvent.FullName}» {reason}。一個事實恰好一個 Flow 訂閱",
						SumorinArchitecture.ScriptOf(domainEvent),
						0,
						domainEvent.Name
					);
				}
			}

			private static IEnumerable<Type> DomainEvents()
			{
				return SumorinArchitecture.Types.Where(type => !type.IsInterface && !type.IsAbstract && typeof(IEvent).IsAssignableFrom(type))
										  .Where(type => !SumorinArchitecture.IsModuleTemplate(type));
			}
		}

		/// <summary>
		///     View 只能透過 I{Domain}ValueService 取值，引用到具體 QueryService 型別即違規
		/// </summary>
		public class ViewMustNotUseQueryServiceRule: IArchitectureRule
		{
			/// <inheritdoc />
			public IEnumerable<ArchitectureViolation> FindViolations()
			{
				// 型別直接取自反射結果，新增 Domain 自動涵蓋，不靠命名猜測。
				// 介面也要收：容器註冊的是 I{Domain}QueryService，View 寫錯時最可能注入的就是它
				var queryServices = SumorinArchitecture.Types.Where(type => SumorinArchitecture.LayerOf(type) == SumorinLayer.DomainQuery)
													   .Where(type => type.Name.EndsWith("QueryService", StringComparison.Ordinal))
													   .Select(type => type.FullName)
													   .ToHashSet();

				if(queryServices.Count == 0) yield break;

				foreach(var view in SumorinArchitecture.Types.Where(type => SumorinArchitecture.LayerOf(type) == SumorinLayer.View))
				{
					// ponytail: 每個 View 各讀一次 assembly，View 數量到三位數再改成按 assembly 分組
					foreach(var (member, typeFullName) in IlScanner.ReferencedTypes(view.Assembly.Location, view.FullName))
					{
						if(!queryServices.Contains(typeFullName)) continue;

						yield return new(
							$"«{view.Name}.{member}» 出現 QueryService «{typeFullName[(typeFullName.LastIndexOf('.') + 1)..]}»。View 只能依賴 I{{Domain}}ValueService 的值面成員",
							SumorinArchitecture.ScriptOf(view),
							0,
							member
						);
					}
				}
			}
		}
	}
#endif