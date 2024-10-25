﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Office;
using DevExpress.ExpressApp.Utils;
using DevExpress.Office.Services;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using OutlookInspired.Module.Attributes;
using OutlookInspired.Module.Features;
using OutlookInspired.Module.Features.CloneView;
using OutlookInspired.Module.Features.Maps;


namespace OutlookInspired.Module.BusinessObjects{
    [XafDefaultProperty(nameof(InvoiceNumber))]
    [CloneView(CloneViewType.DetailView, ChildDetailView)]
    [CloneView(CloneViewType.DetailView, GridViewDetailView)]
    [CloneView(CloneViewType.DetailView, MapsDetailView)]
    [CloneView(CloneViewType.DetailView, InvoiceDetailView)]
    [CloneView(CloneViewType.ListView, ListViewDetail)]
    [ImageName("BO_Order")][VisibleInReports(true)]
    public class Order :OutlookInspiredBaseObject, IViewFilter,IRouteMapsMarker{
        public const string MapsDetailView = "Order_DetailView_Maps";
        public const string InvoiceDetailView = "Order_Invoice_DetailView";
        public const string ChildDetailView = "Order_DetailView_Child";
        public const string GridViewDetailView = "OrderGridView_DetailView";
        public const string ListViewDetail = "Order_ListView_Detail";
        
        [XafDisplayName("Invoice #")]
        [FontSizeDelta(4)][MaxLength(100)]
        public  virtual string InvoiceNumber { get; set; }
        
        public virtual Customer Customer { get; set; }
        public virtual CustomerStore Store { get; set; }
        [MaxLength(100)]
        public  virtual string PONumber { get; set; }
        public virtual Employee Employee { get; set; }
        public  virtual DateTime OrderDate { get; set; }
        [Column(TypeName = CurrencyType)]
        public  virtual decimal SaleAmount { get; set; }
        [Column(TypeName = CurrencyType)]
        public  virtual decimal ShippingAmount { get; set; }
        [Column(TypeName = CurrencyType)]
        public  virtual decimal TotalAmount { get; set; }

        [Browsable(false)]
        public virtual int Year => OrderDate.Year;
        public  virtual DateTime? ShipDate { get; set; }
        public  virtual OrderShipMethod ShipMethod { get; set; }
        [EditorAlias(EditorAliases.DxHtmlPropertyEditor)]
        public  virtual byte[] OrderTerms { get; set; }
        [Aggregated]
        public virtual ObservableCollection<OrderItem> OrderItems{ get; set; } = new();
        public  virtual ShipmentCourier ShipmentCourier { get; set; }
        [EditorAlias(EditorAliases.EnumImageOnlyEditor)]
        public  virtual ShipmentStatus ShipmentStatus { get; set; }

        [VisibleInDetailView(false)]
        [XafDisplayName(nameof(ShipmentStatus))]
        [ImageEditor(ListViewImageEditorMode = ImageEditorMode.PictureEdit,
            DetailViewImageEditorMode = ImageEditorMode.PictureEdit,ImageSizeMode = ImageSizeMode.Zoom)]
        public virtual byte[] ShipmentStatusImage => ImageLoader.Instance.GetEnumValueImageInfo(@ShipmentStatus).ImageBytes;

        [EditorAlias(EditorAliases.PdfViewerEditor)]
        [VisibleInDetailView(false)]
        [NotMapped]
        public virtual byte[] ShipmentDetail{ get; set; } = [];
        
        
        [EditorAlias(EditorAliases.PdfViewerEditor)]
        [VisibleInDetailView(false)]
        [NotMapped]
        public virtual byte[] InvoiceDocument{ get; set; } = [];
        [EditorAlias(EditorAliases.DxHtmlPropertyEditor)]
        public  virtual byte[] Comments { get; set; }
        [Column(TypeName = CurrencyType)]
        public  virtual decimal RefundTotal { get; set; }
        [Column(TypeName = CurrencyType)]
        public  virtual decimal PaymentTotal { get; set; }
        [EditorAlias(EditorAliases.EnumImageOnlyEditor)]
        public PaymentStatus PaymentStatus 
            => PaymentTotal == decimal.Zero && RefundTotal == decimal.Zero ? PaymentStatus.Unpaid :
                RefundTotal == TotalAmount ? PaymentStatus.RefundInFull :
                PaymentTotal == TotalAmount ? PaymentStatus.PaidInFull : PaymentStatus.Other;

        [VisibleInDetailView(false)]
        [XafDisplayName(nameof(ShipmentStatus))]
        [ImageEditor(ListViewImageEditorMode = ImageEditorMode.PictureEdit,
            DetailViewImageEditorMode = ImageEditorMode.PictureEdit,ImageSizeMode = ImageSizeMode.Zoom)]
        public byte[] PaymentStatusImage => ImageLoader.Instance.GetEnumValueImageInfo(PaymentStatus).ImageBytes;
        
        public double ActualWeight 
            => OrderItems == null ? 0 : OrderItems.Where(item => item.Product != null)
                    .Sum(item => item.Product.Weight * item.ProductUnits);

        [EditorAlias(EditorAliases.MapHomeOfficePropertyEditor)]
        public Location Location => new(){Latitude = ((IBaseMapsMarker)this).Latitude, Longitude = ((IBaseMapsMarker)this).Longitude };
        string IBaseMapsMarker.Title => Store?.Customer.Name;

        double IBaseMapsMarker.Latitude => Store?.Latitude??0;

        double IBaseMapsMarker.Longitude => Store?.Longitude??0;

        public byte[] MailMergeInvoice()
            => MailMergeInvoice(CreateDocumentServer(ObjectSpace.GetObjectsQuery<RichTextMailMergeData>()
                .FirstOrDefault(data => data.Name == "Order")!
                .Template, this));
        
        byte[] MailMergeInvoice(IRichEditDocumentServer richEditDocumentServer){
            richEditDocumentServer.CalculateDocumentVariable += (_, e) => CalculateDocumentVariable(e,richEditDocumentServer);
            return MailMerge(richEditDocumentServer,this);
        }
        
        byte[] MailMerge<T>( IRichEditDocumentServer documentServer,params T[] datasource){
            using var stream = new MemoryStream();
            documentServer.GetService<IUriStreamService>().RegisterProvider(new ImageStreamProviderBase(
                documentServer.Options.MailMerge, datasource, XafTypesInfo.Instance.FindTypeInfo(typeof(T))));
            documentServer.MailMerge(documentServer.CreateMailMergeOptions(), stream, DocumentFormat.OpenXml);
            return stream.ToArray();
        }

        void MailMerge<T>( IRichEditDocumentServer documentServer,IRichTextMailMergeData mailMergeData, MergeMode mergeMode,params T[] dataSource){
            using var mergedServer = CreateDocumentServer(mailMergeData.Template,dataSource);
            using var memoryStream = new MemoryStream(mailMergeData.Template);
            mergedServer.LoadDocumentTemplate(memoryStream, DocumentFormat.OpenXml);
            mergedServer.Options.MailMerge.DataSource = dataSource;
            var options = mergedServer.Document.CreateMailMergeOptions();
            options.MergeMode = mergeMode;
            mergedServer.MailMerge(options, documentServer.Document);
        }

        void CalculateDocumentVariable( CalculateDocumentVariableEventArgs e,IRichEditDocumentServer richEditDocumentServer){
            switch (e.VariableName){
                case nameof(OrderItems):
                    MailMerge(richEditDocumentServer,
                        ObjectSpace.GetObjectsQuery<RichTextMailMergeData>().FirstOrDefault(data => data.Name=="OrderItem"), MergeMode.JoinTables,
                        OrderItems.ToArray());
                    e.PreserveInsertedContentFormatting = true;
                    e.KeepLastParagraph = false;
                    e.Value = richEditDocumentServer;
                    e.Handled = true;
                    break;
                case "Total":
                    e.Value = OrderItems.Sum(item => item.Total);
                    e.Handled = true;
                    break;
                case "TotalDue":
                    e.Value = OrderItems.Sum(item => item.Total) + ShippingAmount;
                    e.Handled = true;
                    break;
            }
        }
        
        static RichEditDocumentServer CreateDocumentServer(byte[] bytes, params object[] dataSource) 
            => new(){
                OpenXmlBytes = bytes,
                Options ={
                    MailMerge ={
                        DataSource = dataSource
                    }
                }
            };


    }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OrderShipMethod {
        Ground, Air
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ShipmentCourier {
        None, FedEx, UPS, DHL
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ShipmentStatus {
        [ImageName("ShipmentAwaiting")]
        Awaiting,
        [ImageName("ShipmentTransit")]
        Transit,
        [ImageName("ShipmentReceived")]
        Received
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PaymentStatus {
        [ImageName("PaymentUnPaid")]
        Unpaid, 
        [ImageName("PaymentPaid")]
        PaidInFull, 
        [ImageName("PaymentRefund")]
        RefundInFull,
        Other
    }
}