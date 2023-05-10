import { UmbUserGroupRepository } from '../repository/user-group.repository';
import { UmbEntityWorkspaceContextInterface, UmbWorkspaceContext } from '@umbraco-cms/backoffice/workspace';
import { UmbControllerHostElement } from '@umbraco-cms/backoffice/controller';
import { UserGroupPresentationModel } from '@umbraco-cms/backoffice/backend-api';
import { UmbObjectState } from '@umbraco-cms/backoffice/observable-api';

export class UmbUserGroupWorkspaceContext
	extends UmbWorkspaceContext<UmbUserGroupRepository, UserGroupPresentationModel>
	implements UmbEntityWorkspaceContextInterface<UserGroupPresentationModel | undefined>
{
	#data = new UmbObjectState<UserGroupPresentationModel | undefined>(undefined);
	data = this.#data.asObservable();

	constructor(host: UmbControllerHostElement) {
		super(host, new UmbUserGroupRepository(host));
	}

	async createScaffold() {
		const { data } = await this.repository.createScaffold(null);
		this.setIsNew(true);
		// TODO: Should the data be the base model or the presentation model?
		this.#data.next(data as unknown as UserGroupPresentationModel);
		return { data };
	}

	async load(id: string) {
		const { data } = await this.repository.requestById(id);
		if (data) {
			this.setIsNew(false);
			this.#data.update(data);
		}
	}

	getEntityId(): string | undefined {
		throw new Error('Method not implemented.');
	}
	getEntityType(): string {
		throw new Error('Method not implemented.');
	}
	getData(): UserGroupPresentationModel | undefined {
		throw new Error('Method not implemented.');
	}
	async save() {
		if (!this.#data.value) return;

		//TODO: Could we clean this code up?
		if (this.getIsNew()) {
			await this.repository.create(this.#data.value);
		} else if (this.#data.value.id) {
			await this.repository.save(this.#data.value.id, this.#data.value);
		} else return;

		// If it went well, then its not new anymore?.
		this.setIsNew(false);
	}

	destroy(): void {
		this.#data.complete();
	}

	async delete(id: string) {
		await this.repository.delete(id);
	}

	updateProperty<Alias extends keyof UserGroupPresentationModel>(
		alias: Alias,
		value: UserGroupPresentationModel[Alias]
	) {
		this.#data.update({ [alias]: value });
	}
}
