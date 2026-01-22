using System;
using UniRx;

namespace Rino.GameFramework.RinoUtility
{
    /// <summary>
    /// 響應式事件，結合 Subject 的觸發能力與 IObservable 的訂閱介面
    /// </summary>
    /// <typeparam name="T">事件資料型別</typeparam>
    public sealed class ReactiveEvent<T> : IObservable<T>, IDisposable
    {
        private readonly Subject<T> subject = new();

        /// <summary>
        /// 訂閱事件
        /// </summary>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            return subject.Subscribe(observer);
        }

        /// <summary>
        /// 觸發事件
        /// </summary>
        /// <param name="value">事件資料</param>
        public void Invoke(T value)
        {
            subject.OnNext(value);
        }

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            subject.Dispose();
        }
    }
}
