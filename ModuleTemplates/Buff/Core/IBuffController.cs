using System.Collections.Generic;
using Sumorin.DDDCore;
using UniRx;

namespace Sumorin.Buff
{
	/// <summary>
	///     Buff Controller 介面（資源型）
	/// </summary>
	public interface IBuffController
	{
		/// <summary>
		///     訂閱 Buff 的當前層數
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		/// <returns>層數值面，Buff 不存在時回傳 null</returns>
		IReadOnlyReactiveProperty<int> ObserveStackCount(string buffId);

		/// <summary>
		///     訂閱 Buff 的剩餘時效
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		/// <returns>剩餘時效值面，Buff 不存在時回傳 null</returns>
		IReadOnlyReactiveProperty<float> ObserveLifetime(string buffId);

		/// <summary>
		///     取得 Buff 狀態快照
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		/// <returns>狀態快照，Buff 不存在時回傳 null</returns>
		BuffInfo? GetBuffInfo(string buffId);

		/// <summary>
		///     取得擁有者的所有 Buff 狀態快照
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <returns>狀態快照列表</returns>
		List<BuffInfo> GetBuffInfosByOwner(string ownerId);

		/// <summary>
		///     施加 Buff，已存在同名 Buff 時依配置的堆疊行為處理
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="buffName">Buff 名稱</param>
		/// <param name="sourceId">來源識別碼（施加者）</param>
		/// <returns>成功時酬載為 Buff 識別碼；配置不存在或被互斥擋下時失敗</returns>
		CommandResult AddBuff(string ownerId, string buffName, string sourceId);

		/// <summary>
		///     移除 Buff
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		CommandResult RemoveBuff(string buffId);

		/// <summary>
		///     移除指定來源施加的所有 Buff
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="sourceId">來源識別碼</param>
		CommandResult RemoveBuffsBySource(string ownerId, string sourceId);

		/// <summary>
		///     移除擁有者的所有 Buff
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		CommandResult RemoveBuffsByOwner(string ownerId);

		/// <summary>
		///     移除擁有指定標籤的所有 Buff
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="tag">標籤名稱</param>
		CommandResult RemoveBuffsByTag(string ownerId, string tag);

		/// <summary>
		///     推進時間，只影響 TimeBased 類型的 Buff
		/// </summary>
		/// <param name="deltaTime">經過的時間（秒）</param>
		CommandResult TickTime(float deltaTime);

		/// <summary>
		///     推進回合，只影響指定擁有者的 TurnBased 類型 Buff
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="turns">經過的回合數（預設 1）</param>
		CommandResult TickTurn(string ownerId, int turns = 1);

		/// <summary>
		///     調整 Buff 剩餘時效
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		/// <param name="delta">變更量（正數延長，負數縮短）</param>
		CommandResult AdjustBuffLifetime(string buffId, float delta);

		/// <summary>
		///     設定 Buff 剩餘時效
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		/// <param name="lifetime">新的時效值</param>
		CommandResult SetBuffLifetime(string buffId, float lifetime);

		/// <summary>
		///     增加一層
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		CommandResult AddStack(string buffId);

		/// <summary>
		///     移除一層
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		CommandResult RemoveStack(string buffId);

		/// <summary>
		///     調整層數
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		/// <param name="delta">變更量（正數增加，負數減少）</param>
		CommandResult AdjustStack(string buffId, int delta);
	}
}