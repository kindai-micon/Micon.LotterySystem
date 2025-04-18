<script lang="ts">
    import { onMount } from 'svelte';
    import { page } from '$app/stores';

    const lotteryId = $page.params.lotteryid;
    type Ticket = {
        id: string;
        number: number;
        displayId: string;
        status: string;
        issuedAt: string;
        updatedAt: string;
    };

    let tickets: Ticket[] = [];
    let loading = true;
    let error: string | null = null;

    onMount(async () => {
        loading = true;
        try {
            const res = await fetch(`/api/ticket/list?lotteryGroupDisplayId=${lotteryId}`);
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            tickets = await res.json();
            error = null;
        } catch (e) {
            error = `チケット一覧取得に失敗しました: ${e.message}`;
        } finally {
            loading = false;
        }
    });
</script>

<style>
    .container {
        padding: 2rem;
        max-width: 800px;
        margin: auto;
    }

    table {
        width: 100%;
        border-collapse: collapse;
    }

    th, td {
        border: 1px solid #ccc;
        padding: 0.5rem;
        text-align: center;
    }

    th {
        background: #f0f0f0;
    }

    .error {
        color: red;
    }
</style>

<div class="container">
    <h1>発行済み抽選券一覧</h1>

    {#if loading}
    <p>読み込み中...</p>
    {:else if error}
    <p class="error">{error}</p>
    {:else if tickets.length === 0}
    <p>チケットが発行されていません。</p>
    {:else}
    <table>
        <thead>
            <tr>
                <th>番号</th>
                <th>DisplayID</th>
                <th>状態</th>
                <th>発行日時</th>
                <th>更新日時</th>
            </tr>
        </thead>
        <tbody>
            {#each tickets as t}
            <tr>
                <td>No.{t.number}</td>
                <td>{t.displayId}</td>
                <td>{t.status}</td>
                <td>{new Date(t.issuedAt).toLocaleString('ja-JP')}</td>
                <td>{new Date(t.updatedAt).toLocaleString('ja-JP')}</td>
            </tr>
            {/each}
        </tbody>
    </table>
    {/if}
</div>
