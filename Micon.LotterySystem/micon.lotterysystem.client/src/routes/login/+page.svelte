<script lang="ts">
    import { goto } from "$app/navigation";
    import { user } from "../../store/UserStore.ts";
    let username: string = '';
    let password: string = '';
    let errorMessage: string = '';
    let isLoading: boolean = false; // ローディング状態
    async function handleSubmit(): Promise<void> {
        if (!username) {
            errorMessage = 'ユーザー名を入力してください。';
            return;
        }

        if (!password) {
            errorMessage = 'パスワードを入力してください。';
            return;
        }

        isLoading = true;
        errorMessage = ''; // 前のエラーメッセージをクリア

        try {
            const response = await fetch('/api/user/LoginByUserName', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    UserName: username,
                    Password: password,
                }),
            });
            console.log(response);
            if (response.ok) {
                const data = await response.json();
                console.log(data);

                console.log(user);
                user.set(data);
                console.log('ログイン成功:', data);
                isLoading = false;
                window.location.href = "/";
            } else {
                const errorData = await response.json();
                console.error('ログイン失敗:', errorData);
                errorMessage = errorData?.message || 'ログインに失敗しました。';
                isLoading = false;
            }
        } catch (error) {
            console.error('ログインリクエストエラー:', error);
            errorMessage = 'ネットワークエラーが発生しました。';
            isLoading = false;
        }
    }
</script>
<style>
    .login-container {
        display: flex;
        flex-direction: column;
        align-items: center;
        padding: 20px;
        border: 1px solid #ccc;
        border-radius: 5px;
        width: 300px;
        margin: 50px auto;
        position: relative; /* ローディング表示のために必要 */
    }

    h2 {
        margin-bottom: 20px;
    }

    .form-group {
        margin-bottom: 15px;
        width: 100%;
    }

    label {
        display: block;
        margin-bottom: 5px;
    }

    input[type="text"],
    input[type="password"] {
        width: 100%;
        padding: 10px;
        border: 1px solid #ddd;
        border-radius: 3px;
        box-sizing: border-box;
    }

    button {
        padding: 10px 20px;
        background-color: #007bff;
        color: white;
        border: none;
        border-radius: 3px;
        cursor: pointer;
        transition: background-color 0.3s ease;
    }

        button:disabled {
            background-color: #ccc;
            cursor: not-allowed;
        }

    .error-message {
        color: red;
        margin-top: 10px;
    }

    .loading-overlay {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background-color: rgba(0, 0, 0, 0.5);
        display: flex;
        justify-content: center;
        align-items: center;
        border-radius: 5px;
    }

    .loader {
        border: 4px solid #f3f3f3; /* Light grey */
        border-top: 4px solid #3498db; /* Blue */
        border-radius: 50%;
        width: 40px;
        height: 40px;
        animation: spin 2s linear infinite;
    }

    @keyframes spin {
        0% {
            transform: rotate(0deg);
        }

        100% {
            transform: rotate(360deg);
        }
    }
</style>

<div class="login-container">
    <h2>ログイン</h2>
    {#if errorMessage}
    <p class="error-message">{errorMessage}</p>
    {/if}
    <div class="form-group">
        <label for="username">ユーザー名</label>
        <input type="text" id="username" bind:value={username}>
    </div>
    <div class="form-group">
        <label for="password">パスワード</label>
        <input type="password" id="password" bind:value={password}>
    </div>
    <button on:click={handleSubmit}>ログイン</button>
</div>