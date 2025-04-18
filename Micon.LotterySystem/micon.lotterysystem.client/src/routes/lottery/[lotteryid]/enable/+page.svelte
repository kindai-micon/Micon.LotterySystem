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

	let isScanning = false;
	let showActivateButton = false;

	const lotteryId = $page.params.lotteryid;

	/** スキャン開始 */
	async function startScanner() {
	if (!browser) return;

	errorMessage = '';
	scannedGuid = null;
	ticketNumber = null;
	ticketStatus = null;
	showActivateButton = false;

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
	showActivateButton = (ticketStatus === 'Invalid');

	if (!showActivateButton) {
	// すでに有効なら再スキャン
	scheduleRestart();
	}
	} catch {
	errorMessage = '状態取得に失敗しました';
	scheduleRestart();
	}
	}

	/** 有効化 */
	async function activateTicket() {
	try {
	const res = await fetch(`/api/ticket/activate/${scannedGuid}`, { method: 'POST' });
	if (!res.ok) throw new Error();
	ticketStatus = 'Valid';
	showActivateButton = false;
	errorMessage = '抽選券を有効化しました';
	} catch {
	errorMessage = '有効化に失敗しました';
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
</style>

<h1>抽選券の有効化</h1>
<div id="reader" bind:this={qrReaderEl}></div>

<div class="card">
	<p>抽選券のQRコードをカメラに写してください</p>
	{#if ticketNumber !== null}
	<h2>抽選番号: No.{ticketNumber}</h2>
	<p>状態: {ticketStatus === 'Valid' ? 'すでに有効です' : '現在は無効です'}</p>
	{/if}

	{#if errorMessage}
	<p class="error">{errorMessage}</p>
	{/if}

	{#if showActivateButton}
	<button class="button" on:click={activateTicket}>抽選券を有効化</button>
	{/if}

	<!-- 手動で再スキャンしたい場合のボタン -->
	<button class="rescan" on:click={() => startScanner()}>再スキャン</button>
</div>