<script lang="ts">
    import { onMount } from 'svelte';

    type SendAuthority = { name: string };
    type SendRole = { name: string; authorities: SendAuthority[] };

    let roles: SendRole[] = [];
    let authorityOptions: string[] = [];

    let newRoleName = '';
    let selectedAuthorities: string[] = [''];

    onMount(async () => {
        const [rolesResponse, authoritiesResponse] = await Promise.all([
            fetch('/api/Role/RoleList'),
            fetch('/api/Role/AuthorityList')
        ]);

        roles = await rolesResponse.json();
        authorityOptions = await authoritiesResponse.json();
        console.log(authorityOptions);
    });

    function getAvailableOptions(index: number): string[] {
        const used = selectedAuthorities.filter((_, i) => i !== index);
        return authorityOptions.filter(a => !used.includes(a));
    }

    function addAuthorityField() {
        selectedAuthorities = [...selectedAuthorities, ''];
    }

    function updateAuthority(index: number, value: string) {
        selectedAuthorities[index] = value;
    }

    function removeAuthority(index: number) {
        selectedAuthorities.splice(index, 1);
        selectedAuthorities = [...selectedAuthorities];
    }

    function addRole() {
        if (!newRoleName.trim()) return;

        const validAuthorities = selectedAuthorities
            .filter(name => name.trim() !== '')
            .map(name => ({ name }));

        roles = [
            ...roles,
            {
                name: newRoleName,
                authorities: validAuthorities,
            },
        ];

        newRoleName = '';
        selectedAuthorities = [''];
    }

    function removeRole(index: number) {
        roles.splice(index, 1);
        roles = [...roles];
    }
</script>

<style>
    .container {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 2rem;
        font-family: sans-serif;
        background: #f9f9f9;
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
        color: #555;
        font-size: 0.9rem;
        margin-top: 0.5rem;
    }

    .form-card {
        background: white;
        padding: 2rem;
        border-radius: 10px;
        border: 1px solid #ccc;
        max-width: 500px;
        width: 100%;
    }

    .form-group {
        margin-bottom: 1rem;
    }

        .form-group label {
            font-weight: bold;
            display: block;
            margin-bottom: 0.3rem;
        }

    input[type="text"], select {
        width: 100%;
        padding: 0.5rem;
        border: 1px solid #aaa;
        border-radius: 6px;
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
            padding: 0.3rem 0.6rem;
            border-radius: 4px;
            cursor: pointer;
        }

            .authority-field button:hover {
                background: #d60000;
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
            <input type="text" bind:value={newRoleName} placeholder="例: 管理者" />
        </div>

        <div class="form-group">
            <label>権限</label>
            {#each selectedAuthorities as selected, index}
            <div class="authority-field">
                <select bind:value={selectedAuthorities[index]}
                        on:change={(e) =>
                    updateAuthority(index, e.target.value)}
                    >
                    <option value="" disabled selected>選択してください</option>
                    {#each getAvailableOptions(index) as option}
                    <option value={option}>{option}</option>
                    {/each}
                    {#if selected && !authorityOptions.includes(selected)}
                    <option value={selected} selected>{selected}</option>
                    {/if}
                </select>
                {#if selectedAuthorities.length > 1}
                <button on:click={() => removeAuthority(index)}>×</button>
                {/if}
            </div>
            {/each}
            <button class="add-authority" on:click={addAuthorityField}>＋ 権限を追加</button>
        </div>

        <button class="submit-button" on:click={addRole}>Roleを追加</button>
    </div>
</div>
