﻿using Umbraco.Cms.Core.Models.PublishedContent;

namespace Umbraco.Cms.Infrastructure.HybridCache.Factories;

internal interface IPublishedContentFactory
{
    IPublishedContent? ToIPublishedContent(ContentCacheNode contentCacheNode, bool preview);
}
