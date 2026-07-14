<script lang="ts">
	import { onMount } from 'svelte';

	// Props (Svelte 5 runes mode)
	interface Props {
		lotteryId: string;
		lotteryName?: string;
	}
	let { lotteryId, lotteryName = "" }: Props = $props();

	// 抽選券情報カスタマイズ用（$stateでリアクティブ化）
	let ticketLabel = $state("抽選券");
	let description = $state("");
	let warningText = $state("当日のみ有効\n本券は汚したり破らないよう大切に保管してください");
	let footerText = $state("");

	// デフォルト値（リセット用）
	const defaultTicketLabel = "抽選券";
	const defaultWarningText = "当日のみ有効\n本券は汚したり破らないよう大切に保管してください";
	const defaultFooterText = "";

	let isSaving = $state(false);

	// プレビューキャンバス
	let previewCanvas: HTMLCanvasElement | undefined = $state();

	// PDF抽選券サイズ（QuestPDFはポイント単位）
	// A4: 595pt x 842pt, Margin: 20pt
	// 内側: 555pt x 802pt, 2列x4行
	// 1抽選券: 約277pt x 200pt
	// Border(1) + Padding(8) = 実質コンテンツ: 261pt x 184pt
	const PDF_TICKET_WIDTH_PT = 277;
	const PDF_TICKET_HEIGHT_PT = 200;
	const PDF_PADDING_PT = 8;

	// プレビュースケール（1pt = 1.2px）
	const SCALE = 1.2;
	const PREVIEW_WIDTH = Math.round(PDF_TICKET_WIDTH_PT * SCALE);
	const PREVIEW_HEIGHT = Math.round(PDF_TICKET_HEIGHT_PT * SCALE);

	// PDFフォントサイズ(pt)をプレビュー用ピクセルに変換
	function ptToPx(pt: number): number {
		return Math.round(pt * SCALE);
	}

	// テキストを指定幅で折り返して描画（複数行対応）
	function drawMultilineText(
		ctx: CanvasRenderingContext2D,
		text: string,
		x: number,
		y: number,
		maxWidth: number,
		fontSize: number,
		maxLines: number = 10
	): number {
		ctx.font = `${fontSize}px "Noto Sans JP", sans-serif`;

		const lines = text.split('\n');
		let currentY = y;
		let lineCount = 0;

		for (const line of lines) {
			if (lineCount >= maxLines) break;
			if (currentY > PREVIEW_HEIGHT - ptToPx(PDF_PADDING_PT) - fontSize) break;

			// 空行はスキップせず、行送りだけ行う
			if (line.trim() === '') {
				currentY += fontSize;
				lineCount++;
				continue;
			}

			// 長い行は折り返し
			const avgCharWidth = fontSize * 0.9;
			const maxChars = Math.floor(maxWidth / avgCharWidth);

			let remaining = line;
			while (remaining.length > 0 && lineCount < maxLines) {
				if (currentY > PREVIEW_HEIGHT - ptToPx(PDF_PADDING_PT) - fontSize) break;

				const displayLine = remaining.length > maxChars
					? remaining.substring(0, maxChars)
					: remaining;
				ctx.fillText(displayLine, x, currentY);
				currentY += fontSize + 2;
				lineCount++;
				remaining = remaining.substring(maxChars);
			}
		}

		return currentY;
	}

	function drawPreview() {
		if (!previewCanvas) return;

		const ctx = previewCanvas.getContext('2d');
		if (!ctx) return;

		const width = previewCanvas.width;
		const height = previewCanvas.height;

		// 背景
		ctx.fillStyle = '#ffffff';
		ctx.fillRect(0, 0, width, height);

		// 枠線（Border(1)相当）
		ctx.strokeStyle = '#333333';
		ctx.lineWidth = 1;
		ctx.strokeRect(0.5, 0.5, width - 1, height - 1);

		// 内側パディング
		const padding = ptToPx(PDF_PADDING_PT);
		const innerX = padding;
		const innerWidth = width - padding * 2;

		let currentY = padding;

		// タイトル (PDF: FontSize(16), Bold, Center)
		ctx.fillStyle = '#000000';
		const titleSize = ptToPx(16);
		ctx.font = `bold ${titleSize}px "Noto Sans JP", sans-serif`;
		ctx.textAlign = 'center';
		ctx.textBaseline = 'top';
		ctx.fillText(ticketLabel, width / 2, currentY);
		currentY += titleSize + ptToPx(5);

		// QRコードプレースホルダー (PDF: ConstantItem(100), Height(80))
		const qrWidth = ptToPx(100);
		const qrHeight = ptToPx(80);
		const qrX = width - padding - qrWidth;
		const qrY = currentY;

		ctx.strokeStyle = '#cccccc';
		ctx.lineWidth = 1;
		ctx.strokeRect(qrX, qrY, qrWidth, qrHeight);
		ctx.fillStyle = '#999999';
		ctx.font = `${ptToPx(10)}px sans-serif`;
		ctx.textAlign = 'center';
		ctx.textBaseline = 'middle';
		ctx.fillText('QR', qrX + qrWidth / 2, qrY + qrHeight / 2);

		// 抽選番号 (PDF: FontSize(16), Bold, AlignBottom)
		ctx.fillStyle = '#000000';
		const numberSize = ptToPx(16);
		ctx.font = `bold ${numberSize}px "Noto Sans JP", sans-serif`;
		ctx.textAlign = 'left';
		ctx.textBaseline = 'bottom';
		ctx.fillText('抽選番号：No.123', innerX, qrY + qrHeight);

		currentY = qrY + qrHeight + ptToPx(5);

		// 説明 (PDF: FontSize(9))
		ctx.textBaseline = 'top';
		ctx.textAlign = 'left';
		const descSize = ptToPx(9);
		currentY = drawMultilineText(ctx, description, innerX, currentY, innerWidth, descSize, 3);

		// 説明と注意書きの間を1行開ける
		currentY += descSize;

		// 注意書き (PDF: FontSize(7) - 説明より小さく)
		const warningSize = ptToPx(7);
		ctx.fillStyle = '#333333';
		currentY = drawMultilineText(ctx, warningText, innerX, currentY, innerWidth, warningSize, 5);

		// フッター (PDF: FontSize(6), Italic, Grey, Right align, PaddingTop(10))
		const footerSize = ptToPx(6);
		ctx.fillStyle = '#888888';
		ctx.font = `italic ${footerSize}px sans-serif`;
		ctx.textAlign = 'right';
		ctx.textBaseline = 'bottom';
		const footer = footerText || 'Powered by Micon club';
		ctx.fillText(footer, width - padding, height - padding);
	}

	// リアクティブにプレビューを更新
	$effect(() => {
		if (previewCanvas) {
			ticketLabel;
			description;
			warningText;
			footerText;
			drawPreview();
		}
	});

	onMount(async () => {
		await loadTicketInfo();
	});

	async function loadTicketInfo() {
		try {
			const res = await fetch(`/api/TicketInfo/${lotteryId}`);
			if (res.ok) {
				const data = await res.json();
				ticketLabel = data.ticketLabel || defaultTicketLabel;
				description = data.description || "";
				warningText = data.warningText || defaultWarningText;
				footerText = data.footerText || defaultFooterText;
			}
		} catch (err) {
			console.error("抽選券情報の取得に失敗しました", err);
		}
	}

	async function saveTicketInfo() {
		// 注意書きは必須
		if (!warningText.trim()) {
			alert("注意書きは必須です");
			return;
		}

		isSaving = true;
		try {
			const response = await fetch(`/api/TicketInfo/${lotteryId}`, {
				method: 'PUT',
				headers: {
					'Content-Type': 'application/json'
				},
				body: JSON.stringify({
					ticketLabel,
					description,
					warningText,
					footerText
				})
			});

			if (!response.ok) {
				alert("保存に失敗しました");
				return;
			}

			alert("保存しました");
		} catch (err) {
			console.error("エラー:", err);
			alert("予期せぬエラーが発生しました");
		} finally {
			isSaving = false;
		}
	}

	function resetTicketInfo() {
		ticketLabel = defaultTicketLabel;
		description = lotteryName;
		warningText = defaultWarningText;
		footerText = defaultFooterText;
	}
</script>

<style>
	.customize-grid {
		display: grid;
		grid-template-columns: 1fr 350px;
		gap: 2rem;
	}

	@media (max-width: 900px) {
		.customize-grid {
			grid-template-columns: 1fr;
		}
	}

	.form-group {
		margin-bottom: 1rem;
	}

	.form-group label {
		display: block;
		margin-bottom: 0.25rem;
		font-weight: 500;
	}

	.hint {
		font-size: 0.8rem;
		color: #666;
		margin-top: 0.25rem;
	}

	.required {
		color: #dc3545;
		font-size: 0.8rem;
		margin-left: 0.25rem;
	}

	input[type="text"],
	textarea {
		padding: 0.5rem;
		font-size: 1rem;
		border: 1px solid #ccc;
		border-radius: 0.25rem;
	}

	input[type="text"] {
		width: 100%;
		max-width: 400px;
	}

	textarea {
		width: 100%;
		max-width: 400px;
		min-height: 80px;
		resize: vertical;
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

	button.secondary {
		background-color: #6c757d;
	}

	button.secondary:hover:not(:disabled) {
		background-color: #545b62;
	}

	.preview-container {
		border: 1px solid #ddd;
		border-radius: 0.5rem;
		padding: 1rem;
		background-color: #fafafa;
	}

	.preview-title {
		font-weight: bold;
		margin-bottom: 0.5rem;
		text-align: center;
	}

	.preview-canvas {
		border: 1px solid #ccc;
		background: white;
		display: block;
		margin: 0 auto;
		max-width: 100%;
		height: auto;
	}

	.button-group {
		display: flex;
		gap: 0.5rem;
		margin-top: 1rem;
	}
</style>

<div class="customize-grid">
	<div class="form-section">
		<div class="form-group">
			<label>抽選券名</label>
			<input type="text" bind:value={ticketLabel} placeholder="抽選券" />
		</div>

		<div class="form-group">
			<label>説明文</label>
			<textarea bind:value={description} placeholder="イベント名など（改行可能）" rows="2"></textarea>
			<div class="hint">改行するとPDFでも改行されます</div>
		</div>

		<div class="form-group">
			<label>注意書き<span class="required">※必須</span></label>
			<textarea bind:value={warningText} placeholder="注意事項を入力（改行可能）" rows="3"></textarea>
			<div class="hint">改行するとPDFでも改行されます</div>
		</div>

		<div class="form-group">
			<label>フッターテキスト</label>
			<input type="text" bind:value={footerText} placeholder="空欄の場合はデフォルト表示" />
		</div>

		<div class="button-group">
			<button onclick={saveTicketInfo} disabled={isSaving}>
				{#if isSaving}保存中...{:else}保存{/if}
			</button>
			<button class="secondary" onclick={resetTicketInfo}>リセット</button>
		</div>
	</div>

	<div class="preview-container">
		<div class="preview-title">プレビュー（実物比率）</div>
		<canvas bind:this={previewCanvas} class="preview-canvas" width={PREVIEW_WIDTH} height={PREVIEW_HEIGHT}></canvas>
	</div>
</div>
