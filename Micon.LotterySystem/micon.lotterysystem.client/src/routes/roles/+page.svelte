<script lang="ts">
    import { onMount } from 'svelte';

    type SendAuthority = { name: string };
    type SendRole = { name: string; authorities: SendAuthority[] };

    let roles: SendRole[] = [];

    let newRoleName = '';
    let newAuthorities: string[] = [''];

    onMount(async () => {
        const response = await fetch('/api/Role/RoleList');
        roles = await response.json();
    });

    function addAuthorityField() {
        newAuthorities = [...newAuthorities, ''];
    }

    function updateAuthority(index: number, value: string) {
        newAuthorities[index] = value;
    }

    function removeAuthority(index: number) {
        newAuthorities.splice(index, 1);
        newAuthorities = [...newAuthorities];
    }

    function addRole() {
        if (!newRoleName.trim()) return;

        const validAuthorities = newAuthorities
            .map(name => name.trim())
            .filter(name => name !== '')
            .map(name => ({ name }));

        roles = [
            ...roles,
            {
                name: newRoleName,
                authorities: validAuthorities,
            },
        ];

        newRoleName = '';
        newAuthorities = [''];
    }

    function removeRole(index: number) {
        roles.splice(index, 1);
        roles = [...roles];
    }
</script>

<style>
    body {
        margin: 0;
        font-family: sans-serif;
        background-color: #f5f5f5;
    }

    .container {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 2rem;
    }

    h2 {
        font-size: 2rem;
        margin-bottom: 1rem;
    }

    .role-list {
        width: 100%;
        max-width: 600px;
        margin-bottom: 2rem;
    }

    .role-card {
        background: white;
        border: 1px solid #ccc;
        border-radius: 8px;
        padding: 1rem;
        margin-bottom: 1rem;
    }

    .role-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
    }

    .authority {
        color: #666;
        font-size: 0.9rem;
        margin-top: 0.5rem;
    }

    .form-card {
        background: white;
        border: 1px solid #ccc;
        border-radius: 10px;
        padding: 2rem;
        max-width: 500px;
        width: 100%;
        box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    }

    .form-group {
        margin-bottom: 1rem;
    }

        .form-group label {
            font-weight: bold;
            display: block;
            margin-bottom: 0.25rem;
        }

    input[type="text"] {
        width: 100%;
        padding: 0.5rem;
        border-radius: 6px;
        border: 1px solid #aaa;
        box-sizing: border-box;
    }

    .authority-field {
        display: flex;
        align-items: center;
        margin-bottom: 0.5rem;
    }

        .authority-field button {
            margin-left: 0.5rem;
            background: #ff4d4d;
            color: white;
            border: none;
            padding: 0.25rem 0.5rem;
            border-radius: 4px;
            cursor: pointer;
        }

            .authority-field button:hover {
                background: #e60000;
            }

    .add-authority {
        background: none;
        color: #007bff;
        border: none;
        padding: 0;
        cursor: pointer;
        font-size: 0.9rem;
        text-decoration: underline;
    }

    .submit-button {
        background: #007bff;
        color: white;
        padding: 0.6rem 1.2rem;
        border: none;
        border-radius: 6px;
        cursor: pointer;
        font-size: 1rem;
    }

        .submit-button:hover {
            background: #0056b3;
        }

    .delete-button {
        background: none;
        border: none;
        color: #ff4d4d;
        cursor: pointer;
        font-size: 0.9rem;
        text-decoration: underline;
    }

        .delete-button:hover {
            color: #e60000;
        }
</style>

<div class="container">
    <h2>Role一覧</h2>

    <div class="role-list">
        {#each roles as role, i}
        <div class="role-card">
            <div class="role-header">
                <strong>{role.name}</strong>
                <button class="delete-button" on:click={() => removeRole(i)}>削除</button>
            </div>
            <div class="authority">
                権限: {role.authorities.map(a => a.name).join(', ') || 'なし'}
            </div>
        </div>
        {/each}
    </div>

    <div class="form-card">
        <h3 style="margin-bottom: 1rem;">新しいRoleを追加</h3>

        <div class="form-group">
            <label>Role名</label>
            <input type="text" placeholder="例: 管理者" bind:value={newRoleName} />
        </div>

        <div class="form-group">
            <label>権限</label>
            {#each newAuthorities as authority, index}
            <div class="authority-field">
                <input type="text"
                       placeholder="例: ユーザー管理"
                       bind:value={newAuthorities[index]}
                       on:input={(e) => updateAuthority(index, e.target.value)}
                />
                {#if newAuthorities.length > 1}
                <button on:click={() => removeAuthority(index)}>×</button>
                {/if}
            </div>
            {/each}
            <button class="add-authority" on:click={addAuthorityField}>＋ 権限を追加</button>
        </div>

        <button class="submit-button" on:click={addRole}>Roleを追加</button>
    </div>
</div>
