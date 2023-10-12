﻿using Umbraco.Cms.Api.Management.ViewModels.Item;
using Umbraco.Cms.Core;

namespace Umbraco.Cms.Api.Management.ViewModels.MemberType.Items;

public class MemberTypeItemResponseModel : ItemResponseModelBase
{
    public string? Icon { get; set; }
    public override string Type => Constants.UdiEntityType.MemberType;
}