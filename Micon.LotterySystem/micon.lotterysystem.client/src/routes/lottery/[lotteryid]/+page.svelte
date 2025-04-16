<script lang="ts">
    import { page } from '$app/stores';
    import { derived } from 'svelte/store';
    import { onMount } from 'svelte';
    // URLパラメータから抽選会名を取得
    const lotteryId = $page.params.lotteryid;

    let lotteryName = "";
    onMount(async () => {
        let res = await fetch(`/api/LotteryGroup/Name?id=${lotteryId}`);
        console.log(res);

        lotteryName = await res.text();

        console.log(lotteryName);

    });
</script>

<style>
    .container {
        padding: 2rem;
        max-width: 800px;
        margin: 0 auto;
    }

    .title {
        font-size: 2rem;
        font-weight: bold;
        margin-bottom: 1.5rem;
        padding-left: 1rem;
        background: linear-gradient(to right, #f0f8ff, transparent);
    }

    .nav {
        display: grid;
        gap: 1rem;
        margin-top: 2rem;
    }

    .link-card {
        display: block;
        padding: 1rem 1.5rem;
        border-radius: 0.75rem;
        background: #f9f9f9;
        border: 1px solid #ddd;
        transition: all 0.2s ease;
        text-decoration: none;
        color: #333;
        font-size: 1.1rem;
        font-weight: 500;
    }

        .link-card:hover {
            background: #e9f5ff;
            border-color: #007acc;
            box-shadow: 0 2px 8px rgba(0,0,0,0.08);
        }

    .desc {
        font-size: 0.9rem;
        color: #666;
        margin-top: 0.25rem;
    }
</style>

<div class="container">
    <div class="title">
        抽選会: {lotteryName}
    </div>

    <div class="nav">
        <a class="link-card" href="{lotteryId}/slot">
            抽選枠の管理
            <div class="desc">当選人数などについて設定できます</div>
        </a>

        <a class="link-card" href="{lotteryId}/publishing">
            抽選券の発行
            <div class="desc">抽選券を発行できます</div>
        </a>

        <a class="link-card" href="{lotteryId}/execuete">
            抽選の実行
            <div class="desc">実際に抽選を行い当選者を決定</div>
        </a>

        <a class="link-card" href="{lotteryId}/enable">
            抽選券の有効化
            <div class="desc">発行した抽選券のQRコードを読み込み有効化します</div>
        </a>
        <a class="link-card" href="{lotteryId}/disable">
            抽選券の有効化
            <div class="desc">有効化した抽選券のQRコードを読み込み無効化します</div>
        </a>

        <a class="link-card" href="{lotteryId}/exchange">
            当選した抽選券の交換
            <div class="desc">当選した抽選券のQRコードを読み込み引き換えを行います</div>
        </a>
    </div>
</div>
