#if ODIN_VALIDATOR
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;
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
				var flowFiles = SumorinArchitecture.SourceFilesOf(SumorinLayer.Flow).ToList();

				// 專案還沒有 Flow 層時整條跳過，那代表接線根本還沒開始，不是漏接
				if(flowFiles.Count == 0) yield break;

				foreach(var domainEvent in DomainEvents())
				{
					var pattern = new Regex($@"(?<![A-Za-z0-9_])Subscribe(Async)?\s*<\s*{Regex.Escape(domainEvent.Name)}\s*>");
					var subscriptions = flowFiles.SelectMany(SumorinArchitecture.CodeLines).Count(entry => pattern.IsMatch(entry.line));

					// 條件是恰好 1，不是至多 1
					if(subscriptions == 1) continue;

					var reason = subscriptions == 0 ? "沒有任何 Flow 訂閱，這是死事實：不是漏接就是根本不該發" : $"有 {subscriptions} 個 Flow 訂閱，流程散落";

					yield return new ArchitectureViolation(
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
		///     View 只能透過 I{Domain}ValueService 取值，出現具體 QueryService 型別即違規
		/// </summary>
		public class ViewMustNotUseQueryServiceRule: IArchitectureRule
		{
			/// <inheritdoc />
			public IEnumerable<ArchitectureViolation> FindViolations()
			{
				// 型別名直接取自反射結果，新增 Domain 自動涵蓋，不靠命名猜測。
				// 介面也要收：Zenject 綁的是 I{Domain}QueryService，View 寫錯時最可能注入的就是它
				var queryServices = SumorinArchitecture.Types.Where(type => SumorinArchitecture.LayerOf(type) == SumorinLayer.DomainQuery)
													   .Where(type => type.Name.EndsWith("QueryService", StringComparison.Ordinal))
													   .Select(type => type.Name)
													   .Distinct()
													   .ToList();

				if(queryServices.Count == 0) yield break;

				var pattern = new Regex($@"(?<![A-Za-z0-9_])({string.Join("|", queryServices.Select(Regex.Escape))})(?![A-Za-z0-9_])");

				foreach(var file in SumorinArchitecture.SourceFilesOf(SumorinLayer.View))
				{
					foreach(var (lineNumber, line) in SumorinArchitecture.CodeLines(file))
					{
						var match = pattern.Match(line);
						if(!match.Success) continue;

						yield return new ArchitectureViolation(
							$"出現 QueryService «{match.Value}»。View 只能依賴 I{{Domain}}ValueService 的值面成員",
							SumorinArchitecture.AssetAt(file.AssetPath),
							lineNumber
						);
					}
				}
			}
		}
	}
#endif