#if ODIN_VALIDATOR
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using R3;
	using Sumorin.DDDCore;
	using VContainer.Unity;

	namespace Sumorin.ArchitectureValidator
	{
		/// <summary>
		///     Flow 是葉子，全專案不得有人把它當相依取得。Flow 注入其他 Flow 亦由本條命中
		/// </summary>
		public class FlowMustNotBeInjectedRule: IArchitectureRule
		{
			/// <inheritdoc />
			public IEnumerable<ArchitectureViolation> FindViolations()
			{
				// Installer 不豁免。它的豁免只到「引用 Flow assembly 並在 Install 裡註冊」為止，
				// 綁定不經過注入點所以本來就掃不到。真的寫 [Inject] 取得 Flow，那就是容器把 Flow 交給別人了
				foreach(var type in SumorinArchitecture.Types)
				{
					foreach(var (member, dependency) in SumorinArchitecture.InjectedDependencies(type))
					{
						if(SumorinArchitecture.LayerOf(dependency) != SumorinLayer.Flow) continue;

						yield return new ArchitectureViolation(
							$"{type.FullName}.{member} 注入了 Flow «{dependency.Name}»。Flow 是葉子，全專案不得有人把它當相依取得",
							SumorinArchitecture.ScriptOf(type),
							0,
							member
						);
					}
				}
			}
		}

		/// <summary>
		///     QueryService 禁止出現事件流成員，只有精確的 ReadOnlyReactiveProperty&lt;T&gt; 屬於值面白名單
		/// </summary>
		public class QueryServiceMustNotExposeEventStreamRule: IArchitectureRule
		{
			/// <inheritdoc />
			public IEnumerable<ArchitectureViolation> FindViolations()
			{
				foreach(var type in SumorinArchitecture.Types)
				{
					if(SumorinArchitecture.LayerOf(type) != SumorinLayer.DomainQuery) continue;

					foreach(var (member, memberType) in PublicMemberTypes(type))
					{
						if(!IsEventStream(memberType)) continue;

						// 白名單是泛型定義精確相等，不是可指派：ReactiveProperty<T> 繼承自它且可寫，用 IsAssignableFrom 會誤放行
						if(SumorinArchitecture.IsGenericDefinition(memberType, typeof(ReadOnlyReactiveProperty<>))) continue;

						yield return new ArchitectureViolation(
							$"{type.FullName}.{member} 的型別 «{memberType.Name}» 是事件流。QueryService 只能暴露 ReadOnlyReactiveProperty<T> 值面，事件一律走 DomainEvent",
							SumorinArchitecture.ScriptOf(type),
							0,
							member
						);
					}
				}
			}

			private static IEnumerable<(string member, Type memberType)> PublicMemberTypes(Type type)
			{
				const BindingFlags Public = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

				foreach(var property in type.GetProperties(Public))
				{
					yield return (property.Name, property.PropertyType);
				}

				foreach(var field in type.GetFields(Public))
				{
					yield return (field.Name, field.FieldType);
				}

				foreach(var method in type.GetMethods(Public))
				{
					if(method.IsSpecialName) continue;

					yield return (method.Name, method.ReturnType);
				}
			}

			private static bool IsEventStream(Type type)
			{
				if(type == null || type == typeof(void)) return false;

				// R3 的 Observable<T> 是類別不是介面，要沿基底類別鏈找；System.IObservable<T> 仍是介面，另外掃一次
				for(var current = type; current != null; current = current.BaseType)
				{
					if(SumorinArchitecture.IsGenericDefinition(current, typeof(Observable<>))) return true;
				}

				return type.GetInterfaces().Any(i => SumorinArchitecture.IsGenericDefinition(i, typeof(IObservable<>)));
			}
		}

		/// <summary>
		///     Controller 不訂閱 DomainEvent，事實只由 Flow 接手
		/// </summary>
		public class ControllerMustNotSubscribeRule: IArchitectureRule
		{
			/// <inheritdoc />
			public IEnumerable<ArchitectureViolation> FindViolations()
			{
				foreach(var type in SumorinArchitecture.Types)
				{
					if(!SumorinArchitecture.IsController(type)) continue;

					foreach(var (member, dependency) in SumorinArchitecture.InjectedDependencies(type))
					{
						if(dependency != typeof(ISubscriber) && dependency != typeof(IEventBus)) continue;

						yield return new ArchitectureViolation(
							$"{type.FullName}.{member} 注入了 «{dependency.Name}»。Controller 只發事實不訂閱事實，訂閱一律由 Flow 負責",
							SumorinArchitecture.ScriptOf(type),
							0,
							member
						);
					}
				}
			}
		}

		/// <summary>
		///     容器生命週期回呼只給 Flow，Controller 與 Presenter 實作 IInitializable / ITickable 即違規
		/// </summary>
		public class LifecycleCallbackOnlyForFlowRule: IArchitectureRule
		{
			/// <inheritdoc />
			public IEnumerable<ArchitectureViolation> FindViolations()
			{
				foreach(var type in SumorinArchitecture.Types)
				{
					if(type.IsInterface || type.IsAbstract) continue;
					if(SumorinArchitecture.LayerOf(type) == SumorinLayer.Flow) continue;

					// IDisposable 各層皆合法，只做釋放不改狀態，不列入本條
					var callbacks = new List<string>();
					if(typeof(IInitializable).IsAssignableFrom(type)) callbacks.Add(nameof(IInitializable));
					if(typeof(ITickable).IsAssignableFrom(type)) callbacks.Add(nameof(ITickable));

					if(callbacks.Count == 0) continue;

					yield return new ArchitectureViolation(
						$"{type.FullName} 實作了 {string.Join(" / ", callbacks)}。容器生命週期回呼只給 Flow，初始化與時間推進要做成命令方法交給 BootstrapFlow / TickFlow 呼叫",
						SumorinArchitecture.ScriptOf(type),
						0,
						type.Name
					);
				}
			}
		}

		/// <summary>
		///     View 與 Presenter 都不得訂閱 DomainEvent。
		///     Presenter 更嚴，它連值面都不訂閱，只在需要快照時同步讀 .Value
		/// </summary>
		public class ViewAndPresenterMustNotSubscribeRule: IArchitectureRule
		{
			/// <inheritdoc />
			public IEnumerable<ArchitectureViolation> FindViolations()
			{
				foreach(var type in SumorinArchitecture.Types)
				{
					var layer = SumorinArchitecture.LayerOf(type);
					if(layer != SumorinLayer.View && layer != SumorinLayer.Presenter) continue;

					foreach(var (member, dependency) in SumorinArchitecture.InjectedDependencies(type))
					{
						if(dependency != typeof(ISubscriber) && dependency != typeof(IEventBus)) continue;

						yield return new ArchitectureViolation(
							$"{type.FullName}.{member} 注入了 «{dependency.Name}»。{ReasonFor(layer)}",
							SumorinArchitecture.ScriptOf(type),
							0,
							member
						);
					}
				}

				foreach(var file in SumorinArchitecture.SourceFilesOf(SumorinLayer.View, SumorinLayer.Presenter))
				{
					foreach(var (lineNumber, line) in SumorinArchitecture.CodeLines(file))
					{
						// 只抓泛型呼叫形式，那是 EventBus 的簽名。R3 的 observable.Subscribe(...) 是非泛型呼叫，
						// View 訂閱 I{Domain}ValueService 的值面成員正是它的日常工作，不在此列
						if(line.IndexOf("Subscribe<", StringComparison.Ordinal) < 0 && line.IndexOf("SubscribeAsync<", StringComparison.Ordinal) < 0) continue;

						yield return new ArchitectureViolation(
							$"出現 Subscribe<T> 呼叫。{ReasonFor(file.Layer)}",
							SumorinArchitecture.AssetAt(file.AssetPath),
							lineNumber
						);
					}
				}
			}

			private static string ReasonFor(SumorinLayer layer)
			{
				return layer == SumorinLayer.Presenter ?
						   "Presenter 零訂閱，它只被 Flow 呼叫，需要當下數值就同步讀 I{Domain}ValueService 的 .Value" :
						   "View 不得訂閱 DomainEvent，值變化請訂閱 I{Domain}ValueService 的值面成員";
			}
		}

		/// <summary>
		///     Controller 只碰自己 Domain 的 Repository，跨 Domain 讀取一律呼叫下層 Controller
		/// </summary>
		public class ControllerMustNotInjectForeignRepositoryRule: IArchitectureRule
		{
			/// <inheritdoc />
			public IEnumerable<ArchitectureViolation> FindViolations()
			{
				foreach(var type in SumorinArchitecture.Types)
				{
					if(!SumorinArchitecture.IsController(type)) continue;

					var ownDomain = SumorinArchitecture.DomainOf(type);

					foreach(var (member, dependency) in SumorinArchitecture.InjectedDependencies(type))
					{
						var repository = SumorinArchitecture.FindClosedGeneric(dependency, typeof(IRepository<>));
						if(repository == null) continue;

						var entityDomain = SumorinArchitecture.DomainOf(repository.GetGenericArguments()[0]);
						if(entityDomain == ownDomain) continue;

						yield return new ArchitectureViolation(
							$"{type.FullName}.{member} 注入了 {entityDomain} Domain 的 Repository «{dependency.Name}»。跨 Domain 讀取要呼叫下層 Controller 的查詢方法",
							SumorinArchitecture.ScriptOf(type),
							0,
							member
						);
					}
				}
			}
		}
	}
#endif