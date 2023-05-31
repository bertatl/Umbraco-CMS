import type { ManifestPropertyEditorModel } from '@umbraco-cms/backoffice/extension-registry';

export const manifest: ManifestPropertyEditorModel = {
	type: 'propertyEditorModel',
	name: 'Tags',
	alias: 'Umbraco.Tags',
	meta: {
		defaultPropertyEditorUiAlias: 'Umb.PropertyEditorUi.Tags',
		settings: {
			properties: [
				{
					alias: 'group',
					label: 'Define a tag group',
					description: '',
					propertyEditorUiAlias: 'Umb.PropertyEditorUi.TextBox',
				},
				{
					alias: 'storageType',
					label: 'Storage Type',
					description: '',
					propertyEditorUiAlias: 'Umb.PropertyEditorUi.Tags.StorageType',
				},
			],
			defaultData: [
				{
					alias: 'group',
					value: 'default',
				},
				{
					alias: 'storageType',
					value: 'Json',
				},
			],
		},
	},
};
