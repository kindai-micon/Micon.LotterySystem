<script lang="ts">
    import { onMount } from 'svelte';
    import { writable } from 'svelte/store';

    type SendAuthority = {
        name: string;
    };

    type SendRole = {
        name: string;
        authorities: SendAuthority[];
    };

    let roles = writable < SendRole[] > ([]);
    let authorityList: string[] = [];
    let newRoleName = "";

    onMount(async () => {
        await fetchRoles();
        await fetchAuthorityList();
    });

    async function fetchRoles() {
        const res = await fetch('/api/Role/RoleList');
        const data = await res.json();
        roles.set(data);
    }

    async function fetchAuthorityList() {
        const res = await fetch('/api/Role/AuthorityList');
        authorityList = await res.json();
    }

    async function addRole() {
        if (!newRoleName.trim()) return;

        const res = await fetch('/api/Role/CreateRole', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(newRoleName.trim())
        });

        if (res.ok) {
            newRoleName = "";
            await fetchRoles();
        } else {
            alert("ロールの追加に失敗しました");
        }
    }

    async function deleteRole(roleName: string) {
        const res = await fetch("/api/Role/DeleteRole", {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(roleName.trim())
        });

        if (res.ok) {
            await fetchRoles();
        } else {
            alert("ロールの削除に失敗しました");
        }
    }

    async function toggleAuthority(roleName: string, authority: string, hasAuthority: boolean) {
        const url = hasAuthority ? '/api/Role/RemoveAuthority' : '/api/Role/AddAuthority';

        await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ roleName, authority })
        });

        await fetchRoles();
    }

    function hasAuthority(role: SendRole, authority: string): boolean {
        return role.authorities.some(a => a.name === authority);
    }
</script>

<style>
    .container {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 2rem;
        font-family: sans-serif;
    }

    .input-area {
        margin-bottom: 1rem;
    }

    input[type="text"] {
        padding: 0.4rem;
        margin-right: 0.5rem;
        width: 200px;
    }

    button {
        padding: 0.4rem 0.8rem;
        cursor: pointer;
        margin-left: 0.3rem;
    }

    .role-card {
        border: 1px solid #ccc;
        padding: 1rem;
        margin-bottom: 1rem;
        width: 300px;
        text-align: left;
    }

    .role-header {
        font-weight: bold;
        margin-bottom: 1rem;
        text-align: center;
    }

    .authority-toggle {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin: 0.3rem 0;
    }

    /* Toggle switch */
    .switch {
        position: relative;
        display: inline-block;
        width: 40px;
        height: 20px;
    }

        .switch input {
            opacity: 0;
            width: 0;
            height: 0;
        }

    .slider {
        position: absolute;
        cursor: pointer;
        background-color: #ccc;
        border-radius: 34px;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        transition: .2s;
    }

        .slider:before {
            position: absolute;
            content: "";
            height: 14px;
            width: 14px;
            left: 3px;
            bottom: 3px;
            background-color: white;
            border-radius: 50%;
            transition: .2s;
        }

    .switch input:checked + .slider {
        background-color: #4caf50;
    }

        .switch input:checked + .slider:before {
            transform: translateX(20px);
        }

    .delete-button {
        color: white;
        background-color: red;
        border: none;
        padding: 0.4rem 0.6rem;
        float: right;
        cursor: pointer;
    }

    /* ...既存のスタイルは省略... */
    .add-role-button {
        padding: 10px 20px;
        background-color: #007bff;
        color: white;
        border: none;
        border-radius: 3px;
        cursor: pointer;
        transition: background-color 0.3s ease;
    }

    .add-role-button:disabled {
        background-color: #ccc;
        cursor: not-allowed;
    }
    .switch input:disabled + .slider {
        background-color: #aaa;
        cursor: not-allowed;
        opacity: 0.6;
    }


</style>

<div class="container">
    <h2>ロール管理</h2>
    <div class="input-area">
        <input type="text" bind:value={newRoleName} placeholder="新しいロール名" />
        <button class="add-role-button" on:click={addRole} disabled={!newRoleName.trim()}>追加</button>
    </div>

    {#each $roles as role}
    <div class="role-card">
        <div class="role-header">
            {role.name}
            {#if role.name != "Admin"}
            <button class="delete-button" on:click={() => deleteRole(role.name)}>削除</button>
            {/if}
        </div>

        {#each authorityList as authority}
        <div class="authority-toggle">
            <span>{authority}</span>
            <label class="switch">
                <input type="checkbox"
                       checked={hasAuthority(role, authority)}
                       disabled={role.name === "Admin"}
                       on:change={() => toggleAuthority(role.name, authority, hasAuthority(role, authority))}
                />
                <span class="slider"></span>
            </label>
        </div>
        {/each}
    </div>
    {/each}
</div>
