﻿namespace Umbraco.Cms.Api.Management.ViewModels.Tree;

public abstract class EntityTreeItemResponseModel : TreeItemPresentationModel, INamedEntityPresentationModel
{
    public Guid Id { get; set; }

    public bool IsContainer { get; set; }

    public Guid? ParentId { get; set; }
    public abstract string Type { get; }
}