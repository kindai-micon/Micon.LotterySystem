<script lang="ts">
    import { page } from '$app/stores';
    import { onMount } from 'svelte';
    import { get } from 'svelte/store';
    import type { SendUser } from '$lib/models/user';

    let username = '';
    let user: SendUser | null = null;
    let loading = true;
    let error: string | null = null;

    onMount(async () => {
        const p = get(page);
        username = p.params.username;

        if (!username) {
            error = 'ユーザー名が取得できませんでした';
            loading = false;
            return;
        }

        try {
            const res = await fetch("/api/user/UserInfo?username=" + encodeURIComponent(username));
            if (!res.ok) throw new Error(`Error ${res.status}`);
            user = await res.json();
        } catch (e) {
            error = e.message;
        } finally {
            loading = false;
        }
    });

    async function removeRole(index: number) {
        if (user) {
            let role: SendRole = user.roles[index];
            user.roles.splice(index, 1);
            user.roles = [...user.roles];
            console.log(role);
            const response = await fetch('/api/role/DeleteRole', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(role.name),
            });
        }
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
</style>

<div class="container">
    <div class="title">{username} の設定</div>

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
            <button class="remove-button" on:click={async() =>await removeRole(index)}>削除</button>
        </div>
        {/each}
        {/if}
    </div>
    {/if}
</div>
