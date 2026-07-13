<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { page } from '$app/stores';
    import { HubConnectionBuilder, HubConnection, HubConnectionState } from '@microsoft/signalr';

    type TicketStatus = {
        slotName: string | null;
        merchandise: string | null;
        number: number;
        status: string;
        lotteryGroupId: string | null;
    };

    // ページのパラメータが変わったら反応するために$pageを監視
    $: currentTicketId = $page.params.ticketid;
    let prevTicketId: string | null = null;

    let ticketData: TicketStatus | null = null;
    let lotteryGroupId: string | null = null;

    let loaded = false;
    let connection: HubConnection | null = null;
    let connectionReady = false;

	let notifications = localStorage.getItem('notifications') ? JSON.parse(localStorage.getItem("notifications")) : [];
	$: notification = currentTicketId ? notifications.includes(currentTicketId) : false;
	
	if ('serviceWorker' in navigator) {
		navigator.serviceWorker.register('/service-worker.js');
		navigator.serviceWorker.ready.then(reg => {
			return reg.pushManager.getSubscription();
		}).then(sub => {
			if(!sub && notifications.includes(currentTicketId)) {
				notifications = notifications.filter(v => v !== currentTicketId);
				localStorage.setItem('notifications',JSON.stringify(notifications));
			}
		});
	}

    // URLパラメータ（ticketId）が変更されたときに反応する
    $: if (currentTicketId && currentTicketId !== prevTicketId && connectionReady) {
        handleTicketChange(currentTicketId);
    }

    async function handleTicketChange(newTicketId: string) {
        console.log(`URL changed: new ticketId = ${newTicketId}, previous = ${prevTicketId}`);
        prevTicketId = newTicketId;

        try {
            if (!connection) {
                return;
            }

            if (connection.state === HubConnectionState.Connected && lotteryGroupId) {
                await connection.invoke("RemoveLotteryGroup", lotteryGroupId);
                await Load();
                if (lotteryGroupId) {
                    await connection.invoke("SetLotteryGroup", lotteryGroupId);
                    console.log("SetLotteryGroup invoked after URL change");
                }
            } else {
                console.log("Connection not ready, waiting...");
                connection.onreconnected = async () => {
                    if (lotteryGroupId) {
                        await connection.invoke("RemoveLotteryGroup", lotteryGroupId);
                        await connection.invoke("SetLotteryGroup", lotteryGroupId);
                        await Load();
                    }
                };
            }
        } catch (err) {
            console.error("Error during ticket change:", err);
        }
    }

    onMount(async () => {
        try {
            if (connection) {
                await connection.stop();
                console.log("Stopped existing connection");
            }

            connection = new HubConnectionBuilder()
                .withUrl("/api/LotteryHub")
                .withAutomaticReconnect()
                .build();

            connection.on("UpdateStatus", async (id: string) => {
                await Load();
                loaded = true;
            });

            connection.on("SetTarget", async (id: string) => {
                await Load();
                loaded = true;
            });

            connection.on("SubmitLottery", async (id: string) => {
                await Load();
                loaded = true;
            });

            connection.on("ViewStop", async (id: string) => {
                await Load();
                loaded = true;
            });

            connection.on("ExchangeStop", async (id: string) => {
                await Load();
                loaded = true;
            });

            connection.onreconnected = async (connectionId: string) => {
                console.log("Reconnected with ID:", connectionId);
                if (lotteryGroupId) {
                    await connection.invoke("SetLotteryGroup", lotteryGroupId);
                    await Load();
                }
            };

            await connection.start();
            console.log("SignalR connected");
            connectionReady = true;

            prevTicketId = currentTicketId;
            await Load();
            loaded = true;
        } catch (err) {
            console.error("SignalR connection setup error:", err);
            loaded = true;
        }
    });

    onDestroy(() => {
        const hubConnection = connection;
        connection = null;
        connectionReady = false;

        if (hubConnection) {
            hubConnection.stop()
                .then(() => console.log("SignalR connection stopped"))
                .catch(err => console.error("Error stopping SignalR connection:", err));
        }
    });

    async function Load() {
        try {
            const res = await fetch(`/api/ticket/${currentTicketId}`);
            const data: TicketStatus = await res.json();

            ticketData = data;

            // lotteryGroupId を取得して、まだグループに参加していなければ参加
            if (data.lotteryGroupId && data.lotteryGroupId !== lotteryGroupId) {
                lotteryGroupId = data.lotteryGroupId;

                // 既に接続している場合は新しいグループに参加
                if (connection && connection.state === "Connected") {
                    try {
                        await connection.invoke("SetLotteryGroup", lotteryGroupId);
                        console.log("Joined lottery group:", lotteryGroupId);
                    } catch (err) {
                        console.error("Error joining lottery group:", err);
                    }
                }
            }

            console.log("Loaded ticket data:", ticketData);
        } catch (error) {
            console.error("Error loading ticket data:", error);
        }
    }

	async function subscribeNotification() {
		const reg = await navigator.serviceWorker.ready;
		let sub = await reg.pushManager.getSubscription();
		
		if(!notification || !sub) {
			if ('serviceWorker' in navigator) {
				if(Notification.permission === 'default') {
					await Notification.requestPermission();
				}

				if(Notification.permission === 'granted') {
					let publicKey = await getVapidPublicKey();
					if(!sub) {
						sub = await reg.pushManager.subscribe({
							userVisibleOnly: true,
							applicationServerKey: urlBase64ToUint8Array(publicKey)
						});
					}

					try {
						await fetch(`/api/push-subscription/${currentTicketId}`,{
							method: 'POST',
							headers: {
								'Content-Type': 'application/json'
							},
							body: JSON.stringify(sub)
						});
					} catch (error) {
						console.error('Error loading data:', error);
					}

					notification = true;
					notifications.push(currentTicketId);
					localStorage.setItem("notifications",JSON.stringify(notifications));
				}
			}
		}
	}

	async function getVapidPublicKey() {
		const res = await fetch("/api/push-subscription/vapid-public-key");

		if (!res.ok) {
			throw new Error("Failed to get VAPID key");
		}

		const data = await res.json();
		return data.publicKey;
	}

	function urlBase64ToUint8Array(base64String: string) {
		const padding = '='.repeat((4 - base64String.length % 4) % 4);
		const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
		const rawData = atob(base64);
		return Uint8Array.from([...rawData].map(c => c.charCodeAt(0)));
	}
</script>


<style>
    :global(body) {
        margin: 0;
        padding: 0;
        background-color: #f5f5f5;
    }

    .container {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 1.5rem 1rem;
        min-height: 100vh;
        background: linear-gradient(135deg, #f5f5f5 0%, #efefef 100%);
    }

    .header {
        text-align: center;
        margin-bottom: 2rem;
        width: 100%;
    }

    .header h1 {
        margin: 0;
        font-size: 1.8rem;
        color: #333;
        margin-bottom: 0.5rem;
    }

    .header p {
        margin: 0;
        font-size: 0.9rem;
        color: #666;
    }

	p {
	    text-align: center;
	    margin: 0.5rem 0;
	}

	.ticket-info {
	    background-color: #ffffff;
	    padding: 1.5rem;
	    border-radius: 12px;
	    margin-bottom: 1rem;
	    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
	    width: 100%;
	    max-width: 450px;
	}

	.ticket-number-box {
	    background: linear-gradient(135deg, #3a7d7c 0%, #2d6360 100%);
	    color: white;
	    padding: 2rem;
	    border-radius: 12px;
	    margin-bottom: 1rem;
	    text-align: center;
	    box-shadow: 0 4px 12px rgba(58, 125, 124, 0.3);
	}

	.ticket-number-label {
	    font-size: 0.85rem;
	    opacity: 0.9;
	    margin-bottom: 0.5rem;
	    font-weight: 500;
	}

	.ticket-number {
	    font-size: 2.5rem;
	    font-weight: bold;
	    letter-spacing: 2px;
	    font-family: 'Courier New', monospace;
	}

	.heading {
	    font-weight: 600;
	    font-size: 0.95rem;
	    margin-bottom: 0.8rem;
	    text-transform: uppercase;
	    letter-spacing: 0.5px;
	    color: #666;
	}

	.info-row {
	    padding: 1rem 0;
	    border-bottom: 1px solid #f0f0f0;
	}

	.info-row:last-child {
	    border-bottom: none;
	}

	.info-label {
	    font-size: 0.9rem;
	    color: #999;
	    margin-bottom: 0.3rem;
	    display: block;
	}

	.info-value {
	    font-size: 1.2rem;
	    color: #333;
	    font-weight: 600;
	}

	.status-badge {
	    display: inline-block;
	    padding: 0.6rem 1.2rem;
	    border-radius: 20px;
	    font-weight: 600;
	    font-size: 1rem;
	    margin-top: 0.5rem;
	}

	.status-invalid {
	    background-color: #f0f0f0;
	    color: #999;
	}

	.status-valid {
	    background-color: #e8f5e9;
	    color: #2e7d32;
	}

	.status-winner {
	    background-color: #fff3e0;
	    color: #e65100;
	}

	.status-exchanged {
	    background-color: #e8f5e9;
	    color: #1b5e20;
	}

	.alert-box {
	    margin-top: 1.5rem;
	    padding: 1.5rem;
	    border-radius: 12px;
	    text-align: center;
	    width: 100%;
	    max-width: 450px;
	    font-weight: 600;
	}

	.alert-winner {
	    background-color: #fff8e1;
	    border: 2px solid #ffc107;
	    color: #f57f17;
	    font-size: 1.1rem;
	}

	.alert-exchanged {
	    background-color: #e8f5e9;
	    border: 2px solid #4caf50;
	    color: #1b5e20;
	}

	.loading {
	    text-align: center;
	    padding: 3rem 1rem;
	    font-size: 1.1rem;
	    color: #666;
	}

	.loading-spinner {
	    width: 40px;
	    height: 40px;
	    border: 4px solid #f0f0f0;
	    border-top: 4px solid #3a7d7c;
	    border-radius: 50%;
	    animation: spin 1s linear infinite;
	    margin: 0 auto 1rem;
	}

	@keyframes spin {
	    0% { transform: rotate(0deg); }
	    100% { transform: rotate(360deg); }
	}

	.error-message {
	    color: #d32f2f;
	    text-align: center;
	    padding: 2rem;
	    font-size: 1rem;
	}

	.notification-btn {
		border: none;
		margin-left: auto;
		padding: 0.7rem 1.2rem;
		border-radius: 50px;
		font-weight: 600;
		color: white;
		cursor: pointer;
		box-shadow: 0 2px 6px rgba(0, 0, 0, 0.2);
		transition: background-color 0.2s ease, transform 0.1s ease;
	}

	.notification-registration {
		background-color: #4caf50;
	}

	.notification-no-registration {
		background-color: #9e9e9e;
	}

	.notification-btn:active {
		transform: scale(0.95);
	}

	/* モバイル最適化 */
	@media (max-width: 480px) {
	    .container {
	        padding: 1rem;
	    }

	    .header h1 {
	        font-size: 1.5rem;
	    }

	    .ticket-number {
	        font-size: 2rem;
	    }

	    .ticket-number-box {
	        padding: 1.5rem;
	    }

	    .info-value {
	        font-size: 1.1rem;
	    }

	    .status-badge {
	        padding: 0.5rem 1rem;
	        font-size: 0.95rem;
	    }
	}
</style>

{#if loaded}
	{#if ticketData}
	<div class="container">
		<button 
			class="notification-btn {notification ? 'notification-registration' : 'notification-no-registration'}"
			on:click={subscribeNotification}
		>
			当選通知{notification ? '登録済み✔' : '登録'}
		</button>
		<div class="header">
			<h1>チケット確認</h1>
			<p>QRコード読み込み完了</p>
		</div>

		<!-- チケット番号 -->
		<div class="ticket-number-box">
			<div class="ticket-number-label">抽選券番号</div>
			<div class="ticket-number">{ticketData.number}</div>
		</div>

		<!-- チケット詳細情報 -->
		<div class="ticket-info">
			<div class="heading">ステータス</div>
			<div class="status-badge status-{ticketData.status.toLowerCase()}">
				{#if ticketData.status === 'Invalid'}
					未有効化
				{:else if ticketData.status === 'Valid'}
					有効
				{:else if ticketData.status === 'Winner'}
					当選 🎉
				{:else if ticketData.status === 'Exchanged'}
					交換済み ✓
				{:else}
					{ticketData.status}
				{/if}
			</div>

			{#if ticketData.slotName}
			<div class="info-row">
				<span class="info-label">当選景品</span>
				<div class="info-value">{ticketData.slotName}</div>
				{#if ticketData.merchandise}
				<div style="font-size: 0.85rem; color: #999; margin-top: 0.3rem;">
					{ticketData.merchandise}
				</div>
				{/if}
			</div>
			{/if}
		</div>

		<!-- 当選アラート -->
		{#if ticketData.status === 'Winner'}
		<div class="alert-box alert-winner">
			<div style="font-size: 1.3rem; margin-bottom: 0.5rem;">🎉</div>
			<div>おめでとうございます！</div>
			<div style="font-size: 0.9rem; margin-top: 0.5rem; font-weight: 400;">
				このチケットは当選しました
			</div>
		</div>
		{/if}

		<!-- 交換済みアラート -->
		{#if ticketData.status === 'Exchanged'}
		<div class="alert-box alert-exchanged">
			<div style="font-size: 1.3rem; margin-bottom: 0.5rem;">✓</div>
			<div>交換済みです</div>
			<div style="font-size: 0.9rem; margin-top: 0.5rem; font-weight: 400;">
				このチケットはすでに景品と交換されました
			</div>
		</div>
		{/if}

		<!-- 有効なチケット説明 -->
		{#if ticketData.status === 'Valid' && !ticketData.slotName}
		<div class="alert-box" style="background-color: #e3f2fd; border: 2px solid #2196f3; color: #1565c0;">
			<div style="font-size: 1.3rem; margin-bottom: 0.5rem;">📋</div>
			<div>現在参加中</div>
			<div style="font-size: 0.9rem; margin-top: 0.5rem; font-weight: 400;">
				このチケットは抽選に参加中です
			</div>
		</div>
		{/if}

		<!-- 無効なチケット説明 -->
		{#if ticketData.status === 'Invalid'}
		<div class="alert-box" style="background-color: #f5f5f5; border: 2px solid #999; color: #666;">
			<div style="font-size: 1.3rem; margin-bottom: 0.5rem;">⏳</div>
			<div>未有効化</div>
			<div style="font-size: 0.9rem; margin-top: 0.5rem; font-weight: 400;">
				このチケットはまだ有効化されていません
			</div>
		</div>
		{/if}
	</div>
	{:else}
	<div class="container">
		<div class="error-message">
			<div style="font-size: 1.5rem; margin-bottom: 1rem;">❌</div>
			<p>チケット情報が見つかりません</p>
			<p style="font-size: 0.9rem; margin-top: 1rem; color: #999;">
				QRコードをもう一度読み込んでください
			</p>
		</div>
	</div>
	{/if}
{:else}
<div class="container">
	<div class="loading">
		<div class="loading-spinner"></div>
		<p>チケット情報を読み込み中...</p>
	</div>
</div>
{/if}
