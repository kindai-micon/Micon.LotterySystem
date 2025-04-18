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

    let loaded = false;

    onMount(async () => {
        const encodedid = encodeURIComponent(groupId);
        const res = await fetch(`/api/LotterySlot/List/${encodedid}`);
        slots = await res.json();

        for (const slot of slots) {
            if (!slot.slotId) continue;

            const res2 = await fetch(`/api/LotteryExecute/LotterySlotState?slotId=${slot.slotId}`);
            const model = await res2.json();
            winningModels[slot.slotId] = model;
        }

        loaded = true;
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
    async function onLotteryAction(slotId: string, status:number) {
        console.log(`抽選アクション: ${slotId}`);
        console.log(status);
        let actiontype = ""
        switch (status) {
            case 0:
                actiontype = "TargetSlot";
                break;
            case 1:
                actiontype = "AnimationExecute";
                break;
            case 2:
                actiontype = "LotteryExecute";
                break;
            case 3:
            case 4:
                actiontype = "ExchangeStop";
                break;
            default:
                return null;
        }

        console.log(actiontype);
         await fetch(`/api/LotteryExecute/${actiontype}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify( slotId )
        });
        // 必要に応じてAPI呼び出しをここに
    }

    // 引き換え中止（ボタン押下時の処理）
    async function onStopExchange(slotId: string) {
        console.log(`引き換え中止: ${slotId}`);
        await fetch(`/api/LotteryExecute/ExchangeStop`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(slotId)
        });
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

{#if loaded}
  {#each slots as slot}
<div class="slot">
    <h3>{slot.name}</h3>
    <p>商品: {slot.merchandise}</p>
    <p>枠数: {slot.numberOfFrames}</p>
    <p>締切: {slot.deadLine ?? "未設定"}</p>

    {#if slot.slotId && getWinningModel(slot.slotId) }
    <div class="actions">
        {#if getWinningModel(slot.slotId).status === "Exchange" || getWinningModel(slot.slotId) .status === "ViewResult"}
        <button on:click={async() => await onStopExchange(slot.slotId)}>引き換えを中止する</button>
        {/if}

        {#if getLotteryActionLabel(getWinningModel(slot.slotId).status) != null}
        <button on:click={async() =>
            await onLotteryAction(slot.slotId,getWinningModel(slot.slotId).status)}>
            {getLotteryActionLabel(getWinningModel(slot.slotId).status)}
        </button>
        {/if}
    </div>
    {/if}
</div>
  {/each}
{:else}
<p>読み込み中...</p>
{/if}

