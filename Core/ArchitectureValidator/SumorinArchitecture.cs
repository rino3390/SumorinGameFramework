#if ODIN_VALIDATOR
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Text.RegularExpressions;
	using UnityEditor;
	using UnityEditor.Compilation;
	using UnityEngine;
	using Zenject;

	namespace Sumorin.ArchitectureValidator
	{
		/// <summary>
		///     Sumorin 架構層級，依 assembly 名稱推導
		/// </summary>
		public enum SumorinLayer
		{
			/// <summary>不屬於架構管轄範圍（第三方、Editor、測試）</summary>
			Outside,

			/// <summary>Flow：編排層，全專案無人注入</summary>
			Flow,

			/// <summary>Presenter：表演入口與 View 管理</summary>
			Presenter,

			/// <summary>View：畫面呈現，MonoBehaviour</summary>
			View,

			/// <summary>ActionHandler：使用者意圖推送，MonoBehaviour</summary>
			ActionHandler,

			/// <summary>Installer：組裝根，引用全部 assembly 不算違規</summary>
			Installer,

			/// <summary>Domain Core：Model、Controller、Repository</summary>
			DomainCore,

			/// <summary>Domain Command：CommandService</summary>
			DomainCommand,

			/// <summary>Domain Query：QueryService 與值面／查詢面介面</summary>
			DomainQuery,

			/// <summary>Domain Contract：DomainEvent、Info、I{X}Config</summary>
			DomainContract,

			/// <summary>Domain DataScript：ScriptableObject 配置</summary>
			DomainDataScript,

			/// <summary>Utility：純工具，不參與架構規則</summary>
			Utility
		}

		/// <summary>
		///     架構掃描的共用查詢面，快取全專案型別與原始碼，供各條規則重複取用
		/// </summary>
		public static class SumorinArchitecture
		{
			private static readonly Regex DeclarationPattern = new(@"\b(?:class|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)");

			private static readonly Regex SymbolTailPattern = new(@"([A-Za-z_][A-Za-z0-9_]*)\)?$");

			private static HashSet<string> managedAssemblies;
			private static Type[] cachedTypes;
			private static List<SourceFile> cachedSourceFiles;
			private static Dictionary<string, string> declarationPaths;
			private static Dictionary<string, SourceFile> sourceByPath;
			private static readonly Dictionary<Type, UnityEngine.Object> CachedScripts = new();

			private static IEnumerable<SourceFile> SourceFiles
			{
				get
				{
					cachedSourceFiles ??= CollectSourceFiles();
					return cachedSourceFiles;
				}
			}

			/// <summary>
			///     全專案受架構管轄的具體型別與介面，排除第三方、Editor 與測試 assembly
			/// </summary>
			public static IReadOnlyList<Type> Types
			{
				get
				{
					cachedTypes ??= CollectTypes();
					return cachedTypes;
				}
			}

			/// <summary>
			///     捨棄快取，讓下一次驗證重新掃描（編譯後 Odin 會重建 validator，正常情況不需手動呼叫）
			/// </summary>
			public static void ClearCache()
			{
				managedAssemblies = null;
				cachedTypes = null;
				cachedSourceFiles = null;
				declarationPaths = null;
				sourceByPath = null;
				CachedScripts.Clear();
			}

			/// <summary>
			///     依 assembly 名稱推導型別所屬的架構層級
			/// </summary>
			/// <param name="type">要判定的型別</param>
			/// <returns>架構層級，不受管轄時為 <see cref="SumorinLayer.Outside" /></returns>
			public static SumorinLayer LayerOf(Type type)
			{
				if(type == null) return SumorinLayer.Outside;

				return LayerOfAssembly(type.Assembly.GetName().Name);
			}

			/// <summary>
			///     依 assembly 名稱推導架構層級
			/// </summary>
			/// <param name="assemblyName">assembly 名稱，如 Flow、Sumorin.Buff.Query</param>
			/// <returns>架構層級</returns>
			public static SumorinLayer LayerOfAssembly(string assemblyName)
			{
				if(!IsManaged(assemblyName)) return SumorinLayer.Outside;

				if(MatchesSegment(assemblyName, "Flow")) return SumorinLayer.Flow;
				if(MatchesSegment(assemblyName, "Presenter")) return SumorinLayer.Presenter;
				if(MatchesSegment(assemblyName, "View")) return SumorinLayer.View;
				if(MatchesSegment(assemblyName, "ActionHandler")) return SumorinLayer.ActionHandler;
				if(MatchesSegment(assemblyName, "Installer")) return SumorinLayer.Installer;
				if(MatchesSegment(assemblyName, "Command")) return SumorinLayer.DomainCommand;
				if(MatchesSegment(assemblyName, "Query")) return SumorinLayer.DomainQuery;
				if(MatchesSegment(assemblyName, "Contract")) return SumorinLayer.DomainContract;

				// 標準結構是 {Domain}.DataScript 與 Utility，模組範本用的是 {Domain}.Data 與 Sumorin.SumorinUtility。
				// 兩種寫法都收，否則這兩個層級的判定在框架自己身上永遠命中不到
				if(MatchesSegment(assemblyName, "DataScript") || MatchesSegment(assemblyName, "Data")) return SumorinLayer.DomainDataScript;
				if(MatchesSegment(assemblyName, "Utility") || MatchesSegment(assemblyName, "SumorinUtility")) return SumorinLayer.Utility;

				return SumorinLayer.DomainCore;
			}

			/// <summary>
			///     取得型別所屬的 Domain 名稱，用 namespace 最後一段判定
			/// </summary>
			/// <param name="type">要判定的型別</param>
			/// <returns>Domain 名稱，無 namespace 時為空字串</returns>
			public static string DomainOf(Type type)
			{
				var ns = type?.Namespace;
				if(string.IsNullOrEmpty(ns)) return string.Empty;

				var parts = ns.Split('.');

				// 遊戲側是 Domains.{X}，框架模組是 Sumorin.{X}，兩者都取根之後那一段。
				// 不能取最後一段，Domains.Buff.Controllers 那類子命名空間會被誤判成不同 Domain
				var domainsIndex = Array.IndexOf(parts, "Domains");
				if(domainsIndex >= 0 && domainsIndex + 1 < parts.Length) return parts[domainsIndex + 1];

				return parts.Length >= 2 ? parts[1] : parts[0];
			}

			/// <summary>
			///     判斷型別是否為 Domain 的 Controller
			/// </summary>
			/// <param name="type">要判定的型別</param>
			/// <returns>是具體 Controller 則為 true</returns>
			public static bool IsController(Type type)
			{
				if(type == null || type.IsInterface) return false;
				if(LayerOf(type) != SumorinLayer.DomainCore) return false;

				return type.Name.EndsWith("Controller", StringComparison.Ordinal);
			}

			/// <summary>
			///     走訪原始碼中的實質程式行，略過空行與單行註解
			/// </summary>
			/// <param name="file">原始碼檔案</param>
			/// <returns>行號（自 1 起算）與行內容</returns>
			public static IEnumerable<(int lineNumber, string line)> CodeLines(SourceFile file)
			{
				var inBlockComment = false;

				for(var i = 0; i < file.Lines.Length; i++)
				{
					var code = StripComments(file.Lines[i], ref inBlockComment);
					if(code.Trim().Length == 0) continue;

					yield return (i + 1, code);
				}
			}

			/// <summary>
			///     判斷型別是否來自尚未安裝的模組範本。
			///     範本的接線發生在安裝它的遊戲專案裡，不能拿本專案的缺席去判它有問題
			/// </summary>
			/// <param name="type">要判定的型別</param>
			/// <returns>位於 ModuleTemplates 底下則為 true</returns>
			public static bool IsModuleTemplate(Type type)
			{
				var script = ScriptOf(type);
				if(script == null) return false;

				return AssetDatabase.GetAssetPath(script).Contains("/ModuleTemplates/", StringComparison.Ordinal);
			}

			/// <summary>
			///     在指定檔案內找出某個符號第一次出現的行號。
			///     反射拿不到行號，只能從原始碼回推
			/// </summary>
			/// <param name="assetPath">相對於專案根目錄的路徑</param>
			/// <param name="symbol">要定位的成員或型別名。可以是欄位名，也可以是 ctor(參數) 這種注入點描述</param>
			/// <returns>行號（自 1 起算），找不到時為 0</returns>
			public static int LineOfSymbol(string assetPath, string symbol)
			{
				if(string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(symbol)) return 0;

				sourceByPath ??= SourceFiles.ToDictionary(file => file.AssetPath);
				if(!sourceByPath.TryGetValue(assetPath, out var source)) return 0;

				// 注入點描述可能長成 ctor(config) 或 Construct(dep)，取結尾的識別字去找就對得上宣告那行
				var tail = SymbolTailPattern.Match(symbol);
				if(!tail.Success) return 0;

				var pattern = new Regex($@"(?<![A-Za-z0-9_]){Regex.Escape(tail.Groups[1].Value)}(?![A-Za-z0-9_])");

				foreach(var (lineNumber, line) in CodeLines(source))
				{
					if(pattern.IsMatch(line)) return lineNumber;
				}

				return 0;
			}

			/// <summary>
			///     載入指定路徑的資產，讓驗證結果能點擊跳轉
			/// </summary>
			/// <param name="assetPath">相對於專案根目錄的路徑</param>
			/// <returns>找到的資產，找不到時為 null</returns>
			public static UnityEngine.Object AssetAt(string assetPath)
			{
				return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
			}

			/// <summary>
			///     取得指定層級所有 .cs 檔的內容
			/// </summary>
			/// <param name="layers">要取得的層級</param>
			/// <returns>符合層級的原始碼檔案</returns>
			public static IEnumerable<SourceFile> SourceFilesOf(params SumorinLayer[] layers)
			{
				var wanted = new HashSet<SumorinLayer>(layers);
				return SourceFiles.Where(file => wanted.Contains(file.Layer));
			}

			/// <summary>
			///     列出型別上所有由 Zenject 注入的相依型別（欄位、屬性、建構子參數、注入方法參數）
			/// </summary>
			/// <param name="type">要檢查的型別</param>
			/// <returns>注入點名稱與相依型別</returns>
			public static IEnumerable<(string member, Type dependency)> InjectedDependencies(Type type)
			{
				const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

				foreach(var field in type.GetFields(All))
				{
					if(field.IsDefined(typeof(InjectAttribute), true))
					{
						yield return (field.Name, field.FieldType);
					}
				}

				foreach(var property in type.GetProperties(All))
				{
					if(property.IsDefined(typeof(InjectAttribute), true))
					{
						yield return (property.Name, property.PropertyType);
					}
				}

				foreach(var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				{
					foreach(var parameter in constructor.GetParameters())
					{
						yield return ($"ctor({parameter.Name})", parameter.ParameterType);
					}
				}

				foreach(var method in type.GetMethods(All))
				{
					if(!method.IsDefined(typeof(InjectAttribute), true)) continue;

					foreach(var parameter in method.GetParameters())
					{
						yield return ($"{method.Name}({parameter.Name})", parameter.ParameterType);
					}
				}
			}

			/// <summary>
			///     判斷型別是否為指定泛型定義本身或其封閉建構型別
			/// </summary>
			/// <param name="type">要判定的型別</param>
			/// <param name="genericDefinition">泛型定義，如 typeof(IRepository&lt;&gt;)</param>
			/// <returns>相符則為 true</returns>
			public static bool IsGenericDefinition(Type type, Type genericDefinition)
			{
				if(type == null || !type.IsGenericType) return false;

				return type.GetGenericTypeDefinition() == genericDefinition;
			}

			/// <summary>
			///     沿著型別自身與其所有介面尋找指定泛型定義的封閉型別
			/// </summary>
			/// <param name="type">要搜尋的型別</param>
			/// <param name="genericDefinition">泛型定義，如 typeof(IRepository&lt;&gt;)</param>
			/// <returns>找到的封閉型別，找不到時為 null</returns>
			public static Type FindClosedGeneric(Type type, Type genericDefinition)
			{
				if(type == null) return null;
				if(IsGenericDefinition(type, genericDefinition)) return type;

				return type.GetInterfaces().FirstOrDefault(i => IsGenericDefinition(i, genericDefinition));
			}

			/// <summary>
			///     取得型別對應的 MonoScript，讓驗證結果能點擊跳到原始碼
			/// </summary>
			/// <param name="type">要定位的型別</param>
			/// <returns>找到的 MonoScript，找不到時為 null</returns>
			public static UnityEngine.Object ScriptOf(Type type)
			{
				if(type == null) return null;
				if(CachedScripts.TryGetValue(type, out var cached)) return cached;

				var found = FindScriptByFileName(type) ?? FindScriptByDeclaration(type);

				CachedScripts[type] = found;
				return found;
			}

			// 檔名與型別同名時最快也最準
			private static UnityEngine.Object FindScriptByFileName(Type type)
			{
				foreach(var guid in AssetDatabase.FindAssets($"{type.Name} t:MonoScript"))
				{
					var path = AssetDatabase.GUIDToAssetPath(guid);
					if(Path.GetFileNameWithoutExtension(path) != type.Name) continue;

					var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
					if(script != null) return script;
				}

				return null;
			}

			// 一個檔案放多個型別時檔名對不上，改查原始碼裡的宣告位置。
			// 同名型別分屬不同 namespace 時只會留下後掃到的那個，那是定位資訊不是判定依據，可以接受
			private static UnityEngine.Object FindScriptByDeclaration(Type type)
			{
				declarationPaths ??= BuildDeclarationIndex();

				return declarationPaths.TryGetValue(type.Name, out var path) ? AssetDatabase.LoadAssetAtPath<MonoScript>(path) : null;
			}

			private static Dictionary<string, string> BuildDeclarationIndex()
			{
				var result = new Dictionary<string, string>();

				foreach(var file in SourceFiles)
				{
					foreach(var (_, line) in CodeLines(file))
					{
						var match = DeclarationPattern.Match(line);

						if(match.Success)
						{
							result[match.Groups[1].Value] = file.AssetPath;
						}
					}
				}

				return result;
			}

			// 剝掉註解與字串內容，只留下真正的程式碼。
			// 字串一定要辨識：漏掉的話一句 var s = "/*"; 會讓 inBlockComment 卡住，
			// 該檔案後面所有行被當成註解丟棄，整個檔案的規則靜默失效
			private static string StripComments(string line, ref bool inBlockComment)
			{
				var result = new System.Text.StringBuilder(line.Length);

				for(var i = 0; i < line.Length; i++)
				{
					var current = line[i];
					var next = i + 1 < line.Length ? line[i + 1] : '\0';

					if(inBlockComment)
					{
						if(current == '*' && next == '/')
						{
							inBlockComment = false;
							i++;
						}

						continue;
					}

					if(current == '/' && next == '/') break;

					if(current == '/' && next == '*')
					{
						inBlockComment = true;
						i++;
						continue;
					}

					// 字串與字元常值整段跳過，順便讓型別名比對不會命中字面值裡的同名文字
					if(current == '@' && next == '"')
					{
						i = SkipVerbatimString(line, i + 1);
						continue;
					}

					if(current == '"')
					{
						i = SkipString(line, i, '"');
						continue;
					}

					if(current == '\'')
					{
						i = SkipString(line, i, '\'');
						continue;
					}

					result.Append(current);
				}

				return result.ToString();
			}

			private static int SkipString(string line, int openIndex, char quote)
			{
				for(var i = openIndex + 1; i < line.Length; i++)
				{
					if(line[i] == '\\')
					{
						i++;
						continue;
					}

					if(line[i] == quote) return i;
				}

				return line.Length;
			}

			// 逐字字串沒有反斜線跳脫，連續兩個引號才是一個引號
			private static int SkipVerbatimString(string line, int openIndex)
			{
				for(var i = openIndex + 1; i < line.Length; i++)
				{
					if(line[i] != '"') continue;

					if(i + 1 < line.Length && line[i + 1] == '"')
					{
						i++;
						continue;
					}

					return i;
				}

				return line.Length;
			}

			private static bool IsManaged(string assemblyName)
			{
				if(string.IsNullOrEmpty(assemblyName)) return false;

				managedAssemblies ??= CollectManagedAssemblies();
				return managedAssemblies.Contains(assemblyName);
			}

			// 白名單取自編譯管線，不用名稱黑名單。黑名單一定會漏，任何沒列到的第三方外掛都會被當成自己人掃進來。
			// PlayerWithoutTestAssemblies 已排除 Editor-only 與測試 assembly，這裡再排掉 Plugins 下的第三方
			private static HashSet<string> CollectManagedAssemblies()
			{
				var result = new HashSet<string>();

				foreach(var assembly in CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies))
				{
					if(assembly.sourceFiles.Length == 0) continue;

					var firstFile = assembly.sourceFiles[0];
					if(!firstFile.StartsWith("Assets/", StringComparison.Ordinal)) continue;
					if(firstFile.StartsWith("Assets/Plugins/", StringComparison.Ordinal)) continue;

					result.Add(assembly.name);
				}

				return result;
			}

			// ponytail: 用「以點分隔的最後一段」比對，避免 IReadOnlyReactivePropertyEx 那類「開頭相符」的誤判蔓延到 assembly 名
			private static bool MatchesSegment(string assemblyName, string segment)
			{
				if(assemblyName == segment) return true;

				return assemblyName.EndsWith("." + segment, StringComparison.Ordinal);
			}

			private static Type[] CollectTypes()
			{
				var result = new List<Type>();

				foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					if(!IsManaged(assembly.GetName().Name)) continue;

					Type[] types;

					try
					{
						types = assembly.GetTypes();
					}
					catch(ReflectionTypeLoadException e)
					{
						types = e.Types.Where(t => t != null).ToArray();
					}

					result.AddRange(types.Where(t => !t.IsNested || t.IsNestedPublic));
				}

				return result.ToArray();
			}

			// ponytail: 只掃 Assets/ 下的原始碼。以 UPM package 安裝的框架不含 Flow / View，掃不到也不會漏判；
			// 真要涵蓋 Packages/ 時改用 CompilationPipeline.GetAssemblies() 逐個取 sourceFiles
			private static List<SourceFile> CollectSourceFiles()
			{
				var result = new List<SourceFile>();

				foreach(var path in Directory.EnumerateFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories))
				{
					var assetPath = "Assets" + path.Substring(Application.dataPath.Length).Replace('\\', '/');
					var layer = LayerOfAssembly(AssemblyNameOfSourceFile(assetPath));
					if(layer == SumorinLayer.Outside) continue;

					result.Add(new SourceFile(assetPath, layer, File.ReadAllLines(path)));
				}

				return result;
			}

			// ponytail: 用 CompilationPipeline 查檔案歸屬 assembly，查不到時退回 Outside，不自己猜資料夾語意
			private static string AssemblyNameOfSourceFile(string assetPath)
			{
				var assemblyName = CompilationPipeline.GetAssemblyNameFromScriptPath(assetPath);
				if(string.IsNullOrEmpty(assemblyName)) return string.Empty;

				return assemblyName.EndsWith(".dll", StringComparison.Ordinal) ? assemblyName.Substring(0, assemblyName.Length - 4) : assemblyName;
			}

		#region Nested Types
			/// <summary>
			///     一個受架構管轄的原始碼檔案
			/// </summary>
			public readonly struct SourceFile
			{
				/// <summary>相對於專案根目錄的路徑</summary>
				public readonly string AssetPath;

				/// <summary>該檔案所屬的架構層級</summary>
				public readonly SumorinLayer Layer;

				/// <summary>檔案的每一行內容</summary>
				public readonly string[] Lines;

				/// <summary>
				///     建立原始碼檔案紀錄
				/// </summary>
				/// <param name="assetPath">相對於專案根目錄的路徑</param>
				/// <param name="layer">所屬架構層級</param>
				/// <param name="lines">檔案的每一行內容</param>
				public SourceFile(string assetPath, SumorinLayer layer, string[] lines)
				{
					AssetPath = assetPath;
					Layer = layer;
					Lines = lines;
				}
			}
		#endregion
		}
	}
#endif