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
        const response = await fetch('/api/LotteryExecute/ExecutingSlotState?groupId='+lotteryId);
        const data = await response.json();

        prizeLevel = data.name;
        console.log(data);

        console.log(data.tickets);

        const allTickets = data.tickets.slice(0, data.numberOfFrames);
        console.log(allTickets);
        resultNumbers = allTickets
            .filter(t => t.status === 2)
            .map(t => t.number);

        exchangedNumbers = allTickets
            .filter(t => t.status === 4)
            .map(t => t.number);

        displayedNumbers = resultNumbers.map(() => num.toString().slice(-4).split(''));
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

        connection.on("UpdateStatus", async (id) => {
            await fetchWinningModel()
            loaded = true;
        });

        connection.on("SubmitLottery", async (id) => {
            await fetchWinningModel(); // 確定情報で再取得
            stopDrawing();
            console.log("SubmitLottery");
            
        });

        connection.on("ViewStop", (id) => {
            visible = true;
            console.log("ViewStop");
            
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
	
	p {
	    text-align: center;
	}
	
	.result {
	    text-align: center;
		font-size: 2rem;
		font-weight: bold;
	}
	
	.heading {
	    font-weight: bold;
	}
</style>

{#if visible}
<div class="container">
	<h2>あなたの抽選結果</h2>
	<p class="result">当選！</p>

	<div>
		<p class="heading">あなたの抽選券番号</p>
		<p>1234567</p>
	</div>

	<p>
		<!-- TODO: ページ遷移先を変更 -->
		<a href="/login">当選番号の結果はこちら</a>
	</p>
	
</div>
{/if}
