<script lang="ts">
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import TicketCustomizer from '$lib/components/TicketCustomizer.svelte';

	const lotteryId = $page.params.lotteryid;
	let lotteryName = "";

	onMount(async () => {
		let res = await fetch(`/api/LotteryGroup/Name?id=${lotteryId}`);
		lotteryName = await res.text();
	});
</script>

<style>
	.container {
		padding: 2rem;
		max-width: 1000px;
		margin: 0 auto;
	}

	.title {
		font-size: 2rem;
		font-weight: bold;
		margin-bottom: 1.5rem;
		padding-left: 1rem;
		background: linear-gradient(to right, #f0f8ff, transparent);
	}

	.section {
		margin-bottom: 2rem;
	}

	.section-title {
		font-size: 1.2rem;
		font-weight: bold;
		margin-bottom: 1rem;
		padding: 0.5rem;
		background-color: #f0f8ff;
		border-left: 4px solid #007acc;
	}

	.description {
		color: #666;
		margin-bottom: 1rem;
	}
</style>

<div class="container">
	<div class="title">抽選券設定: {lotteryName}</div>

	<div class="section">
		<div class="section-title">抽選券カスタマイズ</div>
		<p class="description">抽選券のデザインと表示内容をカスタマイズできます。変更後は「保存」ボタンをクリックしてください。</p>
		<TicketCustomizer {lotteryId} lotteryName={lotteryName} />
	</div>
</div>
