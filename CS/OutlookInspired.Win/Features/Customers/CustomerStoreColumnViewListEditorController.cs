﻿using DevExpress.ExpressApp;
using OutlookInspired.Module.BusinessObjects;
using OutlookInspired.Win.Editors.GridListEditor;

namespace OutlookInspired.Win.Features.Customers{
    public class CustomerStoreColumnViewListEditorController:ObjectViewController<ListView,CustomerStore>{
        protected override void OnActivated(){
            base.OnActivated();
            if (View.Editor is not ColumnViewListEditor listEditor) return;
            listEditor.ColumnViewControlCreating+=ListEditorOnColumnViewControlCreating;
        }

        protected override void OnDeactivated(){
            base.OnDeactivated();
            if (View.Editor is not ColumnViewListEditor listEditor) return;
            listEditor.ColumnViewControlCreating-=ListEditorOnColumnViewControlCreating;
        }

        private void ListEditorOnColumnViewControlCreating(object sender, ColumnViewControlCreatingArgs e) 
            => e.Control =new CustomerStoreView();
    }
}