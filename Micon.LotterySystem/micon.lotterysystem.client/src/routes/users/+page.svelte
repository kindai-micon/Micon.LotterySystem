<script lang="ts">
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';
    import type { SendUser } from '$lib/models/user';

    let users: SendUser[] = [];
    let loading = true;
    let error: string | null = null;

    onMount(async () => {
        try {
            const res = await fetch('/api/user/UserList');
            if (!res.ok) throw new Error(`Error ${res.status}`);
            users = await res.json();
        } catch (e) {
            error = e.message;
        } finally {
            loading = false;
        }
    });

    function goToUserSettings(userName: string) {
        goto(`/users/${encodeURIComponent(userName)}`);
    }
</script>

<style>
    .container {
        max-width: 800px;
        margin: 20px auto;
        padding: 10px;
    }

    .title {
        font-size: 1.5rem;
        font-weight: bold;
        margin-bottom: 16px;
    }

    .user-list {
        list-style: none;
        padding: 0;
    }

    .user-item {
        padding: 12px;
        border: 1px solid #ccc;
        border-radius: 6px;
        margin-bottom: 10px;
        cursor: pointer;
        transition: background-color 0.2s ease-in-out;
    }

        .user-item:hover {
            background-color: #f5f5f5;
        }

    .username {
        font-weight: bold;
    }

    .roles {
        font-size: 0.9rem;
        color: #666;
        margin-top: 4px;
    }

    .error {
        color: red;
    }
</style>

<div class="container">
    <div class="title">ユーザー一覧</div>

    {#if loading}
    <p>読み込み中...</p>
    {:else if error}
    <p class="error">エラー: {error}</p>
    {:else if users.length === 0}
    <p>ユーザーが見つかりませんでした。</p>
    {:else}
    <ul class="user-list">
        {#each users as user}
        <li class="user-item" on:click={() =>
            goToUserSettings(user.userName)}>
            <div class="username">{user.userName}</div>
            <div class="roles">
                ロール:
                {#each user.roles as role, i}
                {role.name}{i < user.roles.length - 1 ? ', ' : ''}
                {/each}
            </div>
        </li>
        {/each}
    </ul>
    {/if}
</div>
