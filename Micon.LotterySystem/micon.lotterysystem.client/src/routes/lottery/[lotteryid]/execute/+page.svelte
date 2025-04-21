<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { page } from '$app/stores';

    // ページのパラメータが変わったら反応するために$pageを監視
    $: currentGroupId = $page.params.lotteryid;
    let prevGroupId: string | null = null;

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
    let connection: any;
    let connectionReady = false;

    // URLパラメータ（groupId）が変更されたときに反応する
    $: if (currentGroupId && currentGroupId !== prevGroupId && connectionReady) {
        handleGroupChange(currentGroupId);
    }

    async function handleGroupChange(newGroupId: string) {
        console.log(`URL changed: new groupId = ${newGroupId}, previous = ${prevGroupId}`);
        prevGroupId = newGroupId;

        try {
            if (connection.state === "Connected") {
                await connection.invoke("RemoveLotteryGroup", newGroupId);
                await connection.invoke("SetLotteryGroup", newGroupId);
                console.log("SetLotteryGroup invoked after URL change");
                await Load();
            } else {
                console.log("Connection not ready, waiting...");
                connection.onreconnected = async () => {
                    await connection.invoke("RemoveLotteryGroup", newGroupId);
                    await connection.invoke("SetLotteryGroup", newGroupId);
                    await Load();
                };
            }
        } catch (err) {
            console.error("Error during group change:", err);
        }
    }

    onMount(async () => {
        try {
            if (connection) {
                await connection.stop();
                console.log("Stopped existing connection");
            }

            connection = new window.signalR.HubConnectionBuilder()
                .withUrl("/api/LotteryHub")
                .withAutomaticReconnect()
                .build();

            connection.on("SetTarget", async (id: string) => {
                await Load();
                loaded = true;
            });
            connection.on("UpdateStatus", async (id: string) => {
                await Load();
                loaded = true;
            });
            connection.on("AnimationStart", async (id: string) => {
                await Load();
                loaded = true;
                console.log("AnimationStart");
            });

            connection.on("SubmitLottery", async (id: string) => {
                await Load();
                loaded = true;
                console.log("SubmitLottery");
            });

            connection.on("ViewStop", async (id: string) => {
                await Load();
                loaded = true;
                console.log("ViewStop");
            });
            connection.on("ExchangeStop", async (id: string) => {
                await Load();
                loaded = true;
                console.log("ExchangeStop");
            });

            connection.onreconnected = async (connectionId: string) => {
                console.log("Reconnected with ID:", connectionId);
                if (currentGroupId) {
                    await connection.invoke("SetLotteryGroup", currentGroupId);
                    await Load();
                }
            };

            await connection.start();
            console.log("SignalR connected");
            connectionReady = true;

            prevGroupId = currentGroupId;
            await connection.invoke("SetLotteryGroup", currentGroupId);
            console.log("SetLotteryGroup invoked initially");

            await Load();
            loaded = true;
        } catch (err) {
            console.error("SignalR connection setup error:", err);
            loaded = true;
        }
    });

    onDestroy(() => {
        if (connection) {
            connection.stop()
                .then(() => console.log("SignalR connection stopped"))
                .catch(err => console.error("Error stopping SignalR connection:", err));
        }
    });

    async function Load() {
        try {
            const encodedid = encodeURIComponent(currentGroupId);
            const res = await fetch(`/api/LotterySlot/List/${encodedid}`);
            const fetchedSlots: LotterySlot[] = await res.json();

            const newWinningModels: Record<string, WinningModel> = {};
            for (const slot of fetchedSlots) {
                if (!slot.slotId) continue;
                const res2 = await fetch(`/api/LotteryExecute/LotterySlotState?slotId=${slot.slotId}`);
                const model: WinningModel = await res2.json();
                newWinningModels[slot.slotId] = model;
            }

            slots = fetchedSlots;
            winningModels = newWinningModels;
            loaded = true;

            console.log("Loaded (via SignalR):", slots, winningModels);
        } catch (error) {
            console.error("Error loading data:", error);
            loaded = true;
        }
    }

    // LotteryActionのラベルを状態によって決定
    function getLotteryActionLabel(status: number): string | null {
        switch (status) {
            case 0:
                return "抽選対象にする";
            case 1:
                return "抽選開始";
            case 2:
                return "当選番号を確定";
            case 3:
                return "表示を止める";
            case 4:
                return "受付を停止し再抽選";
            case 5:
                return "抽選対象にする";
            default:
                return null;
        }
    }

    // 抽選アクション（ボタン押下時の処理）
    async function onLotteryAction(slotId: string, status: number) {
        console.log(`抽選アクション: ${slotId}`, status);
        let actiontype = "";
        switch (status) {
            case 0:
            case 5:
                actiontype = "TargetSlot";
                break;
            case 1:
                actiontype = "AnimationExecute";
                break;
            case 2:
                actiontype = "LotteryExecute";
                break;
            case 3:
                actiontype = "ViewStop";
                break;
            case 4:
                actiontype = "ExchangeStop";
                break;
            default:
                return;
        }

        try {
            const res = await fetch(`/api/LotteryExecute/${actiontype}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(slotId)
            });
            if (!res.ok) {
                if (res.status === 404) {
                    window.alert("抽選対象の人が存在しません");
                } else {
                    window.alert(`エラーが発生しました (${res.status}): ${res.statusText}`);
                }
                return;
            }
            await Load();
        } catch (err) {
            console.error("抽選実行エラー:", err);
            window.alert("通信エラーが発生しました");
        }
    }

    // 引き換え中止（ボタン押下時の処理）
    async function onStopExchange(slotId: string) {
        console.log(`引き換え中止: ${slotId}`);
        await fetch(`/api/LotteryExecute/ExchangeStop`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(slotId)
        });
        await Load();
    }

    // WinningModelを取得する関数
    function getWinningModel(slotId: string | null): WinningModel | null {
        if (!slotId) return null;
        return winningModels[slotId] ?? null;
    }

    // チケットのステータスに応じた背景色クラスを返す関数
    function getTicketStatusClass(status: number): string {
        switch (status) {
            case 2:
                return 'status-green';  // 当選後
            case 3:
                return 'status-blue';   // 表示中止など
            case 4:
                return 'status-gray';   // 引換後
            default:
                return '';
        }
    }

    function openNewWindow() {
        window.open(
            './view',
            '_blank',
            'width=600,height=400,left=100,top=100,noopener,noreferrer'
        );
    }
</script>

<style>
    .slot {
        background-color: #f9f9f9;
        border: 1px solid #ddd;
        padding: 1.5rem;
        margin-bottom: 1.5rem;
        border-radius: 12px;
        box-shadow: 0 4px 10px rgba(0, 0, 0, 0.04);
        transition: transform 0.2s ease;
    }

        .slot:hover {
            transform: translateY(-2px);
        }

    .actions {
        margin-top: 1rem;
        display: flex;
        gap: 1rem;
        flex-wrap: wrap;
    }

    button {
        padding: 0.6rem 1.2rem;
        font-size: 0.95rem;
        font-weight: 500;
        color: #ffffff;
        background-color: #3a7d7c;
        border: none;
        border-radius: 8px;
        cursor: pointer;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
        transition: background-color 0.2s ease, transform 0.2s ease;
    }

        button:hover {
            background-color: #316a69;
            transform: translateY(-1px);
        }

    .fancy-button {
        margin-bottom: 1.5rem;
        background-color: #2c6e6b;
        padding: 0.7rem 1.4rem;
        font-size: 1rem;
        border-radius: 10px;
        transition: all 0.2s ease;
        box-shadow: 0 4px 12px rgba(0,0,0,0.1);
    }

        .fancy-button:hover {
            background-color: #225c5a;
            transform: scale(1.03);
        }

    .tickets-container {
        margin-top: 1rem;
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
    }

    .ticket {
        padding: 0.5rem 0.7rem;
        border: 1px solid #ccc;
        border-radius: 6px;
        background-color: #ffffff;
        font-weight: 600;
        color: #444;
        min-width: 60px;
        text-align: center;
    }

    .status-green {
        background-color: #c3e6cb; /* 穏やかなグリーン */
        border-color: #b1dfbb;
        color: #256029;
    }

    .status-blue {
        background-color: #d1ecf1; /* 穏やかなブルー */
        border-color: #bee5eb;
        color: #0c5460;
    }

    .status-gray {
        background-color: #e2e3e5; /* グレイアウト */
        border-color: #d6d8db;
        color: #6c757d;
    }
</style>

{#if loaded}
<button class="fancy-button" onclick={openNewWindow}>
    結果画面を開く
</button>

{#each slots as slot}
<div class="slot">
    <h3>{slot.name}</h3>
    <p>商品: {slot.merchandise}</p>
    <p>枠数: {slot.numberOfFrames}</p>
    <p>締切: {slot.deadLine ?? "未設定"}</p>

    {#if slot.slotId && getWinningModel(slot.slotId)}
    {#if getWinningModel(slot.slotId).tickets.length > 0}
    <div>
        <h4>チケット一覧</h4>
        <div class="tickets-container">
            {#each getWinningModel(slot.slotId).tickets as ticket}
            <div class="ticket {getTicketStatusClass(ticket.status)}">
                {ticket.number}
            </div>
            {/each}
        </div>
    </div>
    {/if}

    <div class="actions">
        {#if [3,4].includes(getWinningModel(slot.slotId).status)}
        <button class="fancy-button" onclick={async ()=>
            await onStopExchange(slot.slotId)}>
            引き換えを中止する
        </button>
        {/if}

        {#if getLotteryActionLabel(getWinningModel(slot.slotId).status)}
        <button class="fancy-button" onclick={async ()=>
            await onLotteryAction(slot.slotId, getWinningModel(slot.slotId).status)}>
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
