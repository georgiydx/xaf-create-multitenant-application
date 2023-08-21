﻿using System.Collections;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using DevExpress.XtraGrid.Views.Base;
using OutlookInspired.Module.Controllers;
using OutlookInspired.Module.Services;
using OutlookInspired.Win.Extensions;

namespace OutlookInspired.Win.UserControls
{
    public partial class ColumnViewUserControl : UserControl, IUserControl
    {
        private EFCoreObjectSpace _objectSpace;
        protected ColumnView ColumnView;
        protected IList DataSource;
        private string _criteria;

        public event EventHandler CurrentObjectChanged;
        public event EventHandler SelectionChanged;
        public event EventHandler SelectionTypeChanged;
        public event EventHandler ProcessObject;

        public ColumnViewUserControl(){
            Disposed+=OnDisposed;
        }

        private void OnDisposed(object sender, EventArgs e){
            
        }

        public void SetCriteria(string criteria){
            _criteria = criteria;
            Refresh();
        }

        public virtual void Refresh(object currentObject) => Refresh();

        public void Setup(IObjectSpace objectSpace, XafApplication application){
            _objectSpace = (EFCoreObjectSpace)objectSpace;
            ColumnView = this.ColumnView();
            ColumnView.SelectionChanged += (_, _) => {
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                CurrentObjectChanged?.Invoke(this, EventArgs.Empty);
            };
            ColumnView.DoubleClick += (_, _) => ProcessObject?.Invoke(this, EventArgs.Empty);
            ColumnView.ColumnFilterChanged += (_, _) => OnDataSourceOfFilterChanged();
            ColumnView.DataSourceChanged += (_, _) => OnDataSourceOfFilterChanged();
            Refresh();
        }

        public override void Refresh() 
            => ColumnView.GridControl.DataSource = (object)DataSource ?? _objectSpace.NewEntityServerModeSource(GetObjectType(), _criteria);

        protected virtual Type GetObjectType() => throw new NotImplementedException();
        public object CurrentObject => ColumnView.FocusedRowObject( _objectSpace,GetObjectType());

        public IList SelectedObjects => ColumnView.GetSelectedRows().Select(i => ColumnView.GetRow(i)).ToArray();
        public SelectionType SelectionType => SelectionType.Full;
        public bool IsRoot => false;

        protected virtual void OnDataSourceOfFilterChanged(){
        }

        protected virtual void OnSelectionTypeChanged() => SelectionTypeChanged?.Invoke(this, EventArgs.Empty);
    }
}
