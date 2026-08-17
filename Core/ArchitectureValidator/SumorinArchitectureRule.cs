#if ODIN_VALIDATOR
	using System.Collections;
	using System.Collections.Generic;
	using Sirenix.OdinInspector;
	using Sirenix.OdinInspector.Editor.Validation;
	using Sumorin.ArchitectureValidator;
	using UnityEditor;

	[assembly: RegisterValidationRule(typeof(SumorinArchitectureRule), Name = "Sumorin 架構規則", Description = "全專案掃描一次，涵蓋程式結構、原始碼、場景佈線與資產。個別條目可在下方開關")]

	namespace Sumorin.ArchitectureValidator
	{
		/// <summary>
		///     一筆架構違規
		/// </summary>
		public readonly struct ArchitectureViolation
		{
			/// <summary>違規描述，需指出違規者與被違反的規則</summary>
			public readonly string Message;

			/// <summary>違規來源資產，Validator 視窗以此提供點擊跳轉，路徑也由它推導</summary>
			public readonly UnityEngine.Object Source;

			/// <summary>原始碼行號，0 表示改用 <see cref="Symbol" /> 回推</summary>
			public readonly int Line;

			/// <summary>要定位的成員或型別名，反射類違規靠它從原始碼回推行號</summary>
			public readonly string Symbol;

			/// <summary>
			///     建立一筆架構違規
			/// </summary>
			/// <param name="message">違規描述</param>
			/// <param name="source">違規來源資產，可為 null</param>
			/// <param name="line">原始碼行號，沒有就留 0</param>
			/// <param name="symbol">要定位的成員或型別名，用於回推行號</param>
			public ArchitectureViolation(string message, UnityEngine.Object source, int line = 0, string symbol = null)
			{
				Message = message;
				Source = source;
				Line = line;
				Symbol = symbol;
			}

		}

		/// <summary>
		///     一條全專案掃描的架構規則
		/// </summary>
		public interface IArchitectureRule
		{
			/// <summary>
			///     找出本條規則的所有違規
			/// </summary>
			/// <returns>違規清單，全部合規時為空</returns>
			IEnumerable<ArchitectureViolation> FindViolations();
		}

		/// <summary>
		///     Sumorin 架構的全專案檢查。
		///     十二條規則共用一次掃描與一組快取，在 Validator 視窗只佔一列，個別條目由下方開關控制
		/// </summary>
		public class SumorinArchitectureRule: GlobalValidator
		{
			/// <summary>Flow 是葉子，全專案不得有人把它當相依取得</summary>
			[ToggleLeft]
			[BoxGroup("程式結構")]
			[LabelText("Flow 無人注入")]
			public bool FlowMustNotBeInjected = true;

			/// <summary>QueryService 只暴露 IReadOnlyReactiveProperty&lt;T&gt; 值面</summary>
			[ToggleLeft]
			[BoxGroup("程式結構")]
			[LabelText("QueryService 無事件流成員")]
			public bool QueryServiceMustNotExposeEventStream = true;

			/// <summary>Controller 只發事實，接手事實是 Flow 的工作</summary>
			[ToggleLeft]
			[BoxGroup("程式結構")]
			[LabelText("Controller 不訂閱 DomainEvent")]
			public bool ControllerMustNotSubscribe = true;

			/// <summary>IInitializable 與 ITickable 只有 Flow 能實作</summary>
			[ToggleLeft]
			[BoxGroup("程式結構")]
			[LabelText("生命週期回呼只給 Flow")]
			public bool LifecycleCallbackOnlyForFlow = true;

			/// <summary>值變化走 I{Domain}ValueService，事實由 Flow 接手</summary>
			[ToggleLeft]
			[BoxGroup("程式結構")]
			[LabelText("View 與 Presenter 不訂閱 DomainEvent")]
			public bool ViewAndPresenterMustNotSubscribe = true;

			/// <summary>跨 Domain 讀取要呼叫下層 Controller 的查詢方法</summary>
			[ToggleLeft]
			[BoxGroup("程式結構")]
			[LabelText("Controller 不注入他 Domain 的 Repository")]
			public bool ControllerMustNotInjectForeignRepository = true;

			/// <summary>零訂閱是死事實，多訂閱是流程散落</summary>
			[ToggleLeft]
			[BoxGroup("原始碼掃描")]
			[LabelText("一 DomainEvent 恰一 Flow")]
			public bool DomainEventMustHaveExactlyOneFlow = true;

			/// <summary>View 只能依賴 I{Domain}ValueService</summary>
			[ToggleLeft]
			[BoxGroup("原始碼掃描")]
			[LabelText("View 不繞過值面介面")]
			public bool ViewMustNotUseQueryService = true;

			/// <summary>序列化欄位只掛自己面板的 UI 元件</summary>
			[ToggleLeft]
			[BoxGroup("場景佈線")]
			[LabelText("ActionHandler 不持有 View")]
			public bool ActionHandlerMustNotHoldView = true;

			/// <summary>進 ViewRegistry 的 View 必須能被 Unbind</summary>
			[ToggleLeft]
			[BoxGroup("場景佈線")]
			[LabelText("prefab 上的 View 實作 IBindableView")]
			public bool ViewMustImplementBindableView = true;

			/// <summary>DataScript 的 Id 必須存在且全專案唯一</summary>
			[ToggleLeft]
			[BoxGroup("資產")]
			[LabelText("DataScript Id 完整性")]
			public bool DataScriptId = true;

			/// <summary>HitTimes 必須與動畫片段上的 Animation Event 時間一致</summary>
			[ToggleLeft]
			[BoxGroup("資產")]
			[LabelText("打點對齊")]
			public bool AnimationHitAlignment = true;

			/// <inheritdoc />
			public override IEnumerable RunValidation(ValidationResult result)
			{
				// 掃描結果由 SumorinArchitecture 與 DataScriptCache 快取，十二條規則跑下來只付一次反射、讀檔與載入資產的代價
				Collect(result, FlowMustNotBeInjected, new FlowMustNotBeInjectedRule());
				Collect(result, QueryServiceMustNotExposeEventStream, new QueryServiceMustNotExposeEventStreamRule());
				Collect(result, ControllerMustNotSubscribe, new ControllerMustNotSubscribeRule());
				Collect(result, LifecycleCallbackOnlyForFlow, new LifecycleCallbackOnlyForFlowRule());
				Collect(result, ViewAndPresenterMustNotSubscribe, new ViewAndPresenterMustNotSubscribeRule());
				Collect(result, ControllerMustNotInjectForeignRepository, new ControllerMustNotInjectForeignRepositoryRule());
				Collect(result, DomainEventMustHaveExactlyOneFlow, new DomainEventMustHaveExactlyOneFlowRule());
				Collect(result, ViewMustNotUseQueryService, new ViewMustNotUseQueryServiceRule());
				Collect(result, ActionHandlerMustNotHoldView, new ActionHandlerMustNotHoldViewRule());
				Collect(result, ViewMustImplementBindableView, new ViewMustImplementBindableViewRule());
				Collect(result, DataScriptId, new DataScriptIdRule());
				Collect(result, AnimationHitAlignment, new AnimationHitAlignmentRule());

				// 回傳值是給長時間驗證分幀用的，yield 一次就交還一幀控制權給 editor，
				// 不是「提交一筆結果」，多筆違規靠 AddError 累積
				return null;
			}

			private static void Collect(ValidationResult result, bool enabled, IArchitectureRule rule)
			{
				if(!enabled) return;

				foreach(var violation in rule.FindViolations())
				{
					ref var item = ref result.AddError(WithLocation(violation));

					if(violation.Source != null)
					{
						item.SetSelectionObject(violation.Source);
					}
				}
			}

			// 訊息一律附上路徑。點擊跳轉解決不了「我想直接搜這個檔」與「把回報貼給別人看」這兩件事
			private static string WithLocation(ArchitectureViolation violation)
			{
				if(violation.Source == null) return violation.Message;

				var path = AssetDatabase.GetAssetPath(violation.Source);
				if(string.IsNullOrEmpty(path)) return violation.Message;

				var line = violation.Line > 0 ? violation.Line : SumorinArchitecture.LineOfSymbol(path, violation.Symbol);

				return line > 0 ? $"{violation.Message}\n{path}:{line}" : $"{violation.Message}\n{path}";
			}
		}
	}
#endif