import {ConstantHelper, test} from '@umbraco/playwright-testhelpers';
import {expect} from "@playwright/test";

let documentTypeId = '';
let contentId = '';
const contentName = 'TestContent';
const documentTypeName = 'DocumentTypeForContent';
const childDocumentTypeName = 'ChildDocumentType';
const decimalDataTypeName = 'Label (decimal)';
const groupName = 'TestGroup';


test.beforeEach(async ({umbracoApi}) => {
  await umbracoApi.document.ensureNameNotExists(contentName);
  await umbracoApi.documentType.ensureNameNotExists(documentTypeName);
});

test.afterEach(async ({umbracoApi}) => {
  await umbracoApi.document.ensureNameNotExists(contentName);
  await umbracoApi.documentType.ensureNameNotExists(childDocumentTypeName);
});

test('can save content with a decimal label', {tag: '@smoke'}, async ({page, umbracoApi, umbracoUi}) => {
  // Arrange
  const dataType = await umbracoApi.dataType.getByName(decimalDataTypeName);
  documentTypeId = await umbracoApi.documentType.createDocumentTypeWithPropertyEditor(documentTypeName, decimalDataTypeName, dataType.id, groupName);
  contentId = await umbracoApi.document.createDefaultDocument(contentName, documentTypeId);
  await umbracoUi.goToBackOffice();
  await umbracoUi.content.goToSection(ConstantHelper.sections.content);

  // Act

  // Assert

});

test('can publish content with a decimal label', {tag: '@smoke'}, async ({page, umbracoApi, umbracoUi}) => {
  // Arrange
  const dataType = await umbracoApi.dataType.getByName(decimalDataTypeName);
  documentTypeId = await umbracoApi.documentType.createDocumentTypeWithPropertyEditor(documentTypeName, decimalDataTypeName, dataType.id, groupName);
  contentId = await umbracoApi.document.createDefaultDocument(contentName, documentTypeId);
  await umbracoUi.goToBackOffice();
  await umbracoUi.content.goToSection(ConstantHelper.sections.content);

  // Act

  // Assert

});

test('can update content with a decimal label', {tag: '@smoke'}, async ({page, umbracoApi, umbracoUi}) => {
  // Arrange
  const dataType = await umbracoApi.dataType.getByName(decimalDataTypeName);
  documentTypeId = await umbracoApi.documentType.createDocumentTypeWithPropertyEditor(documentTypeName, decimalDataTypeName, dataType.id, groupName);
  contentId = await umbracoApi.document.createDefaultDocument(contentName, documentTypeId);
  await umbracoUi.goToBackOffice();
  await umbracoUi.content.goToSection(ConstantHelper.sections.content);

  // Act

  // Assert

});
