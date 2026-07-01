<script lang="ts">
    import { onMount } from 'svelte';
    import { page } from '$app/stores';
    import { get } from 'svelte/store';

    const lotteryId = get(page).params.lotteryid;

    type Ticket = {
        id: string;
        number: number;
        displayId: string;
        status: 'Valid' | 'Invalid' | 'Winner' | 'Exchanged';
        issuedAt: string;
        updatedAt: string;
        issuerName: string;
    };

    let tickets: Ticket[] = [];
    let filteredTickets: Ticket[] = [];

    let loading = true;
    let error: string | null = null;

    let statusCounts = {
        Valid: 0,
        Invalid: 0,
        Winner: 0,
        Exchanged: 0
    };

    let searchTerm = '';

    function countStatuses() {
        statusCounts = {
            Valid: 0,
            Invalid: 0,
            Winner: 0,
            Exchanged: 0
        };

        for (const ticket of filteredTickets) {
            if (statusCounts[ticket.status] !== undefined) {
                statusCounts[ticket.status]++;
            }
        }
    }

    function applyFilter() {
        const lowerTerm = searchTerm.toLowerCase();
        filteredTickets = tickets.filter(t =>
            t.number.toString().includes(lowerTerm) ||
            t.status.toLowerCase().includes(lowerTerm) ||
            t.issuerName.toLowerCase().includes(lowerTerm)
        );
        countStatuses();
    }

    onMount(async () => {
        loading = true;
        try {
            const res = await fetch(`/api/ticket/list?lotteryGroupDisplayId=${lotteryId}`);
            if (!res.ok) throw new Error(`HTTP ${res.status}`);
            tickets = await res.json();
            filteredTickets = tickets;
            countStatuses();
            error = null;
        } catch (e) {
            error = `チケット一覧取得に失敗しました: ${e.message}`;
        } finally {
            loading = false;
        }
    });

    $: if (searchTerm !== undefined) applyFilter();
</script>

<style>
    .container {
        padding: 2rem;
        max-width: 900px;
        margin: auto;
        font-family: sans-serif;
    }

    .search-box {
        margin-bottom: 1rem;
    }

        .search-box input {
            width: 100%;
            padding: 0.5rem;
            font-size: 1rem;
            border-radius: 4px;
            border: 1px solid #ccc;
        }

    .status-summary {
        display: flex;
        gap: 1rem;
        margin-bottom: 2rem;
        flex-wrap: wrap;
    }

    .card {
        flex: 1 1 150px;
        background-color: #f9f9f9;
        padding: 1rem;
        border-radius: 8px;
        border: 1px solid #ddd;
        text-align: center;
        box-shadow: 2px 2px 6px rgba(0, 0, 0, 0.05);
    }

        .card h2 {
            margin: 0;
            font-size: 1.2rem;
            color: #333;
        }

        .card p {
            font-size: 1.5rem;
            font-weight: bold;
            color: #0077cc;
            margin: 0.5rem 0 0;
        }

    table {
        width: 100%;
        border-collapse: collapse;
        margin-top: 1rem;
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
        margin-top: 1rem;
    }

    .loading {
        font-style: italic;
    }
</style>

<div class="container">
    <h1>発行済み抽選券一覧</h1>

    {#if loading}
    <p class="loading">読み込み中...</p>
    {:else if error}
    <p class="error">{error}</p>
    {:else}
    <div class="search-box">
        <input type="text"
               placeholder="番号、状態、発行者名で検索..."
               bind:value={searchTerm} />
    </div>

    <div class="status-summary">
        <div class="card">
            <h2>有効 (Valid)</h2>
            <p>{statusCounts.Valid}</p>
        </div>
        <div class="card">
            <h2>無効 (Invalid)</h2>
            <p>{statusCounts.Invalid}</p>
        </div>
        <div class="card">
            <h2>当選 (Winner)</h2>
            <p>{statusCounts.Winner}</p>
        </div>
        <div class="card">
            <h2>引き換え済 (Exchanged)</h2>
            <p>{statusCounts.Exchanged}</p>
        </div>
    </div>

    {#if filteredTickets.length === 0}
    <p>該当するチケットは見つかりませんでした。</p>
    {:else}
    <table>
        <thead>
            <tr>
                <th>番号</th>
                <th>状態</th>
                <th>発行日時</th>
                <th>更新日時</th>
                <th>発行者</th>
            </tr>
        </thead>
        <tbody>
            {#each filteredTickets as t}
            <tr>
                <td>No.{t.number}</td>
                <td>{t.status}</td>
                <td>{new Date(t.issuedAt).toLocaleString('ja-JP')}</td>
                <td>{new Date(t.updatedAt).toLocaleString('ja-JP')}</td>
                <td>{t.issuerName}</td>
            </tr>
            {/each}
        </tbody>
    </table>
    {/if}
    {/if}
</div>
