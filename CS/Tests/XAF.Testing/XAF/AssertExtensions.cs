﻿using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Aqua.EnumerableExtensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Security;

namespace XAF.Testing.XAF{
    public static class AssertExtensions{
        public static IObservable<ITabControlProvider> AssertTabbedGroup(this XafApplication application,
            Type objectType = null, int tabPagesCount = 0,Func<DetailView,bool> match=null,[CallerMemberName]string caller="")
            => application.AssertTabControl<ITabControlProvider>(objectType,match,caller).IgnoreElements()
                .If(group => tabPagesCount > 0 && group.TabPages != tabPagesCount,group => group.Observe().DelayOnContext()
                        .SelectMany(_ => new Exception(
                            $"{nameof(AssertTabbedGroup)} {objectType?.Name} expected {tabPagesCount} but was {group.TabPages}").ThrowTestException(caller).To<ITabControlProvider>()),
                    group => group.Observe())
                .Merge(application.WhenTabControl(objectType,match)).Replay().AutoConnect();
                
        
        public static IObservable<TTabbedControl> AssertTabControl<TTabbedControl>(this XafApplication application,Type objectType=null,Func<DetailView,bool> match=null,[CallerMemberName]string caller="") 
            => application.WhenTabControl<TTabbedControl>( objectType, match).Assert(objectType?.Name,caller:caller);

        public static IObservable<Frame> AssertNestedListView(this IObservable<ITabControlProvider> source, Frame frame, Type objectType, int selectedTabPageIndex,
            Func<Frame, IObservable<Unit>> existingObjectDetailview = null, Func<Frame,AssertAction> assert=null,bool inlineEdit=false,[CallerMemberName]string caller="")
            => source.AssertNestedListView(frame, objectType, group => group.SelectTab(selectedTabPageIndex),existingObjectDetailview,assert,inlineEdit,caller);
        
        public static IObservable<Frame> AssertNestedListView(this IObservable<ITabControlProvider> source, Frame frame, Type objectType, Action<ITabControlProvider> tabGroupAction,
            Func<Frame, IObservable<Unit>> existingObjectDetailview = null, Func<Frame,AssertAction> assert = null,bool inlineEdit=false,[CallerMemberName]string caller="")
            => frame.AssertNestedListView(objectType, existingObjectDetailview, assert, inlineEdit, caller)
                .Merge(source.DelayOnContext().Do(tabGroupAction).DelayOnContext().IgnoreElements().To<Frame>())
                .ReplayFirstTake();

        public static void ClearFilter(this Frame frame){
            if (frame.View is not ListView listView) return;
            frame.Application.GetRequiredService<IFilterClearer>().Clear(listView);
        }
        
        public static IObservable<Unit> AssertPdfViewer(this DetailView detailView) 
            => detailView.ObjectSpace.GetRequiredService<IPdfViewerAssertion>().Assert(detailView);

        public static IObservable<Unit> AssertRichEditControl(this DetailView detailView, bool assertMailMerge = false) 
            => detailView.ObjectSpace.GetRequiredService<IRichEditControlAsserter>().Assert(detailView, assertMailMerge);

        public static IObservable<object> AssertObjectsCount(this Frame frame, int objectsCount) 
            => frame.View is DetailView detailView ? detailView.WhenGridControl().Where(control =>
                        frame.Application.GetRequiredService<IUserControlObjects>().ObjectsCount(control) == objectsCount)
                    .Assert($"{nameof(AssertObjectsCount)} {frame.View.Id}")
                : frame.View.ToListView().CollectionSource.WhenCriteriaApplied().FirstAsync(o => o.GetCount() == objectsCount)
                    .Assert($"{nameof(AssertObjectsCount)} {frame.View.Id}");

        public static IObservable<Frame> AssertDashboardViewShowInDocumentAction(this IObservable<Frame> source,Func<SingleChoiceAction,int> itemsCount) 
            => source.DashboardViewItem(item => item.MasterViewItem()).ToFrame()
                .AssertSingleChoiceAction("ShowInDocument",itemsCount)
                .SelectMany(action => action.Items.ToNowObservable()
                    .SelectManySequential(item => action.Trigger(action.AssertShowInDocumentAction( item), () => item)))
                .IgnoreElements().Concat(source).ReplayFirstTake();

        public static IObservable<Frame> AssertShowInDocumentAction(this SingleChoiceAction action, ChoiceActionItem item) 
            => action.Application.GetRequiredService<IDocumentActionAssertion>().Assert(action,item);

        
        public static IObservable<Frame> AssertDashboardMasterDetail(this IObservable<Frame> source, Func<Frame, IObservable<Unit>> existingObjectDetailview = null
            , Func<Frame, AssertAction> assert = null, Func<Frame, IObservable<Frame>> detailViewFrameSelector = null, Func<DashboardViewItem, bool> listViewFrameSelector = null) 
            => source.AssertDashboardDetailView(detailViewFrameSelector, listViewFrameSelector).IgnoreElements()
                .Concat(source.AssertDashboardListView(listViewFrameSelector, existingObjectDetailview, assert))
                .IgnoreElements().Concat(source).ReplayFirstTake();
        

        public static IObservable<Frame> AssertDialogControllerListView(this SingleChoiceAction choiceAction,Type objectType, Func<Frame, AssertAction> assert, bool inlineEdit=false,[CallerMemberName]string caller="") 
            => choiceAction.Trigger(choiceAction.Application.AssertFrame(objectType,ViewType.ListView)
                    .Select(frame => (frame, source: choiceAction.Controller.Frame)))
                .DelayOnContext()
                .SelectMany(t => t.frame.Observe().AssertListView(assert:assert,inlineEdit:inlineEdit,caller:caller).To(t)
                    .If(_ => !inlineEdit,t1 => t1.frame.Observe().CloseWindow(choiceAction.Frame()),t1 => t1.Observe().ToFirst()));

        private static IObservable<Frame> AssertDashboardListView(this IObservable<Frame> changeViewVariant, Func<DashboardViewItem, bool> listViewFrameSelector,
            Func<Frame, IObservable<Unit>> assertExistingObjectDetailview, Func<Frame,AssertAction> assert = null, [CallerMemberName] string caller = "") 
            => changeViewVariant.AssertMasterFrame(listViewFrameSelector).IgnoreElements().To<Frame>().Concat(changeViewVariant)
                .AssertListView(assertExistingObjectDetailview, assert,listViewFrameSelector:listViewFrameSelector,caller:caller)
                .ReplayFirstTake();

        public static IObservable<Frame> AssertDashboardListView(this XafApplication application, string navigationId,
            string viewVariant, Func<Frame, IObservable<Unit>> existingObjectDetailview = null,Func<DashboardViewItem, bool> listViewFrameSelector=null, Func<Frame,AssertAction> assert = null)
            => application.AssertNavigation(navigationId).AssertDashboardListView(viewVariant, existingObjectDetailview,listViewFrameSelector,assert );

        public static IObservable<Frame> AssertDashboardListView(this IObservable<Frame> source, Func<Frame, IObservable<Unit>> existingObjectDetailview = null
            , Func<DashboardViewItem, bool> listViewFrameSelector = null, Func<Frame, AssertAction> assert = null)
            => source.AssertDashboardListView(listViewFrameSelector, existingObjectDetailview, assert).IgnoreElements().Concat(source).ReplayFirstTake();
        
        public static IObservable<Frame> AssertDashboardListView(this IObservable<Frame> source, string viewVariant, Func<Frame, IObservable<Unit>> existingObjectDetailview = null,
            Func<DashboardViewItem, bool> listViewFrameSelector=null, Func<Frame,AssertAction> assert = null)
            => source.AssertChangeViewVariant(viewVariant).AssertDashboardListView( existingObjectDetailview,listViewFrameSelector, assert);

        private static IObservable<Frame> AssertDashboardDetailView(this IObservable<Frame> source, Func<Frame, IObservable<Frame>> detailViewFrameSelector=null,
            Func<DashboardViewItem, bool> listViewFrameSelector=null){
            var detailViewFrame = source.SelectMany(detailViewFrameSelector??(frame =>frame.DashboardDetailViewFrame()) ).TakeAndReplay(1).AutoConnect();
            return detailViewFrame.IsEmpty()
                .If(isEmpty => isEmpty, _ => source, _ => source.Cast<Window>()
                    .AssertSelectDashboardListViewObject(listViewFrameSelector ?? (item => item.MasterViewItem()))
                    .SelectMany(_ => detailViewFrame).AssertDetailViewHasObject().IgnoreElements().To<Frame>()
                    .Concat(source)).ReplayFirstTake().Select(frame => frame);
        }
        public static IObservable<Frame> AssertSelectDashboardListViewObject(this IObservable<Frame> source, Func<DashboardViewItem, bool> itemSelector = null)
            => source.SelectMany(frame => frame.Observe().SelectDashboardListViewObject(itemSelector).Assert()).IgnoreElements().To<Frame>().Concat(source).ReplayFirstTake();

        public static IObservable<Frame> AssertDashboardListViewEditView(this IObservable<Frame> source, Func<Frame,IObservable<Frame>> detailView=null,Func<DashboardViewItem, bool> itemSelector = null)
            => source.AssertSelectDashboardListViewObject(itemSelector)
                .Where(frame => frame.MasterFrame(itemSelector).View is ListView listView&&listView.Model.MasterDetailMode==MasterDetailMode.ListViewAndDetailView)
                .Select(frame => frame)
                .AssertDashboardListViewEditViewHasObject(detailView).IgnoreElements()
                .Concat(source).ReplayFirstTake();
        
        public static IObservable<Frame> AssertFrame(this XafApplication application, params ViewType[] viewTypes) 
            => application.AssertFrame(typeof(object),viewTypes);
        public static IObservable<Frame> AssertFrame(this XafApplication application,Type objectType, params ViewType[] viewTypes) 
            => application.WhenFrame(objectType,viewTypes).Assert($"{nameof(AssertFrame)} {string.Join(", ",viewTypes)}");
        
        public static IObservable<Frame> AssertChangeViewVariant(this IObservable<Frame> source,string variantId) 
            => source.If(_ => variantId!=null,frame => frame.Observe().ChangeViewVariant(variantId),frame => frame.Observe()).Assert($"{variantId}")
                .ReplayFirstTake();

        public static IObservable<Unit> AssertNavigation(this XafApplication application,string view, string viewVariant,Func<IObservable<Frame>,IObservable<Unit>> assert, IObservable<Unit> canNavigate) 
            => application.AssertNavigation(view,_ => canNavigate
                    .SwitchIfEmpty(Observable.Defer(() => Observable.Throw<Unit>(new CannotNavigateException()))).ToUnit())
                .SelectMany(window => window.ToActive().Observe().If(_ => viewVariant!=null,window1 => window1.Observe()
                        .AssertChangeViewVariant(viewVariant),window1 => window1.Observe())
                    .SelectMany(frame => assert(frame.Observe())))
                .Catch<Unit,CannotNavigateException>(_ => Unit.Default.Observe())
                .ReplayFirstTake();

        public static IObservable<Unit> AssertReportsNavigation(this XafApplication application,Func<IObservable<Frame>,IObservable<Unit>> assert,IObservable<Unit> canNavigate)
            => application.AssertNavigation("ReportDataV2_ListView",null,assert,canNavigate);
        
        public static IObservable<Window> AssertNavigation(this XafApplication application, string viewId,Func<Window,IObservable<Unit>> navigate=null)
            => application.WhenMainWindowCreated().SelectMany(window => window.Navigate(viewId, window.Observe(),navigate).Cast<Window>().Assert($"{viewId}"));
        
        public static IObservable<(Frame listViewFrame, Frame detailViewFrame)> AssertProcessSelectedObject(this Frame frame) 
            => frame.Observe().SelectMany(frame1 => frame1.ProcessSelectedObject().Assert($"{frame.View.Id}"));
        
        public static IObservable<Frame> AssertCreateNewObject(this Frame window,bool inLine=false)
            => window.CreateNewObject(inLine).Select(frame => (frame.View.Id,t: frame)).Assert(t => t.Id).ToSecond();

        public static IObservable<(ITypeInfo typeInfo, object keyValue, Frame frame)> AssertSaveNewObject(this IObservable<Frame> source,Frame parentFrame)
            => source.SelectMany(frame => {
                var viewId = frame.View.Id;
                return frame.Observe().WhenSaveObject(parentFrame)
                    .SelectMany(t => t.frame.Application.CreateObjectSpace().Use(space => space.GetObjectByKey(t.typeInfo.Type,t.keyValue).Observe().WhenNotDefault()).To(t))
                    .Assert(viewId);
            });

        static IObservable<Frame> AssertDeleteObject(this IObservable<(ITypeInfo typeInfo, object keyValue, Frame frame,Frame parent)> source,Frame parentFrame,[CallerMemberName]string caller="")
            => source.SelectMany(t => t.Observe().WhenDeleteObject(parentFrame)
                    .SelectMany(frame => frame.Application.CreateObjectSpace()
                        .Use(space => space.GetObjectByKey(t.typeInfo.Type,t.keyValue).Observe().WhenDefault()).To(frame)))
                .Assert(caller);

        public static IObservable<Frame> AssertCreateNewObject(this IObservable<Frame> source,bool inLineEdit=false)
            => source.SelectMany(window => window.AssertCreateNewObject(inLineEdit));
        
        static IObservable<SingleChoiceAction> AssertSingleChoiceActionItems(
            this SingleChoiceAction action, ChoiceActionItem[] source, int itemsCount,Func<ChoiceActionItem,int> nestedItemsCountSelector=null,[CallerMemberName]string caller="") 
            => source.Active().ToArray().Observe().Where(items => items.Length==itemsCount||itemsCount==-1)
                .Assert($"{action.Id} has {source.Active().ToArray().Length} items ({source.StringJoin(", ")}) but should have {itemsCount}",caller:caller)
                .IgnoreElements().SelectMany()
                .Concat(source.Active().ToArray().ToNowObservable())
                .SelectMany(item => {
                    var count = nestedItemsCountSelector?.Invoke(item) ?? -1;
                    return count > -1 ? action.AssertSingleChoiceActionItems(item.Items.Active().ToArray(), count) : Observable.Empty<SingleChoiceAction>();
                })
                .IgnoreElements();

        public static IObservable<SimpleAction> AssertSimpleAction(this IObservable<Frame> source, string actionId,Func<SimpleAction,bool> completeWhenNotAvailable=null,[CallerMemberName]string caller="")
            => source.SelectMany(frame => frame.AssertSimpleAction(actionId,completeWhenNotAvailable, caller));

        public static IObservable<SimpleAction> AssertSimpleAction(this Frame frame,string actionId,Func<SimpleAction,bool> completeWhenNotAvailable=null,[CallerMemberName]string caller="") 
            => frame.Actions<SimpleAction>(actionId).ToNowObservable()
                .SelectMany(action => !action.Available() && (completeWhenNotAvailable?.Invoke(action)??false) ? Observable.Empty<SimpleAction>()
                    : action.Observe().Assert($"{nameof(AssertSimpleAction)} {frame.View} {actionId}", caller: caller));

        public static IObservable<SimpleAction> AssertSimpleAction(this IObservable<DashboardView> source,string actionId,Func<SimpleAction,bool> completeWhenNotAvailable=null)
            => source.SelectMany(view => view.DashboardViewItems().Where(item => item.MasterViewItem()).ToNowObservable()
                    .SelectMany(item => item.Frame.AssertSimpleAction(actionId,completeWhenNotAvailable,item.ToString())))
                .ReplayFirstTake();

        public static IObservable<SingleChoiceAction> AssertNavigationItems(this XafApplication application,
            Func<SingleChoiceAction, ChoiceActionItem, int> itemsCount)
            => application.WhenWindowCreated(true).AssertSingleChoiceAction("ShowNavigationItem",itemsCount);

        public static IObservable<string[]> AssertNavigationViews<TUser>(this XafApplication application,Func<TUser,string[]> viewSelector) where TUser:ISecurityUser 
            => application.NavigationViews()
                .SelectMany(views => application.CreateObjectSpace().Use(space => viewSelector(space.CurrentUser<TUser>()).Observe())
                    .Where(departmentViews => departmentViews.SequenceEqual(views.OrderBy(view => view)))
                    .Assert());

        public static IObservable<SingleChoiceAction> AssertSingleChoiceAction(this IObservable<Frame> source,
            string actionId, Func<SingleChoiceAction,int> itemsCount = null) 
            => source.AssertSingleChoiceAction(actionId,(action, item) => item==null? itemsCount?.Invoke(action) ?? -1:-1);
        
        public static IObservable<SingleChoiceAction> AssertSingleChoiceAction(this IObservable<Frame> source,
            string actionId, Func<SingleChoiceAction,ChoiceActionItem, int> itemsCount = null) 
            => source.SelectMany(frame => frame.Actions<SingleChoiceAction>(actionId)
                    .Where(action => action.Available() || itemsCount != null && itemsCount(action, null) == -1).ToNowObservable()
                    .Assert($"{nameof(AssertSingleChoiceAction)} {actionId}")
                .SelectMany(action => {
                        var invoke = itemsCount?.Invoke(action, null) ?? -1;
                        return action.AssertSingleChoiceActionItems(action.Items.Active().ToArray(),
                            invoke, item => itemsCount?.Invoke(action, item) ?? -1).IgnoreElements().Concat(action.Observe());
                    }))
                .ReplayFirstTake();
        
        public static IObservable<Frame> AssertDashboardViewReportsAction(this IObservable<Frame> source, string actionId,
            Func<SingleChoiceAction,ChoiceActionItem, bool> itemSelector = null,
            Func<SingleChoiceAction,ChoiceActionItem, int> reportsCount = null)
            => source.DashboardViewItem(item => item.MasterViewItem()).ToFrame()
                .AssertSingleChoiceAction(actionId, reportsCount)
                .AssertReports(itemSelector)
                .IgnoreElements().To<Frame>().Concat(source).ReplayFirstTake();

        public static IObservable<Frame> AssertDashboardViewReportsAction(this IObservable<Frame> source, string actionId,
            Func<SingleChoiceAction,ChoiceActionItem, bool> itemSelector = null, Func<SingleChoiceAction,int> reportsCount = null)
            => source.AssertDashboardViewReportsAction(actionId,itemSelector,(action, item) => item!=null?-1:reportsCount?.Invoke(action)??-1);

        public static IObservable<SingleChoiceAction> AssertReports(this IObservable<SingleChoiceAction> source,Func<SingleChoiceAction,ChoiceActionItem,bool> itemSelector=null) 
            => source.Where(action => action.Available()).SelectMany(action => action.Items.Available()
                    .SelectManyRecursive(item => item.Items.Available()).ToNowObservable()
                    .Where(item => itemSelector?.Invoke(action, item) ?? true)
                    .SelectManySequential(item => action.Trigger(action.Controller.Frame.AssertReportExecution((item.Data ??item).ToString()), () => item)))
                .IgnoreElements().To<SingleChoiceAction>().Concat(source);

        public static IObservable<Unit> AssertReportExecution(this Frame frame, string report) 
            => frame.Application.GetRequiredService<IAssertReport>().Assert(frame, report);

        
        public static IObservable<DashboardViewItem> AssertMasterFrame(this IObservable<Frame> source,Func<DashboardViewItem, bool> masterItem=null) 
            => source.DashboardViewItem( masterItem)
                .Assert(item => $"{item?.Id}")
                .ReplayFirstTake();

        

        public static IObservable<Frame> AssertDashboardListViewEditViewHasObject(this IObservable<Frame> source,Func<Frame,IObservable<Frame>> detailView=null)
            =>source.SelectMany(frame => frame.DashboardViewItems<ListView>().ToNowObservable()
                    .SelectMany(view => view.EditView.AssertDetailViewHasObject().IgnoreElements().To<Frame>()
                        .ConcatDefer(() => detailView?.Invoke(view.EditFrame)?? Observable.Empty<Frame>()))).IgnoreElements().To<Frame>().Concat(source)
                .ReplayFirstTake();
        
        public static IObservable<Unit> AssertDetailViewHasObject(this DetailView detailView) 
            => detailView.WhenSelectionChanged()
                .Merge(detailView.WhenCurrentObjectChanged()).To(detailView).StartWith(detailView)
                .WhenNotDefault(view => view.CurrentObject)
                .Assert(_ => $"{detailView.Id}").ToUnit();

        public static IObservable<Unit> AssertDetailViewHasObject(this IObservable<Frame> source)
            => source.ToDetailView()
                .SelectMany(detailView => detailView.WhenSelectionChanged()
                    .Merge(detailView.WhenCurrentObjectChanged()).To(detailView).StartWith(detailView)
                    .WhenNotDefault(view => view.CurrentObject)
                    .Assert(_ => $"{detailView.Id}").ToUnit());

        public static IObservable<Frame> AssertListView(this IObservable<Frame> source, Func<Frame, IObservable<Unit>> assertExistingObjectDetailview = null, Func<Frame,AssertAction> assert=null,bool inlineEdit=false,Func<DashboardViewItem, bool> listViewFrameSelector=null,[CallerMemberName]string caller="") 
            => source.Select(frame => (frame, default(Frame))).AssertListView(assertExistingObjectDetailview,assert,inlineEdit,listViewFrameSelector:listViewFrameSelector,caller);

        private static IObservable<Frame> AssertListView(this IObservable<(Frame frame, Frame parent)> source,
            Func<Frame, IObservable<Unit>> assertExistingObjectDetailview = null, Func<Frame,AssertAction> assert = null, bool inlineEdit = false,Func<DashboardViewItem, bool> listViewFrameSelector=null,[CallerMemberName]string caller="") 
            => source.AssertListViewHasObjects( assert,listViewFrameSelector,caller).Finally(() => {})
                .SwitchIfEmpty(Observable.Defer(() => source.AssertListViewNotHasObjects(assert,listViewFrameSelector)))
                .ReplayFirstTake()
                .Select(t => t)
                .AssertProcessSelectedObject(assertExistingObjectDetailview, assert,listViewFrameSelector)
                .AssertCreateSaveAndDeleteObject( assert, inlineEdit,listViewFrameSelector,caller)
                .ReplayFirstTake();

        public static IObservable<object> AssertListViewHasObjects(this IObservable<Frame> source,[CallerMemberName]string caller="")
            => source.AssertObjectViewHasObjects(caller);
            
        public static IObservable<object> AssertObjectViewHasObjects(this IObservable<Frame> source,[CallerMemberName]string caller="")
            => source.SelectMany(frame => frame.AssertListViewHasObject(caller)).ToSecond();

        public static IObservable<(string msg, object t)> AssertListViewHasObject(this Frame frame,[CallerMemberName]string caller="")
            => frame.Observe().WhenObjects(1).Take(1).Select(instance => (msg:$"{frame.View.Id} {instance}", t: instance)).Assert(t => t.msg,caller:caller);

        public static IObservable<Frame> AssertNestedListView(this Frame detailViewFrame, Type objectType,
            Func<Frame, IObservable<Unit>> existingObjectDetailview = null, Func<Frame,AssertAction> assert = null,bool inlineEdit=false, [CallerMemberName] string caller = "") 
            => detailViewFrame.NestedListViews(objectType)
                .Assert($"{nameof(AssertNestedListView)} {objectType.Name}",caller:caller)
                .Select(editor => (editor.Frame, detailViewFrame))
                .AssertListView(existingObjectDetailview, assert,inlineEdit,caller:caller);

        
        private static IObservable<(Frame frame, Frame parent)> AssertListViewHasObjects(
            this IObservable<(Frame frame, Frame parent)> source, Func<Frame, AssertAction> assert,Func<DashboardViewItem, bool> listViewFrameSelector=null, [CallerMemberName] string caller = "") 
            => source.MasterFrame(listViewFrameSelector).ToFirst()
                .SelectMany(frame => frame.Assert(assert).HasFlag(AssertAction.HasObject) ? frame.Observe().AssertListViewHasObjects(caller)
                    .SelectMany(_ => source) : source).ReplayFirstTake().Select(t => t);
        
        static AssertAction Assert(this Frame frame,Func<Frame, AssertAction> assert,AssertAction assertAction=AssertAction.All) 
            => assert?.Invoke(frame)??assertAction;

        private static IObservable<(Frame frame, Frame parent)> AssertProcessSelectedObject(this IObservable<(Frame frame, Frame parent)> source,
            Func<Frame, IObservable<Unit>> assertDetailview, Func<Frame,AssertAction> assertActionSelector,Func<DashboardViewItem, bool> listViewFrameSelector=null) 
            => source.MasterFrame(listViewFrameSelector).SelectMany(t => t.frame.Assert(assertActionSelector).HasFlag(AssertAction.Process)
                    ? t.frame.AssertProcessSelectedObject().ToSecond()
                        .ConcatIgnored(frame1 => assertDetailview?.Invoke(frame1) ?? Observable.Empty<Unit>())
                        .CloseWindow(t.frame).Select(frame => (frame, t.parent))
                    : t.Observe())
                .ReplayFirstTake();
        
        
        private static IObservable<(Frame frame, Frame parent)> AssertListViewNotHasObjects(this IObservable<(Frame frame, Frame parent)> source, Func<Frame,AssertAction> assert,Func<DashboardViewItem, bool> listViewFrameSelector=null) 
            => source.MasterFrame(listViewFrameSelector).ToFirst().SelectMany(frame => frame.Assert(assert, AssertAction.NotHasObject).HasFlag(AssertAction.NotHasObject)
                    ? frame.WhenObjects().Assert().SelectMany(_ => new Exception($"{frame.View} has objects").ThrowTestException().To<(Frame frame, Frame parent)>()) : source)
                .ReplayFirstTake();

        private static IObservable<Frame> AssertCreateSaveAndDeleteObject(
            this IObservable<(Frame frame, Frame parent)> source, Func<Frame, AssertAction> assert, bool inlineEdit,
            Func<DashboardViewItem, bool> listViewFrameSelector = null,[CallerMemberName]string caller="") 
            => source.MasterFrame(listViewFrameSelector)
                .SelectMany(t => t.frame.Observe().AssertCreateNewObject(assert, inlineEdit)
                    .AssertSaveNewObject(assert,t.frame).Select(t1 => (t1.typeInfo,t1.keyValue,t1.frame.MasterFrame(listViewFrameSelector),t.parent))
                    .AssertDeleteObject(assert,inlineEdit,t.frame,caller)
                    .SwitchIfEmpty(t.AssertListViewDeleteOnly(assert, inlineEdit, caller))
                    .ReplayFirstTake())
                .SwitchIfEmpty(source.ToFirst())
                .ReplayFirstTake();

        private static IObservable<Frame> AssertListViewDeleteOnly(this (Frame frame, Frame parent) source,Func<Frame, AssertAction> assert, bool inlineEdit, string caller) 
            => Observable.Defer(() => source.frame.Assert(assert).HasFlag(AssertAction.ListViewDeleteOnly).Observe().WhenNotDefault().Select(_ => source.frame.View)
                .SelectMany(view => view.WhenObjectViewObjects(1).Take(1)
                    .SelectMany(o => (view.ObjectTypeInfo, view.ObjectSpace.GetKeyValue(o), source.frame, source.parent).Observe()
                        .AssertDeleteObject(assert, inlineEdit, source.frame, caller))));

        private static IObservable<(Frame frame, Frame parent)> AssertCreateNewObject(this IObservable<Frame> source, 
            Func<Frame,AssertAction> assert, bool inlineEdit) 
            => source.SelectMany(frame => frame.Assert(assert).HasFlag(AssertAction.ListViewNew)
                    ? frame.Observe().AssertCreateNewObject(inlineEdit).Select(frame1 => (frame: frame1, parent: frame))
                    : Observable.Empty<(Frame frame, Frame parent)>()).ReplayFirstTake();
        
        
        private static IObservable<Frame> AssertDeleteObject(this IObservable<(ITypeInfo typeInfo, object keyValue, Frame frame, Frame parent)> source, Func<Frame, AssertAction> assert,
            bool inlineEdit,Frame parentFrame, [CallerMemberName] string caller = "") 
            => source.SelectMany(t => {
                    var assertAction = t.frame.Assert(assert);
                    return assertAction.HasFlag(AssertAction.ListViewDelete) || assertAction == AssertAction.ListViewDeleteOnly
                        ? t.Observe().AssertDeleteObject(parentFrame, caller) : t.Observe().WhenDefault(_ => inlineEdit).Select(t1 => t1.frame);
                })
                .ReplayFirstTake();

        private static IObservable<(ITypeInfo typeInfo, object keyValue, Frame frame)> AssertSaveNewObject(
            this IObservable<(Frame frame, Frame parent)> source, Func<Frame, AssertAction> assert, Frame parentFrame)
            => source.SelectMany(t => {
                var assertAction = t.frame.Assert(assert);
                return assertAction.HasFlag(AssertAction.DetailViewSave) ? t.frame.Observe().AssertSaveNewObject(parentFrame)
                    : t.frame.Observe().CloseWindow(t.parent).Select(frame1 => (frame1.View.ObjectTypeInfo, (object)null, frame1));
            }).ReplayFirstTake();

        public static IObservable<Unit> AssertMapsControl(this DetailView detailView)
            => detailView.ObjectSpace.GetRequiredService<IAssertMapControl>().Assert(detailView);
    }


    public class CannotNavigateException:Exception{
        
    }

    [Flags]
    public enum AssertAction{
        HasObject = 1 << 0,
        NotHasObject = 1 << 5,
        Process = (1 << 1) | HasObject,
        ListViewNew = (1 << 2),
        DetailViewSave = (1 << 3) | ListViewNew,
        ListViewDelete = (1 << 4) | DetailViewSave,
        ListViewDeleteOnly = 1<<6|HasObject,
        All = ListViewDelete | ListViewNew | DetailViewSave | Process | HasObject,
        AllButDelete = HasObject | Process | ListViewNew | DetailViewSave,
    }
}