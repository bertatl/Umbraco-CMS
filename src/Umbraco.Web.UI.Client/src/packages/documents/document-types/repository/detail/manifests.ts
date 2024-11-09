export const UMB_DOCUMENT_TYPE_DETAIL_REPOSITORY_ALIAS = 'Umb.Repository.DocumentType.Detail';
export const UMB_DOCUMENT_TYPE_DETAIL_STORE_ALIAS = 'Umb.Store.DocumentType.Detail';

export const manifests: Array<UmbExtensionManifest> = [
	{
		type: 'repository',
		alias: UMB_DOCUMENT_TYPE_DETAIL_REPOSITORY_ALIAS,
		name: 'Document Types Repository',
		api: () => import('./document-type-detail.repository.js'),
	},
	{
		type: 'store',
		alias: UMB_DOCUMENT_TYPE_DETAIL_STORE_ALIAS,
		name: 'Document Type Store',
		api: () => import('./document-type-detail.store.js'),
	},
];