﻿using System.Collections.ObjectModel;
using System.Linq.Expressions;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using OutlookInspired.Module.BusinessObjects;

namespace OutlookInspired.Module.Features.Maps{
    public interface IRouteMapsMarker:IMapsMarker{
        
    }

    public interface ISalesMapsMarker:IMapsMarker,IObjectSpaceLink{
        ObservableCollection<MapItem> Sales{ get;  }
        ObservableCollection<MapItem> CitySales{ get; set; }
        IEnumerable<Order> Orders{ get; }
        
        // Expression<Func<OrderItem,bool>> SalesExpression{ get; }
        Period SalesPeriod{ get; set; }
        
    }
}   