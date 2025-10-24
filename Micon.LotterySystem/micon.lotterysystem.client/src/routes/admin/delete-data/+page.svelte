<script lang="ts">
    import { goto } from '$app/navigation';

    let password = '';
    let showPassword = false;
    let error: string | null = null;
    let isDeleting = false;
    let deleteSuccess = false;

    async function handleDeleteAllData() {
        error = null;

        if (!password) {
            error = 'パスワードを入力してください。';
            return;
        }

        if (!confirm('⚠️ 本当にすべてのデータを削除しますか？\n\nこの操作は取り消せません。')) {
            return;
        }

        isDeleting = true;

        try {
            const response = await fetch('/api/admin/DeleteAllData', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ password }),
            });

            if (response.ok) {
                deleteSuccess = true;
                // 3秒後にログイン画面にリダイレクト
                setTimeout(() => {
                    goto('/login');
                }, 3000);
            } else {
                const data = await response.json();
                error = data.error || 'データ削除に失敗しました。';
            }
        } catch (err) {
            error = '通信エラーが発生しました。';
            console.error(err);
        } finally {
            isDeleting = false;
        }
    }

    function handleCancel() {
        goto('/');
    }
</script>

<style>
    .container {
        max-width: 600px;
        margin: 40px auto;
        padding: 20px;
    }

    .title {
        font-size: 1.5rem;
        font-weight: bold;
        margin-bottom: 20px;
        color: #333;
    }

    .warning-box {
        background-color: #ffe6e6;
        border: 2px solid #dc3545;
        border-radius: 8px;
        padding: 20px;
        margin-bottom: 30px;
    }

    .warning-icon {
        font-size: 2rem;
        margin-bottom: 10px;
    }

    .warning-text {
        color: #721c24;
        font-weight: bold;
        margin-bottom: 10px;
    }

    .warning-description {
        color: #721c24;
        font-size: 0.95rem;
        line-height: 1.6;
    }

    .warning-description ul {
        margin: 10px 0;
        padding-left: 20px;
    }

    .warning-description li {
        margin: 5px 0;
    }

    .form-group {
        margin-bottom: 20px;
    }

    label {
        display: block;
        font-weight: bold;
        margin-bottom: 8px;
        color: #333;
    }

    .password-input {
        position: relative;
    }

    input[type="password"],
    input[type="text"] {
        width: 100%;
        padding: 10px;
        border: 1px solid #ddd;
        border-radius: 4px;
        font-size: 1rem;
        box-sizing: border-box;
    }

    input[type="password"]:focus,
    input[type="text"]:focus {
        outline: none;
        border-color: #007bff;
        box-shadow: 0 0 5px rgba(0, 123, 255, 0.5);
    }

    .toggle-password {
        position: absolute;
        right: 10px;
        top: 50%;
        transform: translateY(-50%);
        background: none;
        border: none;
        color: #007bff;
        cursor: pointer;
        font-weight: bold;
        font-size: 0.9rem;
    }

    .error {
        color: #dc3545;
        background-color: #f8d7da;
        border: 1px solid #f5c6cb;
        padding: 12px;
        border-radius: 4px;
        margin-bottom: 20px;
        font-weight: bold;
    }

    .success {
        color: #155724;
        background-color: #d4edda;
        border: 1px solid #c3e6cb;
        padding: 12px;
        border-radius: 4px;
        margin-bottom: 20px;
        font-weight: bold;
        text-align: center;
    }

    .button-group {
        display: flex;
        gap: 10px;
        justify-content: flex-end;
        margin-top: 30px;
    }

    button {
        padding: 10px 20px;
        border: none;
        border-radius: 4px;
        font-size: 1rem;
        font-weight: bold;
        cursor: pointer;
        transition: all 0.3s ease;
    }

    .delete-button {
        background-color: #dc3545;
        color: white;
        flex: 1;
    }

    .delete-button:hover:not(:disabled) {
        background-color: #c82333;
    }

    .delete-button:disabled {
        background-color: #ccc;
        cursor: not-allowed;
    }

    .cancel-button {
        background-color: #6c757d;
        color: white;
    }

    .cancel-button:hover:not(:disabled) {
        background-color: #5a6268;
    }

    .cancel-button:disabled {
        background-color: #ccc;
        cursor: not-allowed;
    }

    .loading {
        display: inline-block;
        width: 20px;
        height: 20px;
        border: 3px solid #f3f3f3;
        border-top: 3px solid #007bff;
        border-radius: 50%;
        animation: spin 1s linear infinite;
        margin-right: 10px;
        vertical-align: middle;
    }

    @keyframes spin {
        0% {
            transform: rotate(0deg);
        }
        100% {
            transform: rotate(360deg);
        }
    }

    .loading-text {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 10px;
    }
</style>

<div class="container">
    <div class="title">⚠️ すべてのデータを削除</div>

    {#if deleteSuccess}
        <div class="success">
            ✓ すべてのデータが削除されました。<br />
            3秒後にログイン画面に移動します...
        </div>
    {:else}
        <div class="warning-box">
            <div class="warning-icon">⚠️</div>
            <div class="warning-text">削除前に必ずお読みください</div>
            <div class="warning-description">
                <p>この操作により、以下のすべてのデータが<strong>完全に削除</strong>されます：</p>
                <ul>
                    <li>全ユーザーアカウント</li>
                    <li>全ロール及び権限設定</li>
                    <li>全抽選会情報</li>
                    <li>全抽選枠情報</li>
                    <li>全抽選券</li>
                    <li>その他すべてのシステムデータ</li>
                </ul>
                <p><strong>注意：この操作は取り消すことができません。</strong></p>
                <p>削除後、システムは初期状態にリセットされ、新しいユーザーを登録してください。</p>
            </div>
        </div>

        {#if error}
            <div class="error">{error}</div>
        {/if}

        <form on:submit|preventDefault={handleDeleteAllData}>
            <div class="form-group">
                <label for="password">確認用パスワード</label>
                <div class="password-input">
                    <input
                        type={showPassword ? 'text' : 'password'}
                        id="password"
                        bind:value={password}
                        placeholder="パスワードを入力してください"
                        disabled={isDeleting}
                    />
                    <button
                        type="button"
                        class="toggle-password"
                        on:click={() => (showPassword = !showPassword)}
                        disabled={isDeleting}
                    >
                        {showPassword ? '非表示' : '表示'}
                    </button>
                </div>
            </div>

            <div class="button-group">
                <button
                    type="button"
                    class="cancel-button"
                    on:click={handleCancel}
                    disabled={isDeleting}
                >
                    キャンセル
                </button>
                <button
                    type="submit"
                    class="delete-button"
                    disabled={isDeleting || !password}
                >
                    {#if isDeleting}
                        <div class="loading-text">
                            <span class="loading"></span>
                            削除中...
                        </div>
                    {:else}
                        すべてのデータを削除する
                    {/if}
                </button>
            </div>
        </form>
    {/if}
</div>
