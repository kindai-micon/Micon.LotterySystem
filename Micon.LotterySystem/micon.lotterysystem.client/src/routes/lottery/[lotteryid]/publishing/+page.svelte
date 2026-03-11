<script lang="ts">
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import TicketCustomizer from '$lib/components/TicketCustomizer.svelte';

	const lotteryId = $page.params.lotteryid;
	let lotteryName = "";
	let issueCount = 10;
	let totalIssued = 0;

	let isGenerating = false;

	// カスタマイズセクションの展開状態
	let isCustomizeOpen = $state(false);

	type LogEntry = {
		issuer: string;
		date: string;
		count: number;
		startNumber: number;
		endNumber: number;
	};

	let logs: LogEntry[] = [];

	onMount(async () => {
		let res = await fetch(`/api/LotteryGroup/Name?id=${lotteryId}`);
		lotteryName = await res.text();

		await refreshLogs();
	});

	async function generateTickets() {
		isGenerating = true;
		try {
			const response = await fetch('/api/pdf/generate', {
				method: 'POST',
				headers: {
					'Content-Type': 'application/json'
				},
				body: JSON.stringify({
					count: issueCount,
					lotteryGroupId: lotteryId
				})
			});

			if (!response.ok) {
				alert("PDFの生成に失敗しました");
				return;
			}

			const blob = await response.blob();
			const url = window.URL.createObjectURL(blob);
			const a = document.createElement('a');
			a.href = url;
			a.download = '抽選券.pdf';
			document.body.appendChild(a);
			a.click();
			document.body.removeChild(a);

			await refreshLogs();
		} catch (err) {
			console.error("エラー:", err);
			alert("予期せぬエラーが発生しました");
		} finally {
			isGenerating = false;
		}
	}

	async function refreshLogs() {
		const logRes = await fetch(`/api/pdf/logs?lotteryGroupId=${lotteryId}`);
		if (logRes.ok) {
			const data = await logRes.json();

			logs = data.map((entry: any) => ({
				issuer: entry.issuerName,
				date: new Date(entry.issuedAt).toLocaleString("ja-JP", { timeZone: "Asia/Tokyo" }),
				count: entry.count,
				startNumber: entry.startNumber,
				endNumber: entry.endNumber
			}));

			totalIssued = logs.reduce((sum, log) => sum + log.count, 0);
		} else {
			console.error("ログの取得に失敗しました");
		}
	}
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

	.label {
		font-weight: bold;
		margin-bottom: 0.5rem;
	}

	input[type="number"] {
		padding: 0.5rem;
		font-size: 1rem;
		border: 1px solid #ccc;
		border-radius: 0.25rem;
		width: 100px;
		margin-right: 1rem;
	}

	button {
		padding: 0.5rem 1rem;
		font-size: 1rem;
		background-color: #007acc;
		color: white;
		border: none;
		border-radius: 0.4rem;
		cursor: pointer;
	}

	button:disabled {
		background-color: #a0c5e8;
		cursor: not-allowed;
	}

	button:hover:not(:disabled) {
		background-color: #005ea2;
	}

	.log-table {
		width: 100%;
		border-collapse: collapse;
		margin-top: 1rem;
	}

	.log-table th,
	.log-table td {
		border: 1px solid #ccc;
		padding: 0.5rem;
		text-align: center;
	}

	.log-table th {
		background-color: #f0f0f0;
	}

	/* 折り畳みセクション */
	.collapsible-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 0.5rem 1rem;
		background-color: #f0f8ff;
		border-left: 4px solid #007acc;
		cursor: pointer;
		user-select: none;
	}

	.collapsible-header:hover {
		background-color: #e0f0ff;
	}

	.collapsible-title {
		font-size: 1.2rem;
		font-weight: bold;
	}

	.collapsible-icon {
		font-size: 1.2rem;
		transition: transform 0.2s ease;
	}

	.collapsible-icon.open {
		transform: rotate(90deg);
	}

	.collapsible-content {
		padding: 1rem;
		border: 1px solid #ddd;
		border-top: none;
		background-color: #fff;
	}
</style>

<div class="container">
	<div class="title">抽選会: {lotteryName}</div>

	<!-- 抽選券カスタマイズセクション（折り畳み） -->
	<div class="section">
		<div class="collapsible-header" onclick={() => isCustomizeOpen = !isCustomizeOpen}>
			<span class="collapsible-title">抽選券カスタマイズ</span>
			<span class="collapsible-icon" class:open={isCustomizeOpen}>▶</span>
		</div>
		{#if isCustomizeOpen}
			<div class="collapsible-content">
				<TicketCustomizer {lotteryId} lotteryName={lotteryName} />
			</div>
		{/if}
	</div>

	<!-- 抽選券発行セクション -->
	<div class="section">
		<div class="section-title">抽選券発行</div>
		<div class="label">発行枚数を入力：</div>
		<input type="number" bind:value={issueCount} min="1" />
		<button onclick={generateTickets} disabled={isGenerating}>
			{#if isGenerating}
				発行中...
			{:else}
				抽選券を発行
			{/if}
		</button>
	</div>

	<!-- 発行ログセクション -->
	<div class="section">
		<div class="section-title">発行履歴</div>
		<div class="label">合計発行枚数: {totalIssued}</div>
		<div class="label">発行ログ:</div>
		<table class="log-table">
			<thead>
				<tr>
					<th>発行者</th>
					<th>発行日時</th>
					<th>枚数</th>
					<th>抽選番号範囲</th>
				</tr>
			</thead>
			<tbody>
				{#each logs as log}
					<tr>
						<td>{log.issuer}</td>
						<td>{log.date}</td>
						<td>{log.count}</td>
						<td>No.{log.startNumber} ～ No.{log.endNumber}</td>
					</tr>
				{/each}
			</tbody>
		</table>
	</div>
</div>
