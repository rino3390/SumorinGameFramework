using UnityEngine;

namespace Sumorin.GameManagerBase
{
	/// <summary>
	///     打點配置契約，DataScript 實作後即自動受「打點對齊」架構檢查涵蓋，
	///     框架不需認識遊戲側的 I{X}Config
	/// </summary>
	public interface IAnimationHitConfig
	{
		/// <summary>
		///     打點所在的動畫片段
		/// </summary>
		AnimationClip Clip { get; }

		/// <summary>
		///     打點時間（秒），必須與 <see cref="Clip" /> 上的 Animation Event 時間一致
		/// </summary>
		float[] HitTimes { get; }
	}
}