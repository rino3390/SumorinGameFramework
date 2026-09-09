#if ODIN_VALIDATOR
	using System.Collections.Generic;
	using System.Linq;
	using Mono.Cecil;

	namespace Sumorin.ArchitectureValidator
	{
		/// <summary>
		///     讀取編譯後的 assembly，從 IL 取出規則需要的呼叫與型別引用。
		///     名字在 IL 裡都已解析完，別名、var、換行與註解都不影響結果
		/// </summary>
		public static class IlScanner
		{
			/// <summary>
			///     列出 assembly 內所有 Subscribe 與 SubscribeAsync 的泛型呼叫
			/// </summary>
			/// <param name="assemblyPath">assembly 檔案路徑</param>
			/// <returns>呼叫者型別與事件型別的 FullName，寫法同 <see cref="System.Type.FullName" /></returns>
			public static IEnumerable<(string owner, string eventType)> SubscribeCalls(string assemblyPath)
			{
				using var assembly = Read(assemblyPath);
				var result = new List<(string owner, string eventType)>();

				foreach(var type in assembly.MainModule.GetTypes())
				{
					var owner = FullNameOf(DeclaringNonGenerated(type));

					foreach(var instruction in type.Methods.Where(method => method.HasBody).SelectMany(method => method.Body.Instructions))
					{
						if(instruction.Operand is not GenericInstanceMethod call) continue;
						if(call.GenericArguments.Count != 1) continue;
						if(call.Name != "Subscribe" && call.Name != "SubscribeAsync") continue;

						result.Add((owner, FullNameOf(call.GenericArguments[0])));
					}
				}

				return result;
			}

			/// <summary>
			///     列出型別在欄位、屬性、方法簽章、區域變數與方法體中引用到的所有型別，含巢狀型別與編譯器產生的閉包
			/// </summary>
			/// <param name="assemblyPath">assembly 檔案路徑</param>
			/// <param name="typeFullName">要檢查的型別，寫法同 <see cref="System.Type.FullName" /></param>
			/// <returns>引用所在的成員名與被引用型別的 FullName，找不到型別時為空</returns>
			public static IEnumerable<(string member, string typeFullName)> ReferencedTypes(string assemblyPath, string typeFullName)
			{
				using var assembly = Read(assemblyPath);
				var target = assembly.MainModule.GetTypes().FirstOrDefault(type => FullNameOf(type) == typeFullName);
				if(target == null) return Enumerable.Empty<(string member, string typeFullName)>();

				var result = new List<(string member, string typeFullName)>();

				foreach(var type in SelfAndNested(target))
				{
					foreach(var field in type.Fields)
					{
						Add(result, field.Name, field.FieldType);
					}

					foreach(var property in type.Properties)
					{
						Add(result, property.Name, property.PropertyType);
					}

					foreach(var method in type.Methods)
					{
						Add(result, method.Name, method.ReturnType);

						foreach(var parameter in method.Parameters)
						{
							Add(result, method.Name, parameter.ParameterType);
						}

						if(!method.HasBody) continue;

						foreach(var variable in method.Body.Variables)
						{
							Add(result, method.Name, variable.VariableType);
						}

						foreach(var instruction in method.Body.Instructions)
						{
							switch(instruction.Operand)
							{
								case TypeReference typeReference:
									Add(result, method.Name, typeReference);
									break;
								case GenericInstanceMethod genericCall:
									Add(result, method.Name, genericCall.DeclaringType);

									foreach(var argument in genericCall.GenericArguments)
									{
										Add(result, method.Name, argument);
									}

									break;
								case MethodReference methodReference:
									Add(result, method.Name, methodReference.DeclaringType);
									break;
								case FieldReference fieldReference:
									Add(result, method.Name, fieldReference.DeclaringType);
									Add(result, method.Name, fieldReference.FieldType);
									break;
							}
						}
					}
				}

				return result.Distinct().ToList();
			}

			private static AssemblyDefinition Read(string assemblyPath)
			{
				// InMemory 讀完就放開檔案，Editor 正在使用的 ScriptAssemblies 不會被鎖住
				return AssemblyDefinition.ReadAssembly(assemblyPath, new() { InMemory = true });
			}

			// 閉包與 async 狀態機是編譯器產生的巢狀型別，名字以 < 開頭；訂閱要算在寫那段程式的類別頭上
			private static TypeDefinition DeclaringNonGenerated(TypeDefinition type)
			{
				while(type.Name.StartsWith("<") && type.DeclaringType != null)
				{
					type = type.DeclaringType;
				}

				return type;
			}

			private static IEnumerable<TypeDefinition> SelfAndNested(TypeDefinition type)
			{
				yield return type;

				foreach(var nested in type.NestedTypes.SelectMany(SelfAndNested))
				{
					yield return nested;
				}
			}

			private static void Add(ICollection<(string member, string typeFullName)> result, string member, TypeReference type)
			{
				foreach(var flattened in Flatten(type))
				{
					result.Add((member, FullNameOf(flattened)));
				}
			}

			// 泛型實例、陣列、ref 都拆到元素型別，List<PlayerQueryService> 才數得到 PlayerQueryService
			private static IEnumerable<TypeReference> Flatten(TypeReference type)
			{
				// 陣列與 ref 一路剝到元素型別；泛型實例要留著，型別參數還得拆
				while(type is TypeSpecification specification and not GenericInstanceType)
				{
					type = specification.ElementType;
				}

				switch(type)
				{
					case null:
						yield break;
					case GenericInstanceType generic:
						yield return generic.ElementType;

						foreach(var inner in generic.GenericArguments.SelectMany(Flatten))
						{
							yield return inner;
						}

						break;
					default:
						yield return type;

						break;
				}
			}

			// Reflection 的巢狀型別分隔是 +，Cecil 是 /；統一成 Reflection 寫法，規則端才能直接拿 Type.FullName 比對
			private static string FullNameOf(TypeReference type)
			{
				return type.FullName.Replace('/', '+');
			}
		}
	}
#endif