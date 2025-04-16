<script lang="ts">
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';
    import type { SendUser } from '$lib/models/user';

    let users: SendUser[] = [];
    let loading = true;
    let error: string | null = null;
    let showPassword = false;
    let newUserName = '';
    let newPassword = '';
    let newEmail = '';
    let registerError: string | null = null;
    let errorMessages = [];

    onMount(loadUsers);

    async function loadUsers() {
        loading = true;
        error = null;
        try {
            const res = await fetch('/api/user/UserList');
            if (!res.ok) throw new Error(`Error ${res.status}`);
            users = await res.json();
        } catch (e) {
            error = e.message;
        } finally {
            loading = false;
        }
    }

    async function registerUser() {
        registerError = null;

        if (!newUserName || !newPassword) {
            registerError = 'ユーザー名とパスワードは必須です。';
            return;
        }
        errorMessages = [];

        try {
            const response = await fetch('/api/user/register', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    userName: newUserName,
                    password: newPassword,
                    email: newEmail,
                }),
            });

            if (response.ok) {
                // 登録成功時の処理
                newUserName = '';
                newPassword = '';
                newEmail = '';
                alert("ユーザーが追加されました。");
                loadUsers();
                // 必要に応じてユーザー一覧を更新
            } else {
                const data = await response.json();
                if (Array.isArray(data)) {
                    // IdentityError[] の形式で返ってきた場合
                    errorMessages = data.map((e) => e.description);
                } else {
                    // それ以外の形式（汎用的なエラーメッセージ）
                    errorMessages = [data?.message || 'ユーザー登録に失敗しました。'];
                }
            }
        } catch (error) {
            console.log(error)
            errorMessages = ['通信エラーが発生しました。'];
        }
    }
    function goToUserSettings(userName: string) {
        goto(`/users/${encodeURIComponent(userName)}`);
    }
    async function deleteUser(userNameToDelete: string) {
        if (!confirm(`ユーザー「${userNameToDelete}」を削除しますか？`)) return;

        try {
            const response = await fetch('/api/user/DeleteUser', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(userNameToDelete),
            });

            if (response.ok) {
                users = users.filter(u => u.userName !== userNameToDelete);
            } else {
                const data = await response.json();
                alert(data?.message || '削除に失敗しました。');
            }
        } catch (err) {
            alert('通信エラーが発生しました。');
        }
    }
    let showModal = false;
    let targetUserToDelete: string | null = null;
    let modalError: string | null = null;

    function confirmDelete(userName: string) {
        targetUserToDelete = userName;
        showModal = true;
        modalError = null;
    }

    async function executeDelete() {
        if (!targetUserToDelete) return;

        try {
            const response = await fetch('/api/user/DeleteUser', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify( targetUserToDelete),
            });

            if (response.ok) {
                users = users.filter(u => u.userName !== targetUserToDelete);
                showModal = false;
            } else {
                const data = await response.json();
                modalError = data?.message || '削除に失敗しました。';
            }
        } catch (err) {
            modalError = '通信エラーが発生しました。';
        }
    }

    function closeModal() {
        showModal = false;
        targetUserToDelete = null;
        modalError = null;
    }
</script>

<style>
    /* 共通コンテナ */
    .container {
        max-width: 800px;
        margin: 20px auto;
        padding: 10px;
    }

    /* タイトル */
    .title {
        font-size: 1.5rem;
        font-weight: bold;
        margin-bottom: 16px;
    }

    /* ユーザー一覧リスト */
    .user-list {
        list-style: none;
        padding: 0;
    }

    .user-item {
        padding: 12px;
        border: 1px solid #ccc;
        border-radius: 6px;
        margin-bottom: 10px;
        cursor: pointer;
        transition: background-color 0.2s ease-in-out;
    }

        .user-item:hover {
            background-color: #f5f5f5;
        }

    .username {
        font-weight: bold;
    }

    .roles {
        font-size: 0.9rem;
        color: #666;
        margin-top: 4px;
    }

    /* エラーメッセージ */
    .error,
    .error-message {
        color: red;
        margin-top: 10px;
        margin-bottom: 10px;
    }

    /* ユーザー追加フォーム（containerと統一幅） */
    .register-container {
        border: 1px solid #ccc;
        border-radius: 5px;
        padding: 20px;
        margin: 40px auto;
        max-width: 800px;
        background-color: #fafafa;
    }

    h2 {
        margin-bottom: 20px;
    }

    /* フォームのラベルと入力欄 */
    .form-group {
        margin-bottom: 15px;
    }

    label {
        display: block;
        margin-bottom: 5px;
    }

    input[type="text"],
    input[type="password"],
    input[type="email"] {
        width: 100%;
        padding: 10px;
        border: 1px solid #ddd;
        border-radius: 3px;
        box-sizing: border-box;
    }

    /* ボタン */
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

    /* パスワード表示切り替え */
    .password-input {
        position: relative;
    }

        .password-input input {
            width: 100%;
            padding-right: 80px;
        }

    .toggle-text {
        position: absolute;
        right: 10px;
        top: 50%;
        transform: translateY(-50%);
        background: none;
        border: none;
        color: #007bff;
        font-weight: bold;
        cursor: pointer;
        font-size: 0.9rem;
    }

    .user-item {
        display: flex;
        justify-content: space-between;
        align-items: center;
    }

    .user-info {
        flex-grow: 1;
        cursor: pointer;
    }

    .delete-button {
        background-color: #dc3545;
        color: white;
        border: none;
        padding: 6px 12px;
        border-radius: 4px;
        cursor: pointer;
        transition: background-color 0.2s ease;
    }

        .delete-button:hover {
            background-color: #c82333;
        }

    .modal-overlay {
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background-color: rgba(0, 0, 0, 0.4);
        display: flex;
        justify-content: center;
        align-items: center;
        z-index: 1000;
    }

    .modal {
        background-color: #fff;
        padding: 20px;
        border-radius: 8px;
        width: 90%;
        max-width: 400px;
        box-shadow: 0 2px 10px rgba(0,0,0,0.3);
    }

        .modal h3 {
            margin-top: 0;
        }

    .modal-buttons {
        margin-top: 20px;
        display: flex;
        justify-content: flex-end;
        gap: 10px;
    }

    .modal-error {
        color: red;
        margin-top: 10px;
    }
</style>

<div class="container">
    {#if showModal}
    <div class="modal-overlay">
        <div class="modal">
            <h3>ユーザー削除の確認</h3>
            <p>「{targetUserToDelete}」を削除してもよろしいですか？</p>
            {#if modalError}
            <p class="modal-error">{modalError}</p>
            {/if}
            <div class="modal-buttons">
                <button class="delete-button" on:click={executeDelete}>削除する</button>
                <button on:click={closeModal}>キャンセル</button>
            </div>
        </div>
    </div>
    {/if}

    <div class="title">ユーザー一覧</div>
    <!-- ユーザー追加フォーム（ログイン画面と統一デザイン） -->
    <div class="register-container">
        <h2>新規ユーザー追加</h2>
        {#if registerError}
        <p class="error-message">{registerError}</p>
        {/if}
        <div class="form-group">
            <label for="newUserName">ユーザー名</label>
            <input type="text" id="newUserName" bind:value={newUserName} />
        </div>
        <div class="form-group password-wrapper">
            <label for="newPassword">パスワード</label>
            <div class="password-input">
                <input type={showPassword ? 'text' : 'password' }
                       id="newPassword"
                       bind:value={newPassword} />
                <button type="button" class="toggle-text" on:click={() =>
                    showPassword = !showPassword}>
                    {showPassword ? '非表示' : '表示'}
                </button>
            </div>
        </div>
        <div class="form-group">
            <label for="newEmail">メールアドレス（任意）</label>
            <input type="email" id="newEmail" bind:value={newEmail} />
        </div>
        {#if errorMessages.length > 0}
        <ul class="error-message">
            {#each errorMessages as msg}
            <li>{msg}</li>
            {/each}
        </ul>
        {/if}
        <button on:click={registerUser}>登録</button>
    </div>

    {#if loading}
    <p>読み込み中...</p>
    {:else if error}
    <p class="error">エラー: {error}</p>
    {:else if users.length === 0}
    <p>ユーザーが見つかりませんでした。</p>
    {:else}
    <ul class="user-list">
        {#each users as user}
        <li class="user-item">
            <div class="user-info" on:click={() =>
                goToUserSettings(user.userName)}>
                <div class="username">{user.userName}</div>
                <div class="roles">
                    ロール:
                    {#each user.roles as role, i}
                    {role.name}{i < user.roles.length - 1 ? ', ' : ''}
                    {/each}
                </div>
            </div>
            <button class="delete-button" on:click={() => confirmDelete(user.userName)}>削除</button>
        </li>
        {/each}
    </ul>
    {/if}
</div>
