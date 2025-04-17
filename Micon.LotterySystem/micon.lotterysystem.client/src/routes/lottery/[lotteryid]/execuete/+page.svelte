<script lang="ts">
    import { onMount } from 'svelte';
    import { page } from '$app/stores';

    const groupId = $page.params.lotteryid;

    type LotterySlot = {
        lotteryId: string;
        slotId: string | null;
        name: string | null;
        merchandise: string | null;
        numberOfFrames: number;
        deadLine: string | null;
    };

    type WinnerTicket = {
        number: string;
        status: number;
    };

    type WinningModel = {
        slotId: string;
        name: string;
        tickets: WinnerTicket[];
        status: number;
        numberOfFrames: number;
    };

    let slots: LotterySlot[] = [];
    let winningModels: Record<string, WinningModel> = {};

    // LotterySlotデータとWinningModelデータを初期化
    onMount(async () => {
        const encodedid = encodeURIComponent(groupId);
        const res = await fetch(`/api/LotterySlot/List/${encodedid}`);
        slots = await res.json();

        // 各slotIdに対してWinningModelを取得
        for (const slot of slots) {
            if (!slot.slotId) continue;

            const res2 = await fetch(`/api/LotteryExecute/LotterySlotState?slotId=${slot.slotId}`);
            const model = await res2.json();
            winningModels[slot.slotId] = model;
        }
        console.log(winningModels);
        console.log(slots);
    });

    // LotteryActionのラベルを状態によって決定
    function getLotteryActionLabel(status: number): string | null {
        switch (status) {
            case 0:
                return "抽選対象にする"
            case 1:
                return "抽選開始";
            case 2:
                return "数値を決定";
            case 3:
            case 4:
                return "再抽選";
            default:
                return null;
        }
    }

    // 抽選アクション（ボタン押下時の処理）
    function onLotteryAction(slotId: string) {
        console.log(`抽選アクション: ${slotId}`);
        // 必要に応じてAPI呼び出しをここに
    }

    // 引き換え中止（ボタン押下時の処理）
    function onStopExchange(slotId: string) {
        console.log(`引き換え中止: ${slotId}`);
        // 必要に応じてAPI呼び出しをここに
    }

    // WinningModelを取得する関数
    function getWinningModel(slotId: string | null): WinningModel | null {
        if (!slotId) return null;
        return winningModels[slotId] ?? null;
    }
</script>

<style>
    .slot {
        border: 1px solid #ccc;
        padding: 1rem;
        margin-bottom: 1rem;
        border-radius: 8px;
    }

    .actions {
        margin-top: 0.5rem;
    }

    button {
        margin-right: 1rem;
    }
</style>

{#each slots as slot}
<div class="slot">
    <h3>{slot.name}</h3>
    <p>商品: {slot.merchandise}</p>
    <p>枠数: {slot.numberOfFrames}</p>
    <p>締切: {slot.deadLine ?? "未設定"}</p>

    {#if slot.slotId}
    {#if getWinningModel(slot.slotId) as model}
    <div class="actions">
        {#if model.status === "Exchange" || model.status === "ViewResult"}
        <button on:click={() => onStopExchange(slot.slotId)}>引き換えを中止する</button>
        {/if}

        {#if getLotteryActionLabel(model.status) != null}
        <button on:click={() =>
            onLotteryAction(slot.slotId)}>
            {getLotteryActionLabel(model.status)}
        </button>
        {/if}
    </div>
    {/if}
    {/if}
</div>
{/each}
