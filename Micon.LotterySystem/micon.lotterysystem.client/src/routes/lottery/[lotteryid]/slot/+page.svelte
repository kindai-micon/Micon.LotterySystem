﻿
<script lang="ts">
    import { page } from '$app/stores';
    import { onMount } from 'svelte';
    import { get } from 'svelte/store';

    const lotteryid = get(page).params.lotteryid;
    let lotteryName = "";

    onMount(async () => {
        const res = await fetch(`/api/LotteryGroup/Name?id=${lotteryid}`);
        lotteryName = await res.text();
        await fetchSlots();
    });

    type LotterySlot = {
        name: string;
        lotteryId: string;
        slotId: string;
        merchandise: string;
        numberOfFrames: number;
        deadLine: string | null;
        noDeadline?: boolean;
    };

    let slots: LotterySlot[] = [];
    let editing: Record<string, boolean> = {};
    let newSlot: Partial<LotterySlot> = {};
    let noDeadline = false;
    let loading = true;
    function toDateTimeLocalFormat(dateStr: string | null): string {
        if (!dateStr) return "";
        const date = new Date(dateStr);
        const offsetDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
        return offsetDate.toISOString().slice(0, 16);
    }

    async function fetchSlots() {
        loading = true;
        const encodedid = encodeURIComponent(lotteryid);
        const res = await fetch(`/api/LotterySlot/List/${encodedid}`);
        const obj = await res.json();

        slots = obj.map((slot: LotterySlot) => ({
            ...slot,
            deadLine: toDateTimeLocalFormat(slot.deadLine),
            noDeadline: slot.deadLine === null
        }));
        console.log(slots);
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
                deadLine: slot.noDeadline ? null : slot.deadLine ? new Date(slot.deadLine).toISOString() : null
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
            DeadLine: noDeadline ? null : newSlot.deadLine ? new Date(newSlot.deadLine).toISOString() : new Date().toISOString()
        };

        const res = await fetch('/api/LotterySlot/Create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            newSlot = {};
            noDeadline = false;
            await fetchSlots();
        } else {
            alert('作成に失敗しました');
        }
    }
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
        justify-content: flex-start;
        gap: 0.5rem;
        margin-top: 0.5rem;
    }

    input {
        padding: 0.4rem;
        border-radius: 0.5rem;
        border: 1px solid #ccc;
    }

    .width-max {
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

    .checkbox-label {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        justify-content: flex-start;
        font-size: 0.95rem;
        margin-top: 0.5rem;
    }
</style>

<h1>抽選枠管理 - {lotteryName}</h1>

<div class="new-slot">
    <h3>＋ 新しい抽選枠を追加</h3>
    <div class="field">
        <label>名前</label>
        <input bind:value={newSlot.name} class="width-max" />
    </div>
    <div class="field">
        <label>景品</label>
        <input bind:value={newSlot.merchandise} class="width-max" />
    </div>
    <div class="field">
        <label>枠数</label>
        <input type="number" bind:value={newSlot.numberOfFrames} class="width-max" />
    </div>
    <div class="field">
        <label>締切</label>
        <input type="datetime-local" class="width-max"
               bind:value={newSlot.deadLine}
               disabled={noDeadline}
               oninput={(e) => newSlot.deadLine = e.target.value} />
        <label class="checkbox-label">
            <input type="checkbox"
                   bind:checked={noDeadline}
                   onchange={() => {
            if (slot.noDeadline) {
            slot.deadLine = "";
            } else {
            slot.deadLine = toDateTimeLocalFormat(new Date().toISOString());
            }
            }} />
            締切なし
        </label>
    </div>
    <button class="btn" onclick={createSlot}>作成</button>
</div>

{#if loading}
<p>読み込み中...</p>
{:else}
    {#each slots as slot, index}
<div class="slot-card">
    {#if editing[slot.slotId]}
    <div class="field">
        <label>名前</label>
        <input bind:value={slot.name} class="width-max " />
    </div>
    <div class="field">
        <label>景品</label>
        <input bind:value={slot.merchandise} class="width-max" />
    </div>
    <div class="field">
        <label>枠数</label>
        <input type="number" class="width-max " bind:value={slot.numberOfFrames} />
    </div>
    <div class="field">
        <label>締切</label>
        <input class="width-max " type="datetime-local"
               bind:value={slot.deadLine}
               disabled={slot.noDeadline}
               oninput={(e) => slot.deadLine = e.target.value} />
        <label class="checkbox-label">
            <input type="checkbox"
                   bind:checked={slot.noDeadline}
                   onchange={() => {
            if (slot.noDeadline) {
            slot.deadLine = "";
            } else {
            slot.deadLine = toDateTimeLocalFormat(new Date().toISOString());
            }
            }} />
            締切なし
        </label>
    </div>
    <div class="slot-actions">
        <button class="btn" onclick={() => updateSlot(slot, slot.slotId)}>保存</button>
        <button class="btn" onclick={() => editing[slot.slotId] = false}>キャンセル</button>
    </div>
    {:else}
    <p><strong>{slot.name}</strong></p>
    <p>景品: {slot.merchandise}</p>
    <p>枠数: {slot.numberOfFrames}</p>
    <p>締切: {slot.deadLine ? new Date(slot.deadLine).toLocaleString() : '未設定'}</p>
    <div class="slot-actions">
        <button class="btn" onclick={() => editing[slot.slotId] = true}>編集</button>
        {#if index > 0}
        <button class="btn" onclick={() => moveSlot(slot.slotId, index - 1)}>↑ 上へ</button>
        {/if}
        {#if index < slots.length - 1}
        <button class="btn" onclick={() => moveSlot(slot.slotId, index + 1)}>↓ 下へ</button>
        {/if}
    </div>
    {/if}
</div>
    {/each}
{/if}