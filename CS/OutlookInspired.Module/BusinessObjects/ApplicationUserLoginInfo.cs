﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Validation;

namespace OutlookInspired.Module.BusinessObjects;

[Table("PermissionPolicyUserLoginInfo")]
public class ApplicationUserLoginInfo : ISecurityUserLoginInfo {
    [Browsable(false)]
    public virtual Guid ID { get; protected set; }
    [Appearance("PasswordProvider", Enabled = false, Criteria = "!(IsNewObject(this)) and LoginProviderName == '" + SecurityDefaults.PasswordAuthentication + "'", Context = "DetailView")]
    [MaxLength(100)]
    public virtual string LoginProviderName { get; set; }
    [Appearance("PasswordProviderUserKey", Enabled = false, Criteria = "!(IsNewObject(this)) and LoginProviderName == '" + SecurityDefaults.PasswordAuthentication + "'", Context = "DetailView")]
    [MaxLength(255)]
    public virtual string ProviderUserKey { get; set; }
    [Browsable(false)]
    public virtual Guid UserForeignKey { get; set; }
    [RuleRequiredField]
    [ForeignKey(nameof(UserForeignKey))]
    public virtual ApplicationUser User { get; set; }
    object ISecurityUserLoginInfo.User => User;
}
