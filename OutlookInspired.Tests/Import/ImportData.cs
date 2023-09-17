using System.Reactive.Linq;
using DevExpress.ExpressApp;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OutlookInspired.Module.BusinessObjects;
using Shouldly;
using XAF.Testing;
using XAF.Testing.XAF;

namespace OutlookInspired.Tests.ImportData.Import{
    public class ImportData:TestBase{
        [Test]
        public async Task Test(){
            
            using var application = await SetupWinApplication(application => {
                var ensureDeletedAsync = application.ServiceProvider.GetRequiredService<OutlookInspiredEFCoreDbContext>().Database
                    .EnsureDeletedAsync();
                return ensureDeletedAsync;
                return Task.CompletedTask;
            },useServer:false);
            
            
            using var objectSpace = application.ObjectSpaceProvider.CreateObjectSpace();
            await objectSpace.ImportFromSqlLite();
            objectSpace.CommitChanges();
            objectSpace.Count<Crest>().ShouldBe(20);
            objectSpace.Count<State>().ShouldBe(51);
            objectSpace.Count<Customer>().ShouldBe(20);
            objectSpace.Count<Picture>().ShouldBe(112);
            objectSpace.Count<Probation>().ShouldBe(4);
            objectSpace.Count<CustomerStore>().ShouldBe(200);
            objectSpace.Count<Employee>().ShouldBe(51);
            objectSpace.Count<ProductImage>().ShouldBe(76);
            objectSpace.Count<ProductCatalog>().ShouldBe(19);
            objectSpace.Count<Evaluation>().ShouldBe(127);
            objectSpace.Count<Product>().ShouldBe(19);
            objectSpace.Count<CustomerCommunication>().ShouldBe(1);
            objectSpace.Count<EmployeeTask>().ShouldBe(220);
            objectSpace.Count<TaskAttachedFile>().ShouldBe(84);
            objectSpace.Count<CustomerEmployee>().ShouldBe(600);
            objectSpace.Count<Order>().ShouldBe(4720);
            objectSpace.Count<OrderItem>().ShouldBe(14440);
            objectSpace.Count<Quote>().ShouldBe(8788);
            objectSpace.Count<QuoteItem>().ShouldBe(26859);
            
            // objectSpace.GenerateOrders();
        }

        
    }
}