﻿
<script lang="ts">
    import { onMount } from 'svelte';
    type LotteryGroup = {
        name: string;
        id: string;
    };
    let lottery: LotteryGroup[] = [];
    let loading = true;
    let error: string | null = null;

    let newName = '';
    let createError: string | null = null;
    let creating = false;

    async function loadList() {
        loading = true;
        try {
            const res = await fetch('/api/LotteryGroup/List');
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            lottery = await res.json();
            error = null;
        } catch (err) {
            error = `抽選会一覧の取得に失敗しました: ${err.message}`;
        } finally {
            loading = false;
        }
    }

    async function createLottery() {
        createError = null;
        if (!newName.trim()) {
            createError = '名前を入力してください';
            return;
        }

        creating = true;
        try {
            const res = await fetch('/api/LotteryGroup/Create', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(newName.trim())
            });

            if (!res.ok) throw new Error(`HTTP ${res.status}`);

            newName = '';
            await loadList();
        } catch (err) {
            createError = `作成に失敗しました: ${err.message}`;
        } finally {
            creating = false;
        }
    }

    onMount(loadList);
</script>

<style>
    .container {
        padding: 2rem;
        max-width: 800px;
        margin: 0 auto;
    }

    .new-form {
        display: flex;
        gap: 0.5rem;
        margin-bottom: 1.5rem;
        flex-wrap: wrap;
    }

        .new-form input {
            flex: 1;
            padding: 0.5rem;
            border: 1px solid #ccc;
            border-radius: 0.5rem;
            font-size: 1rem;
        }

        .new-form button {
            padding: 0.5rem 1rem;
            border: none;
            background: #007acc;
            color: white;
            border-radius: 0.5rem;
            cursor: pointer;
            transition: background 0.2s;
        }

            .new-form button:hover {
                background: #005fa3;
            }

    .lottery-list {
        display: grid;
        gap: 1rem;
    }

    .lottery-item {
        padding: 1rem;
        border: 1px solid #ccc;
        border-radius: 0.75rem;
        background: #f9f9f9;
        transition: background 0.2s, box-shadow 0.2s;
        text-decoration: none;
        color: inherit;
        display: block;
    }

        .lottery-item:hover {
            background: #e9f5ff;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        }

    .status {
        margin-top: 1rem;
        color: gray;
    }

    .error {
        color: red;
        font-weight: bold;
        margin-top: 0.5rem;
    }
</style>

<div class="container">
    <h1>抽選会一覧</h1>

    <div class="new-form">
        <input type="text"
               placeholder="新しい抽選会名を入力"
               bind:value={newName}
               onkeydown={(e) => e.key === 'Enter' && createLottery()}
        disabled={creating}
        />
        <button onclick={createLottery} disabled={creating}>
            {creating ? '作成中...' : '作成'}
        </button>
    </div>

    {#if createError}
    <p class="error">{createError}</p>
    {/if}

    {#if loading}
    <p class="status">読み込み中...</p>
    {:else if error}
    <p class="error">{error}</p>
    {:else if lottery.length === 0}
    <p class="status">抽選会が登録されていません。</p>
    {:else}
    <div class="lottery-list">
        {#each lottery as item}
        <a class="lottery-item" href={`/lottery/${encodeURIComponent(item.id)}`}>
            {item.name}
        </a>
        {/each}
    </div>
    {/if}
</div>