<script lang="ts">
    import { onMount } from 'svelte';
    import { page } from '$app/stores';
    let prevGroupId = $page.params.lotteryid;


    $: currentGroupId = $page.params.lotteryid;

    interface WinnerTicket {
        number: string;
        status: number;
    }

    interface WinningModel {
        slotId: string;
        name: string;
        tickets: WinnerTicket[];
        status: number;
        numberOfFrames: number;
    }

    interface LotterySlots {
        lotteryId: string;
        slotId?: string;
        name?: string;
        merchandise?: string;
        numberOfFrames: number;
        deadline?: string;
        winning?: WinningModel;
    }

    let slots: LotterySlots[] = [];
    let loading = true;
    // URLパラメータ（groupId）が変更されたときに反応する
    $: if (currentGroupId && currentGroupId !== prevGroupId && connectionReady) {
        handleGroupChange(currentGroupId);
    }
    async function handleGroupChange(newGroupId) {
        console.log(`URL changed: new groupId = ${newGroupId}, previous = ${prevGroupId}`);
        prevGroupId = newGroupId;

        try {
            // 接続状態をチェックし、接続されている場合のみinvokeを実行
            if (connection.state === "Connected") {
                await connection.invoke("RemoveLotteryGroup", newGroupId);
                await connection.invoke("SetLotteryGroup", newGroupId);
                console.log("SetLotteryGroup invoked after URL change");
                await fetchLotteryData();
            } else {
                console.log("Connection not ready, waiting...");
                // 接続が確立していない場合は、接続状態が変わるのを待つ
                connection.onreconnected = async () => {
                    await connection.invoke("RemoveLotteryGroup", newGroupId);
                    await connection.invoke("SetLotteryGroup", newGroupId);
                    await fetchLotteryData();
                };
            }
        } catch (err) {
            console.error("Error during group change:", err);
        }
    }

    async function fetchLotteryData(): Promise<LotterySlots[]> {
        const slotRes = await fetch('/api/LotterySlot/List/'+currentGroupId);
        const newslots: LotterySlots[] = await slotRes.json();
        loading = true;
        const withWinning = await Promise.all(
            newslots.map(async (slot) => {
                if (!slot.slotId) return slot;

                try {
                    const res = await fetch(`/api/LotteryExecute/${slot.slotId}`);
                    if (!res.ok) throw new Error('Failed to fetch winning data');

                    const winning: WinningModel = await res.json();
                    return { ...slot, winning };
                } catch (err) {
                    console.error(`Failed to fetch winning for slotId: ${slot.slotId}`, err);
                    return slot;
                }
            })
        );
        loading = false;
        slots = newslots;
    }

    onMount(async () => {
        try {
            // 既存の接続があれば停止
            if (connection) {
                await connection.stop();
                console.log("Stopped existing connection");
            }

            connection = new window.signalR.HubConnectionBuilder()
                .withUrl("/api/LotteryHub")
                .withAutomaticReconnect()
                .build();

            connection.on("UpdateStatus", async (id) => {
                await fetchLotteryData();
                loading = false;
            });

            connection.on("SubmitLottery", async (id) => {
                await fetchLotteryData();
                loading = false;

                console.log("SubmitLottery");
            });


            connection.on("ExchangeStop", async (id) => {
                await fetchLotteryData();
                loading = false;

                console.log("ViewStop")
            })

            // 重複したイベントハンドラを削除（2回ViewStopが登録されていた）

            // 接続が確立したときに実行
            connection.onreconnected = async (connectionId) => {
                console.log("Reconnected with ID:", connectionId);
                if (currentGroupId) {
                    await connection.invoke("SetLotteryGroup", currentGroupId);
                    await fetchLotteryData();
                    loading = false;
                }
            };

            await connection.start();
            console.log("SignalR connected");

            // 初期接続時にグループIDを設定
            prevGroupId = currentGroupId;
            await connection.invoke("SetLotteryGroup", currentGroupId);
            console.log("SetLotteryGroup invoked initially");
        }
        catch (err) {
            console.error("SignalR connection setup error:", err);
            
        }
        slots = await fetchLotteryData();
        loading = false;
    });
</script>

<style>
    .container {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
        gap: 1.5rem;
        padding: 2rem;
    }

    .card {
        background: #fff;
        border-radius: 12px;
        padding: 1.5rem;
        box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
        border: 1px solid #e0e0e0;
        transition: transform 0.2s;
    }

        .card:hover {
            transform: translateY(-4px);
        }

        .card h2 {
            font-size: 1.25rem;
            margin-bottom: 0.5rem;
            color: #333;
        }

    .info {
        font-size: 0.95rem;
        margin-bottom: 0.25rem;
        color: #666;
    }

    .section-title {
        margin-top: 1rem;
        font-weight: bold;
        color: #444;
    }

    .ticket-list {
        margin-top: 0.5rem;
        padding-left: 1rem;
        list-style: disc;
    }

    .ticket-item {
        font-size: 0.9rem;
        color: #333;
        margin-bottom: 0.3rem;
    }

    .status-tag {
        display: inline-block;
        padding: 0.2rem 0.5rem;
        border-radius: 6px;
        font-size: 0.8rem;
        background-color: #f0f0f0;
        color: #333;
        margin-left: 0.5rem;
    }
</style>

{#if loading}
<p style="padding: 2rem;">読み込み中...</p>
{:else}
<div class="container">
    {#each slots as slot}
    <div class="card">
        <h2>{slot.name}</h2>
        <div class="info">景品: {slot.merchandise}</div>
        <div class="info">枠数: {slot.numberOfFrames}</div>
        <div class="info">締切: {slot.deadline || '未設定'}</div>

        {#if slot.winning}
        <div class="section-title">抽選結果</div>
        <div class="info">
            ステータス:
            <span class="status-tag">{slot.winning.status}</span>
        </div>
        <div class="info">当選数: {slot.winning.tickets.length}</div>
        <ul class="ticket-list">
            {#each slot.winning.tickets as ticket}
            <li class="ticket-item">
                番号: {ticket.number}
                <span class="status-tag">ステータス: {ticket.status}</span>
            </li>
            {/each}
        </ul>
        {:else}
        <div class="section-title">まだ抽選されていません</div>
        {/if}
    </div>
    {/each}
</div>
{/if}
