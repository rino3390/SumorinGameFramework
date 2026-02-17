using System;
using System.Collections.Generic;

namespace Sumorin.GameFramework.BuffSystem
{
	/// <summary>
	/// Buff Controller 介面（資源型）
	/// </summary>
	public interface IBuffController
	{
		/// <summary>
		/// 註冊 Buff 配置
		/// </summary>
		/// <param name="configs">配置列表</param>
		void RegisterConfigs(List<BuffConfig> configs);

		/// <summary>
		/// 訂閱擁有者的 Buff 列表變化
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <returns>Buff 資訊列表的 Observable</returns>
		IObservable<List<BuffInfo>> ObserveBuffs(string ownerId);

		/// <summary>
		/// 新增 Buff
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="buffName">Buff 名稱</param>
		/// <param name="sourceId">來源識別碼（施加者）</param>
		/// <returns>新增的 Buff Id，若因互斥而無法新增則回傳 null</returns>
		string AddBuff(string ownerId, string buffName, string sourceId);

		/// <summary>
		/// 移除 Buff
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		void RemoveBuff(string buffId);

		/// <summary>
		/// 移除來源的所有 Buff
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="sourceId">來源識別碼</param>
		void RemoveBuffsBySource(string ownerId, string sourceId);

		/// <summary>
		/// 移除擁有者的所有 Buff
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		void RemoveBuffsByOwner(string ownerId);

		/// <summary>
		/// 移除擁有指定標籤的所有 Buff
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="tag">標籤名稱</param>
		void RemoveBuffsByTag(string ownerId, string tag);

		/// <summary>
		/// 時間流逝處理（僅影響 TimeBased 類型的 Buff）
		/// </summary>
		/// <param name="deltaTime">經過的時間（秒）</param>
		void TickTime(float deltaTime);

		/// <summary>
		/// 回合流逝處理（僅影響 TurnBased 類型的 Buff）
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <param name="turns">經過的回合數（預設為 1）</param>
		void TickTurn(string ownerId, int turns = 1);

		/// <summary>
		/// 取得 Buff
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		/// <returns>找到的 Buff，若不存在則回傳 null</returns>
		Buff GetBuff(string buffId);

		/// <summary>
		/// 取得擁有者的所有 Buff
		/// </summary>
		/// <param name="ownerId">擁有者識別碼</param>
		/// <returns>Buff 列表</returns>
		List<Buff> GetBuffsByOwner(string ownerId);

		/// <summary>
		/// 調整 Buff 剩餘時效
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		/// <param name="delta">變更量（正數延長，負數縮短；單位依 LifetimeType 決定為秒或回合）</param>
		void AdjustBuffLifetime(string buffId, float delta);

		/// <summary>
		/// 設定 Buff 剩餘時效
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		/// <param name="lifetime">新的時效值（單位依 LifetimeType 決定為秒或回合）</param>
		void SetBuffLifetime(string buffId, float lifetime);

		/// <summary>
		/// 新增一層堆疊
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		void AddStack(string buffId);

		/// <summary>
		/// 移除一層堆疊
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		void RemoveStack(string buffId);

		/// <summary>
		/// 調整 Buff 堆疊數
		/// </summary>
		/// <param name="buffId">Buff 識別碼</param>
		/// <param name="delta">變更量（正數增加，負數減少）</param>
		void AdjustStack(string buffId, int delta);
	}
}
