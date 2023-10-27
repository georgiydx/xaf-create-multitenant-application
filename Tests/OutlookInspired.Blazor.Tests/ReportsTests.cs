﻿using System.Reactive.Linq;
using OutlookInspired.Tests.Common;
using OutlookInspired.Tests.Services;
using TestBase = OutlookInspired.Blazor.Tests.Common.TestBase;

namespace OutlookInspired.Blazor.Tests{
    
    public class ReportsTests : TestBase{
        
        [RetryTestCaseSource(nameof(CustomerVariants),MaxTries=MaxTries)]
        public async Task Customer(string user,string view,string viewVariant){
            await StartTest(user, application => application.AssertCustomerReports(view, viewVariant));
        }
        [RetryTestCaseSource(nameof(ProductVariants),MaxTries=MaxTries)]
        public async Task Product(string user,string view,string viewVariant){
            await StartTest(user, application => application.AssertProductReports(view, viewVariant));
        }
        [RetryTestCaseSource(nameof(OrderVariants),MaxTries=MaxTries)]
        public async Task Order(string user,string view,string viewVariant){
            await StartTest(user, application => application.AssertOrderReports(view, viewVariant));
        }
    }
}