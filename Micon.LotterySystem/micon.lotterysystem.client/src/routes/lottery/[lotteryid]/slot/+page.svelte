<script lang="ts">
    import { page } from '$app/stores';
    import { onMount } from 'svelte';

    let lotteryid = $page.params.lotteryid;
    let lotteryName = "";

    onMount(async () => {
        let res = await fetch(`/api/LotteryGroup/Name?id=${lotteryid}`);
        lotteryName = await res.text();
    });

    type LotterySlot = {
        name: string;
        lotteryId: string;
        slotId: string;
        merchandise: string;
        numberOfFrames: number;
        deadLine: string | null;
    };

    let slots: LotterySlot[] = [];
    let editing: Record<string, boolean> = {};
    let newSlot: Partial<LotterySlot> = {};
    let loading = true;

    function toDateTimeLocalFormat(dateStr: string | null): string | null {
        if (!dateStr) return null;
        const date = new Date(dateStr);
        const offsetDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
        return offsetDate.toISOString().slice(0, 16);
    }

    async function fetchSlots() {
        loading = true;
        const encodedid = encodeURIComponent(lotteryid);
        const res = await fetch(`/api/LotterySlot/List/${encodedid}`);
        let obj = await res.json();

        slots = obj.map((slot: LotterySlot) => ({
            ...slot,
            deadLine: toDateTimeLocalFormat(slot.deadLine)
        }));

        loading = false;
    }

    async function updateSlot(slot: LotterySlot, id: string) {
        const res = await fetch('/api/LotterySlot/Update', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                LotteryId: lotteryid,
                SlotId: id,
                ...slot,
                deadLine: slot.deadLine ? new Date(slot.deadLine).toISOString() : null
            })
        });

        if (res.ok) {
            editing[slot.slotId] = false;
            await fetchSlots();
        } else {
            alert('更新に失敗しました');
        }
    }

    async function moveSlot(id: string, toIndex: number) {
        const res = await fetch('/api/LotterySlot/MoveIndex', {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ id: id, newIndex: toIndex })
        });
        if (res.ok) await fetchSlots();
    }

    async function createSlot() {
        if (!newSlot.name) {
            alert('名前を入力してください');
            return;
        }

        const payload = {
            LotteryName: lotteryName,
            Name: newSlot.name,
            LotteryId: lotteryid,
            Merchandise: newSlot.merchandise ?? '',
            NumberOfFrames: newSlot.numberOfFrames ?? 0,
            DeadLine: newSlot.deadLine ? new Date(newSlot.deadLine).toISOString() : new Date().toISOString()
        };

        const res = await fetch('/api/LotterySlot/Create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            newSlot = {};
            await fetchSlots();
        } else {
            alert('作成に失敗しました');
        }
    }

    onMount(fetchSlots);
</script>

<style>
    .slot-card {
        border: 1px solid #ccc;
        border-radius: 0.75rem;
        padding: 1rem;
        margin-bottom: 1rem;
        background: #f9f9f9;
    }

    .slot-actions {
        display: flex;
        gap: 0.5rem;
        margin-top: 0.5rem;
    }

    input, select {
        padding: 0.4rem;
        border-radius: 0.5rem;
        border: 1px solid #ccc;
        width: 100%;
    }

    .field {
        margin-bottom: 0.5rem;
    }

    .new-slot {
        margin-bottom: 2rem;
        padding: 1rem;
        border: 2px dashed #aaa;
        border-radius: 1rem;
    }

    .btn {
        padding: 0.4rem 0.8rem;
        background: #007acc;
        color: white;
        border: none;
        border-radius: 0.5rem;
        cursor: pointer;
    }

        .btn:hover {
            background: #005fa3;
        }
</style>

<h1>抽選枠管理 - {lotteryName}</h1>

<div class="new-slot">
    <h3>＋ 新しい抽選枠を追加</h3>
    <div class="field">
        <label>名前</label>
        <input bind:value={newSlot.name} />
    </div>
    <div class="field">
        <label>景品</label>
        <input bind:value={newSlot.merchandise} />
    </div>
    <div class="field">
        <label>枠数</label>
        <input type="number" bind:value={newSlot.numberOfFrames} />
    </div>
    <div class="field">
        <label>締切</label>
        <input type="datetime-local" bind:value={newSlot.deadLine} />
    </div>
    <button class="btn" on:click={createSlot}>作成</button>
</div>

{#if loading}
<p>読み込み中...</p>
{:else}
  {#each slots as slot, index}
<div class="slot-card">
    {#if editing[slot.slotId]}
    <div class="field">
        <label>名前</label>
        <input bind:value={slot.name} />
    </div>
    <div class="field">
        <label>景品</label>
        <input bind:value={slot.merchandise} />
    </div>
    <div class="field">
        <label>枠数</label>
        <input type="number" bind:value={slot.numberOfFrames} />
    </div>
    <div class="field">
        <label>締切</label>
        <input type="datetime-local" bind:value={slot.deadLine} />
    </div>
    <div class="slot-actions">
        <button class="btn" on:click={() => updateSlot(slot, slot.slotId)}>保存</button>
        <button class="btn" on:click={() => editing[slot.slotId] = false}>キャンセル</button>
    </div>
    {:else}
    <p><strong>{slot.name}</strong></p>
    <p>景品: {slot.merchandise}</p>
    <p>枠数: {slot.numberOfFrames}</p>
    <p>締切: {slot.deadLine ? new Date(slot.deadLine).toLocaleString() : '未設定'}</p>
    <div class="slot-actions">
        <button class="btn" on:click={() => editing[slot.slotId] = true}>編集</button>
        {#if index > 0}
        <button class="btn" on:click={() => moveSlot(slot.slotId, index - 1)}>↑ 上へ</button>
        {/if}
        {#if index < slots.length - 1}
        <button class="btn" on:click={() => moveSlot(slot.slotId, index + 1)}>↓ 下へ</button>
        {/if}
    </div>
    {/if}
</div>
  {/each}
{/if}
