// Copyright (c) Umbraco.
// See LICENSE for more details.

using System;
using System.Collections.Generic;
using Moq;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Services;
using System.Reflection;
using Moq;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Cms.Tests.Common.Builders.Interfaces;

namespace Umbraco.Cms.Tests.Common.Builders
{
    public class DataEditorBuilder<TParent>
        : ChildBuilderBase<TParent, IDataEditor>,
            IWithAliasBuilder,
            IWithNameBuilder
    {
        private string _alias;
        private string _name;
        private readonly ConfigurationEditorBuilder<DataEditorBuilder<TParent>> _explicitConfigurationEditorBuilder;
        private readonly DataValueEditorBuilder<DataEditorBuilder<TParent>> _explicitValueEditorBuilder;
        private IDictionary<string, object> _defaultConfiguration;

        public DataEditorBuilder(TParent parentBuilder)
            : base(parentBuilder)
        {
            _explicitConfigurationEditorBuilder = new ConfigurationEditorBuilder<DataEditorBuilder<TParent>>(this);
            _explicitValueEditorBuilder = new DataValueEditorBuilder<DataEditorBuilder<TParent>>(this);
        }

        public DataEditorBuilder<TParent> WithDefaultConfiguration(IDictionary<string, object> defaultConfiguration)
        {
            _defaultConfiguration = defaultConfiguration;
            return this;
        }

        public ConfigurationEditorBuilder<DataEditorBuilder<TParent>> AddExplicitConfigurationEditorBuilder() =>
            _explicitConfigurationEditorBuilder;

        public DataValueEditorBuilder<DataEditorBuilder<TParent>> AddExplicitValueEditorBuilder() =>
            _explicitValueEditorBuilder;

        public override IDataEditor Build()
        {
            var name = _name ?? Guid.NewGuid().ToString();
            var alias = _alias ?? name.ToCamelCase();

            IDictionary<string, object> defaultConfiguration = _defaultConfiguration ?? new Dictionary<string, object>();
            IConfigurationEditor explicitConfigurationEditor = _explicitConfigurationEditorBuilder.Build();
            IDataValueEditor explicitValueEditor = _explicitValueEditorBuilder.Build();

            var dataEditor = new DataEditor(
                Mock.Of<IDataValueEditorFactory>(),
                EditorType.PropertyValue)
            {
                Alias = alias,
                DefaultConfiguration = defaultConfiguration
            };

            // Use reflection to set the properties that might have protected setters
            typeof(DataEditor).GetProperty("Name").SetValue(dataEditor, name);
            typeof(DataEditor).GetProperty("View").SetValue(dataEditor, "~/App_Plugins/MyCustomEditor/editor.html");

// Set the configuration editor and value editor using reflection
            typeof(DataEditor).GetProperty("ConfigurationEditor").SetValue(dataEditor, explicitConfigurationEditor);
            typeof(DataEditor).GetProperty("Editor").SetValue(dataEditor, explicitValueEditor);

            return dataEditor;
        }

        string IWithAliasBuilder.Alias
        {
            get => _alias;
            set => _alias = value;
        }

        string IWithNameBuilder.Name
        {
            get => _name;
            set => _name = value;
        }
    }
}