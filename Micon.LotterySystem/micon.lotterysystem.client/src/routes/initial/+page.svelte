<script lang="ts">
    let passcode = '';
    let username = '';
    let password = '';
    let confirmPassword = '';
    let email = '';
    let error = '';

    let success = '';
    import { onMount } from 'svelte';
    import { goto } from "$app/navigation";

    onMount(async () => {
        const response = await fetch('/api/user/HasUser', {
            method: 'Get'
        });


        if (response.ok) {
            const data: bool = await response.json();
            console.log(data);
            if (data) {
                await goto("/login");
            }
        }
    })
    async function handleSubmit(): Promise<void> {

        error = '';
        success = '';

        if (password !== confirmPassword) {
            error = 'パスワードが一致しません。';
            return;
        }

        if (!passcode || !username || !password || !confirmPassword) {
            error = 'すべての項目を入力してください。';
            return;
        }

        // ここでAPIに送信する処理を実装できます
        // fetch('/api/create-initial-user', { method: 'POST', body: JSON.stringify(...) })
        const response = await fetch('/api/user/InitialRegister', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                Passcode: passcode,
                UserName: username,
                Password: password,
                ConfirmPassword: confirmPassword,
                Email: email
            }),
        });
        if (response.ok) {

            await goto("/Login");


        } else if (response.status === 400) {
            // IdentityError[] を読み取る
            const errors: { code: string; description: string }[] = await response.json();
            error = errors.map(e => e.description).join('\n');
        } else {
            // その他のエラー
            error = '不明なエラーが発生しました。';
            console.error(await response.text());
        }

    }
</script>

<style>
    .form-container {
        max-width: 400px;
        margin: 50px auto;
        padding: 20px;
        background-color: white;
        border-radius: 10px;
        box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
        box-sizing: border-box;
    }

        .form-container h2 {
            font-size: 1.5rem;
            font-weight: bold;
            text-align: center;
            margin-bottom: 20px;
        }

        .form-container label {
            display: block;
            font-size: 1rem;
            margin-bottom: 5px;
        }

    .required-mark::after {
        content: " *";
        color: red;
        font-weight: bold;
    }

    .form-container input {
        width: 100%;
        padding: 10px;
        margin-bottom: 15px;
        border: 1px solid #ccc;
        border-radius: 5px;
        font-size: 1rem;
        box-sizing: border-box;
    }

        .form-container input[type="password"] {
            -webkit-text-security: disc;
        }

    .form-container .error {
        color: red;
        font-size: 0.875rem;
        margin-bottom: 10px;
    }

    .form-container .success {
        color: green;
        font-size: 0.875rem;
        margin-bottom: 10px;
    }

    .form-container button {
        width: 100%;
        padding: 12px;
        background-color: #007bff;
        color: white;
        border: none;
        border-radius: 5px;
        font-size: 1rem;
        cursor: pointer;
        box-sizing: border-box;
    }

        .form-container button:hover {
            background-color: #0056b3;
        }
</style>


<div class="form-container">
    <h1>初期ユーザー作成</h1>

    <div>
        <label for="passcode" class="required-mark">作成用パスコード</label>
        <input id="passcode" type="text" bind:value={passcode} />
    </div>

    <div>
        <label for="username" class="required-mark">ユーザー名</label>
        <input id="username" type="text" bind:value={username} />
    </div>
    <div>
        <label for="email">メールアドレス(任意)</label>
        <input id="email" type="text" bind:value={email} />
    </div>
    <div>
        <label for="password" class="required-mark">パスワード</label>
        <input id="password" type="password" bind:value={password} />
    </div>

    <div>
        <label for="confirmPassword" class="required-mark">パスワードの確認</label>
        <input id="confirmPassword" type="password" bind:value={confirmPassword} />
    </div>

    {#if error}
    <div class="error">{error}</div>
    {/if}

    {#if success}
    <div class="success">{success}</div>
    {/if}

    <button on:click={handleSubmit}>作成する</button>
</div>
