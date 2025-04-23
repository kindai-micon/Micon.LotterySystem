<script lang="ts">
    import { page } from '$app/stores';
    import { onMount } from 'svelte';
    import { get } from 'svelte/store';
    import type { SendUser } from '$lib/models/user';

    let username = '';
    let user: SendUser | null = null;
    let loading = true;
    let error: string | null = null;
    let newRoleName = '';
    let availableRoles: SendRole[] = [];

    onMount(async () => {
        const p = get(page);
        username = p.params.username;

        if (!username) {
            error = 'ユーザー名が取得できませんでした';
            loading = false;
            return;
        }

        try {
            // ユーザー情報取得
            const res = await fetch("/api/user/UserInfo?username=" + encodeURIComponent(username));
            if (!res.ok) throw new Error(`Error ${res.status}`);
            user = await res.json();

            // 利用可能ロール一覧取得
            const rolesRes = await fetch("/api/Role/RoleList");
            if (!rolesRes.ok) throw new Error(`ロール一覧取得失敗: ${rolesRes.status}`);
            availableRoles = await rolesRes.json();
        } catch (e) {
            error = e.message;
        } finally {
            loading = false;
        }
    })
    function getUnassignedRoles(): SendRole[] {
        if (!user) return [];
        const assignedNames = new Set(user.roles.map(r => r.name));
        return availableRoles.filter(r => !assignedNames.has(r.name));
    }

    async function addRole() {
        if (!newRoleName || !user) return;

        const payload = {
            userName: user.userName,
            roleName: newRoleName
        };

        const response = await fetch('/api/User/AddRole', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(payload),
        });

        if (response.ok) {
            const addedRole = availableRoles.find(r => r.name === newRoleName);
            if (addedRole) {
                user.roles = [...user.roles, addedRole];
            }
            newRoleName = '';
        } else {
            const text = await response.text();
            alert('ロール追加失敗: ' + text);
        }
    }
    let showModal = false;
    let roleToRemove: SendRole | null = null;

    function confirmRemoveRole(role: SendRole) {
        roleToRemove = role;
        showModal = true;
    }

    async function removeConfirmedRole() {
        if (!user || !roleToRemove) return;

        const payload = {
            userName: user.userName,
            roleName: roleToRemove.name
        };

        const response = await fetch('/api/User/RemoveRole', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(payload),
        });

        if (response.ok) {
            user.roles = user.roles.filter(r => r.name !== roleToRemove?.name);
        } else {
            const text = await response.text();
            alert('ロール削除失敗: ' + text);
        }

        roleToRemove = null;
        showModal = false;
    }

</script>


<style>
    .container {
        max-width: 800px;
        margin: auto;
        padding: 20px;
    }

    .title {
        font-size: 24px;
        margin-bottom: 16px;
    }

    .section {
        margin-bottom: 24px;
    }

    .label {
        font-weight: bold;
        margin-bottom: 8px;
    }

    .role {
        border: 1px solid #ccc;
        padding: 12px;
        border-radius: 8px;
        margin-bottom: 12px;
    }

    .role-name {
        font-size: 18px;
        font-weight: bold;
    }

    .authority-list {
        margin-top: 8px;
    }

    .remove-button {
        margin-top: 8px;
        padding: 4px 8px;
        background-color: #e74c3c;
        color: white;
        border: none;
        border-radius: 4px;
        cursor: pointer;
    }

        .remove-button:hover {
            background-color: #c0392b;
        }

    .modal-overlay {
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background-color: rgba(0, 0, 0, 0.5);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 999;
    }

    .modal {
        background: white;
        padding: 20px;
        border-radius: 10px;
        width: 300px;
        box-shadow: 0 0 10px rgba(0,0,0,0.3);
    }

    .modal-title {
        font-size: 18px;
        font-weight: bold;
        margin-bottom: 12px;
    }

    .modal-content {
        margin-bottom: 16px;
    }

    .modal-buttons {
        display: flex;
        justify-content: flex-end;
        gap: 10px;
    }

    .modal-button {
        padding: 6px 12px;
        border: none;
        border-radius: 4px;
        cursor: pointer;
    }

        .modal-button.confirm {
            background-color: #e74c3c;
            color: white;
        }

        .modal-button.cancel {
            background-color: #ccc;
        }
    .back-link {
        display: inline-block;
        margin-bottom: 16px;
        text-decoration: none;
        color: #3498db;
        font-weight: bold;
    }

        .back-link:hover {
            text-decoration: underline;
        }

</style>
{#if showModal && roleToRemove}
<div class="modal-overlay">
    <div class="modal">
        <div class="modal-title">確認</div>
        <div class="modal-content">
            ロール「<strong>{roleToRemove.name}</strong>」を削除してもよろしいですか？
        </div>
        <div class="modal-buttons">
            <button class="modal-button confirm" onclick={removeConfirmedRole}>削除</button>
            <button class="modal-button cancel" onclick={() => { showModal = false; roleToRemove = null; }}>キャンセル</button>
        </div>
    </div>
</div>
{/if}

<div class="container">
    <a href="/users" class="back-link">← ユーザー一覧に戻る</a>
    <h2>{username} の設定</h2>

    {#if loading}
    <p>読み込み中...</p>
    {:else if error}
    <p class="error">エラー: {error}</p>
    {:else if !user}
    <p>ユーザー情報が取得できませんでした。</p>
    {:else}
    <div class="section">
        <div class="label">ユーザー名:</div>
        <div>{user.userName}</div>
    </div>
    <!-- この部分は .section クラス内のロール一覧の下に追加 -->
    <div class="section">
        <div class="label">ロール一覧:</div>

        {#if user.roles.length === 0}
        <div>ロールなし</div>
        {:else}
        {#each user.roles as role, index}
        <div class="role">
            <div class="role-name">{role.name}</div>
            <div class="authority-list">
                権限:
                <ul>
                    {#each role.authorities as authority}
                    <li>{authority.name}</li>
                    {/each}
                </ul>
            </div>
            <button class="remove-button" onclick={() => confirmRemoveRole(role)}>削除</button>

        </div>
        {/each}
        {/if}

        <!-- ロール追加UI -->
        {#if getUnassignedRoles().length > 0}
        <div style="margin-top: 16px;">
            <select bind:value={newRoleName} style="padding: 6px; margin-right: 8px; border: 1px solid #ccc; border-radius: 4px;">
                <option value="" disabled selected>ロールを選択</option>
                {#each getUnassignedRoles() as role}
                <option value={role.name}>{role.name}</option>
                {/each}
            </select>
            <button onclick={addRole}
                    disabled={!newRoleName}
                    style="padding: 6px 12px; background-color: #2ecc71; color: white; border: none; border-radius: 4px; cursor: pointer;">
                ロール追加
            </button>
        </div>
        {:else}
        <div style="margin-top: 16px;">追加可能なロールはありません</div>
        {/if}


    </div>

    {/if}
</div>
