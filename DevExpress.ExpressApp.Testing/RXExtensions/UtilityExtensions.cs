﻿using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Testing.DevExpress.ExpressApp;

namespace DevExpress.ExpressApp.Testing.RXExtensions{
    public static class UtilityExtensions{
        public static IObservable<T> SubscribeReplay<T>(this IObservable<T> source, int bufferSize = 0){
            var replay = bufferSize > 0 ? source.Replay(bufferSize) : source.Replay();
            replay.Connect();
            return replay;
        }

        public static IObservable<T> Log<T>(this IObservable<T> source,[CallerMemberName]string caller="") 
            => source.Do(_ => Console.WriteLine(caller));

        public static IObservable<T> ReplayConnect<T>(this IObservable<T> source, int bufferSize = 0) 
            => source.SubscribeReplay(bufferSize);
        
        public static IObservable<TResult> Use<T, TResult>(this T source, Func<T, IObservable<TResult>> selector) where T : IDisposable
            => Observable.Using(() => source, selector);
        
        public static IObservable<T> Defer<T>(this object o, IObservable<T> execute)
            => Observable.Defer(() => execute);
        
        public static IObservable<Unit> DeferAction<T>(this T o, Action execute)
            => Observable.Defer(() => {
                execute();
                return Observable.Empty<Unit>();
            });
        public static IObservable<Unit> DeferAction<T>(this T o, Action<T> execute)
            => Observable.Defer(() => {
                execute(o);
                return Observable.Empty<Unit>();
            });

        public static IObservable<T> Defer<T,TObject>(this TObject o, Func<TObject,IObservable<T>> selector)
            => Observable.Defer(() => selector(o));
        
        public static IObservable<T> Defer<T>(this object o, Func<IObservable<T>> selector)
            => Observable.Defer(selector);
        
        public static IObservable<T> Defer<T>(this object o, Func<IEnumerable<T>> selector)
            => Observable.Defer(() => selector().ToNowObservable());
        
        public static IObservable<T> ObserveOnContext<T>(this IObservable<T> source, SynchronizationContext synchronizationContext) 
            => source.ObserveOn(synchronizationContext);

        public static IObservable<T> ObserveOnContext<T>(this IObservable<T> source, bool throwIfNull) 
            => source.If(_ => throwIfNull && SynchronizationContext.Current == null,
                () => Observable.Throw<T>(new NullReferenceException(nameof(SynchronizationContext))), () => source);

        public static IObservable<T> ObserveOnContext<T>(this IObservable<T> source) {
            var synchronizationContext = SynchronizationContext.Current;
            return synchronizationContext != null ? source.ObserveOn(synchronizationContext) : source;
        }
        
        public static IObservable<T> DoAfterSubscribe<T>(this IObservable<T> source, Action action) 
            => Observable.Create<T>(observer => {
                var disposable = source.Subscribe(observer);
                action();
                return disposable;
            });
        
        public static IObservable<TSource> DoWhen<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate, Action<TSource> action)
            => source.Do(source1 => {
                if (predicate(source1)) {
                    action(source1);
                }
            });

        public static IObservable<TSource> Timeout<TSource>(
            this IObservable<TSource> source, TimeSpan dueTime, string timeoutMessage) 
            => source.Timeout(dueTime, Observable.Throw<TSource>(new TimeoutException(timeoutMessage)));
        
        public static IObservable<T> DoOnComplete<T>(this IObservable<T> source, Action onComplete)
            => source.Do(_ => { }, onComplete);
        
        public static TimeSpan TimeoutInterval = (Debugger.IsAttached ? 120 : 20).Seconds();
        public static IObservable<TSource> Timeout<TSource>(
            this IObservable<TSource> source,string message) 
            => source.Timeout(TimeoutInterval, Observable.Throw<TSource>(new TimeoutException(message)));

        public static IObservable<SingleChoiceAction> AssertSingleChoiceAction<TItemDataType>(this IObservable<Frame> source,string actionId,int itemsCount) 
            => source.Select(frame => frame.Action<SingleChoiceAction>(actionId)).Assert($"{nameof(AssertSingleChoiceAction)} {actionId}")
                .SelectMany(choiceAction => choiceAction.Items<TItemDataType>().Skip(itemsCount - 1).ToNowObservable().To(choiceAction))
                .Assert($"{nameof(AssertSingleChoiceAction)} {actionId} {itemsCount}");

        public static IObservable<TSource> Assert<TSource>(
            this IObservable<TSource> source, TimeSpan? timeout = null, [CallerMemberName] string caller = "") 
            => source.Assert(caller, timeout);

        public static IObservable<TSource> Assert<TSource>(this IObservable<TSource> source,string message,TimeSpan? timeout=null) 
            => source.Log(message).ThrowIfEmpty(message).TakeAndReplay(1).RefCount().Timeout(timeout??TimeoutInterval, message);
        
        public static TestObserver<T> Test<T>(this IObservable<T> source){
            var testObserver = new TestObserver<T>();
            source.Subscribe(testObserver);
            return testObserver;
        }

        public static IObservable<T> AsyncFinally<T>(this IObservable<T> source, Func<IObservable<object>> action)
            => source.AsyncFinally(async () => await action().ToTask());

        public static IObservable<T> AsyncFinally<T>(this IObservable<T> source, Func<Task> action) 
            => source
                .Materialize()
                .SelectMany(async n => {
                    switch (n.Kind){
                        case NotificationKind.OnCompleted:
                        case NotificationKind.OnError:
                            await action();
                            return n;
                        case NotificationKind.OnNext:
                            return n;
                        default:
                            throw new NotImplementedException();
                    }
                })
                .Dematerialize();
    }
}