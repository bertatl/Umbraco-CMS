import { css, html, LitElement, nothing } from 'lit';
import { UUITextStyles } from '@umbraco-ui/uui-css/lib';
import { customElement, property, state } from 'lit/decorators.js';
import { UmbContextConsumerMixin } from '../../../../../core/context';
import { repeat } from 'lit/directives/repeat.js';

interface TableColumn {
	name: string;
	sort: Function;
}

interface TableItem {
	key: string;
	name: string;
	userGroup: string;
	lastLogin: string;
	status?: string;
}

@customElement('umb-editor-view-users-grid')
export class UmbEditorViewUsersGridElement extends UmbContextConsumerMixin(LitElement) {
	static styles = [
		UUITextStyles,
		css`
			#user-grid {
				display: grid;
				grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
				gap: var(--uui-size-space-4);
				margin-top: var(--uui-size-space-4);
			}

			uui-card-user {
				width: 100%;
				height: 180px;
			}

			.user-login-time {
				margin-top: auto;
			}
		`,
	];

	@property()
	public users: Array<TableItem> = [];

	render() {
		return html`
			<div id="user-grid">
				${repeat(
					this.users,
					(user) => user.key,
					(user) => html`
						<uui-card-user .name=${user.name}>
							${user.status ? html`<uui-tag slot="tag" size="s">${user.status}</uui-tag>` : nothing}
							<div>${user.userGroup}</div>
							<div class="user-login-time">${user.lastLogin}</div>
						</uui-card-user>
					`
				)}
			</div>
		`;
	}
}

export default UmbEditorViewUsersGridElement;

declare global {
	interface HTMLElementTagNameMap {
		'umb-editor-view-users-grid': UmbEditorViewUsersGridElement;
	}
}
