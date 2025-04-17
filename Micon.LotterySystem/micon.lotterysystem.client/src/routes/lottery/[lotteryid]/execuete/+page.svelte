<script>
    import { onMount } from 'svelte';

    export let prizeLevel = "1等";
    export let resultNumbers = [
        "1234", "5678", "9012", "3456", "7890",
        "1111", "2222", "3333", "4444", "5555",
        "6666", "7777", "8888", "9999", "0000",
        "1212", "3434", "5656", "7878", "9090"
    ].slice(0, 20); // 上限20人

    let spinning = false;
    let displayedNumbers = [];

    const digitCount = 4;

    const startDrawing = () => {
        spinning = true;
        displayedNumbers = resultNumbers.map(() => Array(digitCount).fill("?"));

        resultNumbers.forEach((number, idx) => {
            for (let i = 0; i < digitCount; i++) {
                const interval = setInterval(() => {
                    displayedNumbers[idx][i] = Math.floor(Math.random() * 10);
                }, 40 + i * 30);

                setTimeout(() => {
                    clearInterval(interval);
                    displayedNumbers[idx][i] = number[i];
                    if (idx === resultNumbers.length - 1 && i === digitCount - 1) {
                        spinning = false;
                    }
                }, 1000 + idx * 100 + i * 300);
            }
        });
    };
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
    </div>
</div>

