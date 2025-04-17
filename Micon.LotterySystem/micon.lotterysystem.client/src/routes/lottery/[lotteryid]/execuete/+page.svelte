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

<h2>{prizeLevel} 抽選中！</h2>

<div class="grid">
    {#each displayedNumbers as digits, i}
    <div class="slot">
        {#each digits as num}
        <div class="digit">{num}</div>
        {/each}
    </div>
    {/each}
</div>

{#if !spinning}
<button on:click={startDrawing}>抽選スタート</button>
{:else}
<p>抽選中...</p>
{/if}

<style>
    .grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
        gap: 1rem;
        margin: 2rem 0;
    }

    .slot {
        display: flex;
        justify-content: center;
        gap: 0.3rem;
        padding: 0.5rem;
        border: 2px solid #333;
        border-radius: 8px;
        background: #f9f9f9;
        box-shadow: 0 2px 8px rgba(0,0,0,0.1);
    }

    .digit {
        font-size: 2rem;
        font-weight: bold;
        width: 2.5rem;
        height: 2.5rem;
        text-align: center;
        line-height: 2.5rem;
        border: 1px solid #ccc;
        border-radius: 6px;
        background: white;
        box-shadow: inset 0 1px 2px rgba(0,0,0,0.1);
    }

    button {
        font-size: 1.2rem;
        padding: 0.6rem 2rem;
        cursor: pointer;
    }
</style>
