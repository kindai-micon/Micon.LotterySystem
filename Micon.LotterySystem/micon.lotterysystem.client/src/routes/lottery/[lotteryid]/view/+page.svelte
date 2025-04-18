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
    let prevGroupId = lotteryId;

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
                await fetchWinningModel();
            } else {
                console.log("Connection not ready, waiting...");
                // 接続が確立していない場合は、接続状態が変わるのを待つ
                connection.onreconnected = async () => {
                    await connection.invoke("RemoveLotteryGroup", newGroupId);
                    await connection.invoke("SetLotteryGroup", newGroupId);
                    await fetchWinningModel();
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
    :root {
        --primary-color: #3498db;
        --secondary-color: #2ecc71;
        --accent-color: #f39c12;
        --dark-color: #2c3e50;
        --light-color: #ecf0f1;
        --shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
        --transition: all 0.3s ease;
    }

    .lottery-container {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 1rem;
        max-width: 1400px;
        margin: 0 auto;
        font-family: 'Hiragino Kaku Gothic Pro', 'Meiryo', sans-serif;
    }

    .lottery-header {
        text-align: center;
        margin-bottom: 1.5rem;
        width: 100%;
    }

    .lottery-title {
        font-size: 2.5rem;
        color: var(--dark-color);
        margin-bottom: 0.5rem;
        font-weight: 800;
    }

    .prize-level {
        font-size: 1.8rem;
        background: linear-gradient(135deg, var(--primary-color), var(--secondary-color));
        color: white;
        padding: 0.5rem 2rem;
        border-radius: 50px;
        display: inline-block;
        margin: 1rem 0;
        box-shadow: var(--shadow);
        animation: pulse 2s infinite;
    }

    @keyframes pulse {
        0% {
            box-shadow: 0 0 0 0 rgba(52, 152, 219, 0.7);
        }

        70% {
            box-shadow: 0 0 0 15px rgba(52, 152, 219, 0);
        }

        100% {
            box-shadow: 0 0 0 0 rgba(52, 152, 219, 0);
        }
    }

    .lottery-grid {
        display: grid;
        grid-template-columns: repeat(auto-fill, minmax(180px, 1fr));
        gap: 1.5rem;
        width: 100%;
        margin-top: 1rem;
    }

    .lottery-card {
        background: white;
        border-radius: 16px;
        box-shadow: var(--shadow);
        padding: 1.2rem;
        display: flex;
        flex-direction: column;
        align-items: center;
        transition: var(--transition);
        border: 1px solid rgba(0, 0, 0, 0.05);
    }

        .lottery-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 12px 20px rgba(0, 0, 0, 0.15);
        }

        .lottery-card.exchanged {
            background: rgba(255, 255, 255, 0.7);
            border: 1px dashed rgba(0, 0, 0, 0.2);
        }

    .card-label {
        font-size: 0.9rem;
        font-weight: 600;
        color: var(--dark-color);
        background: var(--light-color);
        padding: 0.3rem 0.8rem;
        border-radius: 20px;
        margin-bottom: 0.8rem;
    }

    .exchanged .card-label {
        background: rgba(236, 240, 241, 0.5);
        color: rgba(44, 62, 80, 0.6);
    }

    .digits-container {
        display: flex;
        gap: 0.4rem;
        justify-content: center;
    }

    .digit {
        font-size: 1.8rem;
        font-weight: bold;
        width: 2.5rem;
        height: 3rem;
        line-height: 3rem;
        text-align: center;
        border: 2px solid var(--dark-color);
        border-radius: 8px;
        background: linear-gradient(to bottom, #ffffff, #f2f2f2);
        box-shadow: inset 0 1px 3px rgba(0, 0, 0, 0.1);
        transition: var(--transition);
    }

        .digit.spinning {
            animation: spin 0.5s infinite;
            background: linear-gradient(to bottom, #e9f7fe, #d6eeff);
            border-color: var(--primary-color);
            color: var(--primary-color);
        }

    @keyframes spin {
        0% {
            transform: translateY(-2px);
        }

        50% {
            transform: translateY(2px);
        }

        100% {
            transform: translateY(-2px);
        }
    }

    .exchanged .digit {
        color: rgba(44, 62, 80, 0.5);
        border-color: rgba(44, 62, 80, 0.3);
        background: rgba(242, 242, 242, 0.5);
    }

    .empty-state {
        text-align: center;
        padding: 3rem;
        background: var(--light-color);
        border-radius: 16px;
        box-shadow: var(--shadow);
        margin: 2rem auto;
        max-width: 600px;
    }

    .empty-message {
        font-size: 1.5rem;
        color: var(--dark-color);
        margin-bottom: 1rem;
    }

    /* レスポンシブ対応 */
    @media (max-width: 768px) {
        .lottery-grid {
            grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
            gap: 1rem;
        }

        .lottery-title {
            font-size: 2rem;
        }

        .prize-level {
            font-size: 1.5rem;
            padding: 0.4rem 1.5rem;
        }

        .digit {
            font-size: 1.5rem;
            width: 2rem;
            height: 2.5rem;
            line-height: 2.5rem;
        }
    }

    @media (max-width: 480px) {
        .lottery-grid {
            grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
            gap: 0.8rem;
        }

        .lottery-title {
            font-size: 1.8rem;
        }

        .prize-level {
            font-size: 1.2rem;
            padding: 0.3rem 1.2rem;
        }

        .digit {
            font-size: 1.2rem;
            width: 1.6rem;
            height: 2.2rem;
            line-height: 2.2rem;
        }

        .card-label {
            font-size: 0.8rem;
        }
    }
</style>

<div class="lottery-container">
    <div class="lottery-header">
        <h1 class="lottery-title">抽選結果画面</h1>
        {#if visible && prizeLevel}
        <div class="prize-level">{prizeLevel} 抽選中！</div>
        {/if}
    </div>

    {#if visible}
    <div class="lottery-grid">
        {#each displayedNumbers as digits, index}
        <div class="lottery-card">
            <div class="card-label">No.{index + 1}</div>
            <div class="digits-container">
                {#each digits as num}
                <div class="digit {spinning ? 'spinning' : ''}">{num}</div>
                {/each}
            </div>
        </div>
        {/each}

        {#each exchangedNumbers as number, index}
        <div class="lottery-card exchanged">
            <div class="card-label">引換済 No.{displayedNumbers.length + index + 1}</div>
            <div class="digits-container">
                {#each number as numChar}
                <div class="digit">{numChar}</div>
                {/each}
            </div>
        </div>
        {/each}
    </div>
    {:else}
    <div class="empty-state">
        <h2 class="empty-message">現在抽選中のものはありません</h2>
        <p>抽選が開始されるまでお待ちください</p>
    </div>
    {/if}
</div>