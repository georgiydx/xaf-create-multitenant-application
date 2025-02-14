﻿using System.Collections;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Utils;
using View = DevExpress.ExpressApp.View;

namespace XAF.Testing.XAF{
    public static class ActionExtensions{
        public static IObservable<T> WhenAvailable<T>(this IObservable<T> source) where T:ActionBase 
            => source.Where(action => action.Available());
        public static bool Available(this ChoiceActionItem item)
            => item.Active && item.Enabled;
        public static IEnumerable<ChoiceActionItem> Available(this IEnumerable<ChoiceActionItem> source) 
                => source.Where(item => item.Available());
        
        public static IEnumerable<ChoiceActionItem> Active(this IEnumerable<ChoiceActionItem> source) 
                => source.Where(item => item.Active);
        
        public static T View<T>(this ActionBase actionBase) where T : View => actionBase.Controller.Frame?.View as T;
        public static View View(this ActionBase actionBase) => actionBase.View<View>();
        public static IObservable<ItemsChangedEventArgs> WhenItemsChanged(this SingleChoiceAction action,ChoiceActionItemChangesType? changesType=null) 
            => action.WhenEvent<ItemsChangedEventArgs>(nameof(SingleChoiceAction.ItemsChanged))
                .Where(e =>changesType==null||e.ChangedItemsInfo.Any(pair => pair.Value==changesType) ).TakeUntil(action.WhenDisposed());

        public static Frame Frame(this ActionBase action) => action.Controller?.Frame;
        public static IEnumerable<ChoiceActionItem> Items<T>(this SingleChoiceAction action)
            => action.Items.Where(item => item.Data is T);
        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuteCompleted(this SimpleAction action) 
            => action.WhenEvent<ActionBaseEventArgs>(nameof(ActionBase.ExecuteCompleted)).Cast<SimpleActionExecuteEventArgs>().TakeUntilDisposed(action);
        public static IObservable<SimpleActionExecuteEventArgs> WhenExecuted(this SimpleAction action) 
            => action.WhenEvent<ActionBaseEventArgs>(nameof(SimpleAction.Executed)).Cast<SimpleActionExecuteEventArgs>().TakeUntilDisposed(action);
        public static IObservable<(TAction action, CancelEventArgs e)> WhenExecuting<TAction>(this TAction action) where TAction : ActionBase 
            => action.WhenEvent<CancelEventArgs>(nameof(ActionBase.Executing)).InversePair(action).TakeUntilDisposed(action);
        public static IObservable<T> Trigger<T>(this SimpleAction action, IObservable<T> afterExecuted,params object[] selection)
            => afterExecuted.Trigger(() => action.DoExecute(selection));
        public static IObservable<Unit> Trigger(this SimpleAction action, params object[] selection)
            => action.Trigger(action.WhenExecuteCompleted().Take(1).ToUnit(),selection);
        
        public static IObservable<TAction> WhenActivated<TAction>(this TAction simpleAction,params string[] contexts) where TAction : ActionBase 
            => simpleAction.ResultValueChanged(action => action.Active).SelectMany(t => contexts.Concat(Controller.ControllerActiveKey.YieldItem()).Select(context => (t,context)))
                .Where(t => t.t.action.Active.ResultValue&&t.t.action.Active.Contains(t.context)&& t.t.action.Active[t.context])
                .Select(t => t.t.action);

        public static IObservable<(BoolList boolList, BoolValueChangedEventArgs e)> ResultValueChanged(
            this IObservable<BoolList> source,bool? newValue=null) 
            => source.SelectMany(item => item.WhenEvent<BoolValueChangedEventArgs>(nameof(BoolList.ResultValueChanged))
                .Where(eventArgs => !newValue.HasValue || eventArgs.NewValue == newValue).InversePair(item));

        public static IObservable<(TAction action, BoolList boolList, BoolValueChangedEventArgs e)> ResultValueChanged<TAction>(
            this TAction source, Func<TAction, BoolList> boolListSelector) where TAction : ActionBase 
            => boolListSelector(source).Observe().ResultValueChanged().Select(tuple => (source, tuple.boolList, tuple.e));

        public static void DoExecute(this SimpleAction action, params object[] selection) 
            => action.DoExecute(() => action.DoExecute(),selection);
        
        public static IObservable<T> Trigger<T>(this SingleChoiceAction action, IObservable<T> afterExecuted,params object[] selection)
            => action.Trigger(afterExecuted,() => action.SelectedItem,selection);
        
        public static IObservable<T> Trigger<T>(this SingleChoiceAction action, IObservable<T> afterExecuted,Func<ChoiceActionItem> selectedItem,params object[] selection)
            => afterExecuted.Trigger(() => action.DoExecute(selectedItem(), selection));
        
        public static void DoExecute(this SingleChoiceAction action,ChoiceActionItem selectedItem, params object[] objectSelection) 
            => action.DoExecute( () => action.DoExecute(selectedItem), objectSelection);
        public static bool Available(this ActionBase actionBase) 
            => actionBase.Active && actionBase.Enabled;
        public static void DoExecute(this ActionBase action, Action execute, object[] objectSelection){
            if (objectSelection.Any()) {
                var context = action.SelectionContext;
                action.SelectionContext = new SelectionContext(objectSelection.Single());
                execute();
                action.SelectionContext = context;
            }
            else {
                execute();
            }
        }
        
        private static IObservable<T> Trigger<T>(this IObservable<T> afterExecuted, Action action)
            => afterExecuted.Merge(action.DeferAction(action).To<T>(),new SynchronizationContextScheduler(SynchronizationContext.Current!));
        
    }
    
    sealed class SelectionContext:ISelectionContext {
        public SelectionContext(object currentObject) {
            CurrentObject = currentObject;
            SelectedObjects = new List<object>(){currentObject};
            OnCurrentObjectChanged();
            OnSelectionChanged();
        }
        public object CurrentObject { get; set; }
        public IList SelectedObjects { get; set; }
        public SelectionType SelectionType => SelectionType.MultipleSelection;
        public string Name => null;
        public bool IsRoot => false;
        public event EventHandler CurrentObjectChanged;
        public event EventHandler SelectionChanged;
        public event EventHandler SelectionTypeChanged;
            
        private void OnSelectionChanged() => SelectionChanged?.Invoke(this, EventArgs.Empty);
        private void OnCurrentObjectChanged() => CurrentObjectChanged?.Invoke(this, EventArgs.Empty);

        public void OnSelectionTypeChanged() => SelectionTypeChanged?.Invoke(this, EventArgs.Empty);
    }

}