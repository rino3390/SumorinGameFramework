using Sumorin.DDDCore;
using VContainer;

namespace Sumorin.Save
{
	/// <summary>
	///     參與者註冊的輔助方法，讓組裝根每個參與者一行
	/// </summary>
	public static class SaveBindingExtensions
	{
		/// <summary>
		///     把一個 Repository 包成轉接層後註冊為存檔參與者
		/// </summary>
		/// <typeparam name="TEntity">Repository 管理的 Entity 類型</typeparam>
		/// <param name="builder">容器建置器</param>
		/// <param name="saveKey">存檔鍵，全域不可重複</param>
		/// <param name="loadOrder">載入順序，數字越大越先載入</param>
		/// <param name="isGlobal">是否為跨存檔資料</param>
		/// <remarks>目標 Repository 與 <see cref="ISerializer" /> 由容器解析，建置前須完成註冊。</remarks>
		public static void RegisterRepositoryParticipant<TEntity>(this IContainerBuilder builder, string saveKey, int loadOrder, bool isGlobal)
			where TEntity: Entity
		{
			builder.Register<ISaveParticipant, RepositorySaveAdapter<TEntity>>(Lifetime.Singleton)
				   .WithParameter("saveKey", saveKey)
				   .WithParameter("loadOrder", loadOrder)
				   .WithParameter("isGlobal", isGlobal);
		}

		/// <summary>
		///     把一個單例狀態物件同時註冊為自身型別與存檔參與者
		/// </summary>
		/// <typeparam name="TState">自行實作 <see cref="ISaveParticipant" /> 的狀態型別</typeparam>
		/// <param name="builder">容器建置器</param>
		/// <remarks>兩個註冊指向同一個實例，狀態物件由持有它的 Controller 取用。</remarks>
		public static void RegisterStateParticipant<TState>(this IContainerBuilder builder) where TState: ISaveParticipant
		{
			builder.Register<TState>(Lifetime.Singleton).AsSelf().As<ISaveParticipant>();
		}
	}
}