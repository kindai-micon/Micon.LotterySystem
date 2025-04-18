<script lang="ts">
  import { onMount } from 'svelte';
  import { page } from '$app/stores';
  import { browser } from '$app/environment';

  let qrReaderEl: HTMLDivElement;
  let scannedGuid: string | null = null;
  let isScanning = true;
  let errorMessage = "";

  const lotteryId = $page.params.lotteryid;

  onMount(async () => {
    if (!browser) return;

    const { Html5Qrcode } = await import('html5-qrcode');

    const html5QrCode = new Html5Qrcode(qrReaderEl.id);

    html5QrCode.start(
      { facingMode: 'environment' },
      {
        fps: 10,
        qrbox: { width: 250, height: 250 }
      },
      (decodedText) => {
        if (!isScanning) return;
        isScanning = false;

        try {
          const guidMatch = decodedText.match(/[0-9a-fA-F\-]{36}/);
          if (guidMatch) {
            scannedGuid = guidMatch[0];
          } else {
            errorMessage = "QRコードに有効な情報が含まれていません";
          }
        } catch (e) {
          errorMessage = "QRコードの処理中にエラーが発生しました";
        }

        html5QrCode.stop();
      },
      (errorMessage) => {
        // 読み取り失敗時のログ（必要なら console に表示）
      }
    );
  });
</script>

<style>
  #reader {
    width: 100%;
    max-width: 400px;
    margin: auto;
    padding: 1rem;
  }

  .status {
    margin-top: 1rem;
    text-align: center;
  }

  .guid {
    font-weight: bold;
    color: green;
    font-size: 1.2rem;
  }

  .error {
    color: red;
    margin-top: 1rem;
  }
</style>

<h1>抽選券の有効化</h1>

<div id="reader" bind:this={qrReaderEl}></div>

<div class="status">
  {#if scannedGuid}
    <p>読み取った抽選券ID: <span class="guid">{scannedGuid}</span></p>
    <!-- このあと「状態取得＆有効化ボタン表示」などを追加 -->
  {:else if errorMessage}
    <p class="error">{errorMessage}</p>
  {:else}
    <p>QRコードを読み取っています...</p>
  {/if}
</div>
