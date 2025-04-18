<script>
    import { onMount } from 'svelte';
    import { page } from '$app/stores';

    let connection;
    let prizeLevel = "";
    let resultNumbers = [];
    let exchangedNumbers = [];
    let spinning = false;
    let displayedNumbers = [];
    let visible = false;

    const digitCount = 4;
    let intervals = [];
    const lotteryId = $page.params.lotteryid;
    const fetchWinningModel = async () => {
        const response = await fetch('/api/LotteryExecute/ExecutingSlotState?groupId=' + lotteryId);
        const data = await response.json();

        prizeLevel = data.name;

        const frameCount = data.numberOfFrames;
        const allTickets = data.tickets || [];
        if (data.status == 2) {
            startDrawing();
        }
        // 初期表示は全て "-" で埋める
        displayedNumbers = Array.from({ length: frameCount }, () =>
            Array(digitCount).fill("-")
        );

        // 確定済み番号 (status === 2)
        resultNumbers = allTickets
            .filter(t => t.status === 2)
            .map(t => t.number.toString().padStart(digitCount, '0'));

        // 確定している枠にだけ番号を表示
        allTickets.forEach((t, index) => {
            if (t.status === 2) {
                const digits = t.number.toString().padStart(digitCount, '0').split('');
                displayedNumbers[index] = digits;
            }
        });

        // 引換済み枠（別枠として下に表示）
        exchangedNumbers = allTickets
            .filter(t => t.status === 4)
            .map(t => t.number.toString().padStart(digitCount, '0').split(''));
    };


    const startDrawing = () => {
        spinning = true;

        // 表示枠数だけ "?" を用意（初期状態）


        intervals.forEach(({ interval }) => clearInterval(interval)); // 既存をクリア
        intervals = [];

        displayedNumbers.forEach((_, idx) => {
            for (let i = 0; i < digitCount; i++) {
                const interval = setInterval(() => {
                    displayedNumbers[idx][i] = Math.floor(Math.random() * 10).toString();
                }, 40 + i * 30); // 少しずつズレるとスロット感が出る
                intervals.push({ idx, i, interval });
            }
        });
    };

    const stopDrawing = () => {
        intervals.forEach(({ interval }) => clearInterval(interval));
        intervals = [];

        // 確定番号で表示を上書き
        displayedNumbers = resultNumbers.map(num =>
            num.toString().padStart(digitCount, '0').split('')
        );

        spinning = false;
    };
    async function handleGroupChange(newGroupId) {
        console.log(`URL changed: new groupId = ${newGroupId}, previous = ${prevGroupId}`);
        prevGroupId = newGroupId;

        try {
            // 接続状態をチェックし、接続されている場合のみinvokeを実行
            if (connection.state === "Connected") {
                await connection.invoke("RemoveLotteryGroup", newGroupId);

                await connection.invoke("SetLotteryGroup", newGroupId);

                console.log("SetLotteryGroup invoked after URL change");
                await Load();
            } else {
                console.log("Connection not ready, waiting...");
                // 接続が確立していない場合は、接続状態が変わるのを待つ
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
            await fetchWinningModel();
            visible = true;
            console.log("SetTarget");
        } catch (error) {
            console.error("Error in fetchWinningModel:", error);
        }
        connection = new window.signalR.HubConnectionBuilder()
            .withUrl("/api/LotteryHub")
            .withAutomaticReconnect()
            .build();

        connection.on("SetTarget", async (id) => {
            try {
                await fetchWinningModel();
                visible = true;
                console.log("SetTarget");
            } catch (error) {
                console.error("Error in fetchWinningModel:", error);
            }
        });

        connection.on("AnimationStart", () => {
            startDrawing();
            console.log("AnimationStart");
        });

        connection.on("UpdateStatus", async (id) => {
            try {
                await fetchWinningModel();
                visible = true;
                console.log("UpdateStatus");
            } catch (error) {
                console.error("Error in fetchWinningModel:", error);
            }
        });
        connection.on("SubmitLottery", async () => {
            await fetchWinningModel();
            stopDrawing();
            console.log("SubmitLottery");
        });

        connection.on("ViewStop", () => {
            visible = false;
            console.log("ViewStop");

        });

        connection.on("ExchangeStop", () => {
            visible = false;
            console.log("ExchangeStop");
        });

        connection.start().then(() => {
            console.log("SignalR connected");
            connection.invoke("SetLotteryGroup", lotteryId)
                .then(() => console.log("SetLotteryGroup invoked"))
                .catch(err => console.error("SetLotteryGroup error:", err));
        }).catch(err => console.error("SignalR connection error:", err));
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
<h1>抽選結果画面</h1>
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
{:else}
<h2>現在抽選中のものはありません</h2>
{/if}

