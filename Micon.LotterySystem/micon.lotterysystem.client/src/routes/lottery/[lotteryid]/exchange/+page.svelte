<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/stores';
	import { browser } from '$app/environment';

	let qrReaderEl: HTMLDivElement;
	let html5QrCode: any;

	let scannedGuid: string | null = null;
	let ticketNumber: number | null = null;
	let ticketStatus: string | null = null;
	let errorMessage = '';
	let merchandise = "";
	let slotName = "";
	let isScanning = false;
	let showExchangeButton = false; // 「引き換えますか？」ボタンの表示を制御
	let isWinner = false; // 当選しているかどうかを制御
	let isExchanged = false; // 引き換え済みかどうかを制御

	const lotteryId = $page.params.lotteryid;

	/** スキャン開始 */
	async function startScanner() {
		if (!browser) return;

		errorMessage = '';
		scannedGuid = null;
		ticketNumber = null;
		ticketStatus = null;
		merchandise = null;
		slotName = null;
		showExchangeButton = false;
		isWinner = false; // スキャン開始時にリセット
		isExchanged = false; // スキャン開始時にリセット

		isScanning = true;

		const { Html5Qrcode } = await import('html5-qrcode');
		html5QrCode = new Html5Qrcode(qrReaderEl.id);

		await html5QrCode.start(
			{ facingMode: 'environment' },
			{ fps: 15, qrbox: { width: 450, height: 450 } },
			async (decodedText: string) => {
				if (!isScanning) return;
				isScanning = false;

				// GUID を抽出
				const m = decodedText.match(/[0-9a-fA-F\-]{36}/);
				if (m) {
					scannedGuid = m[0];
					await checkTicketStatus();
				} else {
					errorMessage = 'QRコードに有効なGUIDが含まれていません';
					scheduleRestart();
				}

				await html5QrCode.stop();
			},
			(_err: any) => {
				// 読み取り失敗は無視
			}
		);
	}

	/** チケット状態を取得して UI を制御 */
	async function checkTicketStatus() {
		try {
			const res = await fetch(`/api/ticket/${scannedGuid}`);
			if (!res.ok) throw new Error();
			const data = await res.json();
			ticketNumber = data.number;
			ticketStatus = data.status;
			slotName = data.slotName ?? "";
			merchandise = data.merchandise ?? "";

			isWinner = (ticketStatus === 'Winner'); // 当選しているかを判定
			isExchanged = (ticketStatus === 'Exchanged'); // 引き換え済みかを判定
			showExchangeButton = isWinner && !isExchanged; // 当選しており、かつ未引き換えの場合のみ交換ボタンを表示

			if (!isWinner || isExchanged) {
				// 当選していないか、すでに引き換え済みの場合は再スキャン
				scheduleRestart();
			}
		} catch {
			errorMessage = '状態取得に失敗しました';
			scheduleRestart();
		}
	}

	/** 有効化（引き換え） */
	async function activateTicket() {
		try {
			const res = await fetch(`/api/ticket/Exchange/${scannedGuid}`, { method: 'POST' });
			if (!res.ok) throw new Error();
			ticketStatus = 'Exchanged';
			isExchanged = true; // 引き換えが完了したので状態を更新
			showExchangeButton = false;
			errorMessage = '抽選券を交換しました';
		} catch {
			errorMessage = '抽選券の交換が拒否されました';
		} finally {
			scheduleRestart();
		}
	}

	/** 自動再スキャン予約 */
	function scheduleRestart() {
		// 1.5秒後に再スキャン
		setTimeout(() => {
			startScanner();
		}, 1500);
	}

	onMount(() => {
		startScanner();
	});
</script>

<style>
	#reader {
		width: 100%;
		max-width: 400px;
		margin: auto;
		padding: 1rem;
	}

	.card {
		max-width: 400px;
		margin: 2rem auto;
		padding: 1rem;
		border: 1px solid #ccc;
		border-radius: 8px;
		text-align: center;
	}

	.button, .rescan {
		margin: 0.5rem;
		padding: 0.5rem 1rem;
		font-size: 1rem;
		background-color: #007acc;
		color: white;
		border: none;
		border-radius: 0.4rem;
		cursor: pointer;
	}

		.button:hover, .rescan:hover {
			background-color: #005ea2;
		}

	.error {
		color: red;
		margin-top: 0.5rem;
	}

	.info-message {
		color: #333;
		margin-top: 1rem;
		font-weight: bold;
	}

	.cannot-exchange {
		color: #d9534f; /* 赤系の色で強調 */
		font-weight: bold;
		margin-top: 0.5rem;
	}
</style>

<h1>抽選券(当選券)の引き換え</h1>
<div id="reader" bind:this={qrReaderEl}></div>

<div class="card">
	<p>抽選券のQRコードをカメラに写してください</p>
	{#if ticketNumber !== null}
	<h2>抽選番号: No.{ticketNumber}</h2>
	{#if isWinner}
	<p>当選枠: {slotName}</p>
	<p>商品: {merchandise}</p>
	{#if isExchanged}
	<p class="cannot-exchange">この抽選券は**既に引き換え済み**です。交換できません。</p>
	{:else}
	<p>状態: 現在は引き換え前です</p>
	{/if}
	{:else}
	<p class="cannot-exchange">この抽選券は**当選していません**。引き換えできません。</p>
	{/if}
	{/if}

	{#if errorMessage}
	<p class="error">{errorMessage}</p>
	{/if}

	{#if showExchangeButton}
	<button class="button" on:click={activateTicket}>引き換えますか？</button>
	{/if}

	<button class="rescan" on:click={() => startScanner()}>再スキャン</button>
</div>