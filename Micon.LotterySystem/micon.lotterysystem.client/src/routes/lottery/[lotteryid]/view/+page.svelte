<script>
    import { onMount } from 'svelte';
    import { page } from '$app/stores';

    let connection;
    let prizeLevel = "1等";
    let resultNumbers = [];        // アニメーション対象
    let exchangedNumbers = [];     // 引き換え済み
    let spinning = false;
    let displayedNumbers = [];
    let visible = true;

    const digitCount = 4;
    let intervals = [];
    const lotteryId = $page.params.lotteryid;

    const fetchWinningModel = async () => {
        const response = await fetch('/api/LotteryExecute');
        const data = await response.json();

        prizeLevel = data.name;

        const allTickets = data.tickets.slice(0, data.numberOfFrames);

        resultNumbers = allTickets
            .filter(t => t.status === "Winner")
            .map(t => t.number);

        exchangedNumbers = allTickets
            .filter(t => t.status === "Exchanged")
            .map(t => t.number);

        displayedNumbers = resultNumbers.map(() => Array(digitCount).fill("?"));
    };

    const startDrawing = () => {
        spinning = true;
        displayedNumbers = resultNumbers.map(() => Array(digitCount).fill("?"));
        intervals = [];

        resultNumbers.forEach((number, idx) => {
            for (let i = 0; i < digitCount; i++) {
                const interval = setInterval(() => {
                    displayedNumbers[idx][i] = Math.floor(Math.random() * 10);
                }, 40 + i * 30);
                intervals.push({ idx, i, interval });
            }
        });
    };

    const stopDrawing = () => {
        intervals.forEach(({ idx, i, interval }) => {
            clearInterval(interval);
            displayedNumbers[idx][i] = resultNumbers[idx][i];
        });
        intervals = [];
        spinning = false;
    };

    onMount(() => {
        connection = new window.signalR.HubConnectionBuilder()
            .withUrl("/api/LotteryHub")
            .withAutomaticReconnect()
            .build();

        connection.start().then(() => {
            console.log("SignalR connected");
            connection.invoke("SetLotteryGroup", lotteryId)
                .then(() => console.log("SetLotteryGroup invoked"))
                .catch(err => console.error("SetLotteryGroup error:", err));
        }).catch(err => console.error("SignalR connection error:", err));

        connection.on("SetTarget", async () => {
            await fetchWinningModel();
            visible = true;
        });

        connection.on("AnimationStart", () => {
            startDrawing();
        });

        connection.on("SubmitLottery", async () => {
            await fetchWinningModel(); // 確定情報で再取得
            stopDrawing();
        });

        connection.on("ViewStop", () => {
            visible = false;
        });

        connection.on("ExchangeStop", () => {
            // 必要に応じて処理追加
        });
    });
</script>


<style>
    .container {
        display: flex;
        flex-direction: column;
        align-items: center;
    }

    .grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
        gap: 1.5rem;
        width: 100%;
        max-width: 1200px;
        padding: 2rem;
    }

    .card {
        background: white;
        border-radius: 12px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.1);
        padding: 1rem;
        display: flex;
        flex-direction: column;
        align-items: center;
        transition: transform 0.3s ease;
    }

        .card:hover {
            transform: scale(1.03);
        }

    .digits {
        display: flex;
        gap: 0.4rem;
        margin-top: 0.5rem;
    }

    .digit {
        font-size: 2rem;
        font-weight: bold;
        width: 2.8rem;
        height: 2.8rem;
        line-height: 2.8rem;
        text-align: center;
        border: 2px solid #333;
        border-radius: 6px;
        background: #f0f0f0;
        box-shadow: inset 0 1px 3px rgba(0,0,0,0.1);
    }

    .label {
        font-size: 1rem;
        color: #666;
    }
</style>

{#if visible}
<div class="container">
    <h2>{prizeLevel} 抽選中！</h2>

    <div class="grid">
        {#each displayedNumbers as digits, index}
        <div class="card">
            <div class="label">No.{index + 1}</div>
            <div class="digits">
                {#each digits as num}
                <div class="digit">{num}</div>
                {/each}
            </div>
        </div>
        {/each}

        {#each exchangedNumbers as number, index}
        <div class="card" style="opacity: 0.5;">
            <div class="label">引換済 No.{displayedNumbers.length + index + 1}</div>
            <div class="digits">
                {#each number as numChar (numChar)}
                <div class="digit">{numChar}</div>
                {/each}
            </div>
        </div>
        {/each}
    </div>
</div>
{/if}

