<script lang="ts">
    import Menu from "../components/menu.svelte";
    import { page } from '$app/stores';
    import { goto } from "$app/navigation";
    import { user } from "../store/UserStore.ts";
    import { loadUser } from "../store/UserStore.ts";
    import { onMount } from 'svelte';
    import type { User } from '../stores/UsetStore.ts';
    let { children } = $props();
    let currentUser: User | null = null;

    let isLoginPath = $state(false);
    const menuItems = [
        { name: 'ユーザー管理', href: '/users' },
        { name: 'ロール管理', href: '/roles' },
        { name: '抽選会管理', href: '/lottery' },


    ];
    const unsubscribe = user.subscribe(value => {
        currentUser = value;
    });
    const isLotteryViewPath = window.location.pathname.match(/\/lottery\/[^\/]+\/view/);

    console.log(currentUser);
    onMount(async () => {
        if (!window.location.pathname.startsWith("/login") && !window.location.pathname.startsWith("/initial")&&!window.location.pathname.startsWith("/live") && !window.location.pathname.startsWith("/ticket")&& !isLotteryViewPath) {

	await loadUser();
	console.log(currentUser);
	if (currentUser === null) {

	await goto("/login")
	isLoginPath = true;
	}
	else {

	}
	}
	else {
	isLoginPath = true;
	}
	})
	let drawerOpen = $state(false);

	const toggleDrawer = () => {
	drawerOpen = !drawerOpen;
	};
</script>

<style>
    * {
        box-sizing: border-box;
    }

    body {
        margin: 0;
        font-family: sans-serif;
    }

    .app-container {
        display: flex;
        flex-direction: column;
        min-height: 100vh;
    }

    header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        background-color: #3f51b5;
        color: white;
        padding: 0.75rem 1rem;
        position: sticky;
        top: 0;
        z-index: 100;
    }

    .header-left {
        display: flex;
        align-items: center;
    }

    .menu-button {
        display: none;
        font-size: 1.5rem;
        background: none;
        border: none;
        color: white;
        cursor: pointer;
        margin-right: 1rem;
    }

    .title {
        font-size: 1.2rem;
        font-weight: bold;
    }

    .layout-body {
        display: flex;
        flex: 1;
        height: 100%;
    }

    .sidebar {
        width: 240px;
        background-color: #f4f4f4;
        padding: 1rem;
        border-right: 1px solid #ddd;
    }

        .sidebar nav a {
            display: block;
            padding: 0.5rem;
            color: #333;
            text-decoration: none;
            border-radius: 4px;
        }

            .sidebar nav a:hover {
                background-color: #ddd;
            }

    .main {
        flex: 1;
        padding: 1rem;
    }

    /* モバイル用ドロワー */
    .drawer {
        position: fixed;
        top: 0;
        left: 0;
        width: 240px;
        height: 100%;
        background-color: #f4f4f4;
        padding: 1rem;
        box-shadow: 2px 0 5px rgba(0, 0, 0, 0.2);
        z-index: 200;
    }

        .drawer nav a {
            display: block;
            padding: 0.5rem;
            text-decoration: none;
            color: #333;
        }

            .drawer nav a:hover {
                background-color: #ddd;
            }

    .drawer-close {
        background: none;
        border: none;
        font-size: 1.2rem;
        margin-bottom: 1rem;
        cursor: pointer;
    }

    @media (max-width: 768px) {
        .sidebar {
            display: none;
        }

        .menu-button {
            display: block;
        }

        .layout-body {
            flex-direction: column;
        }
    }
</style>

<div class="app-container">
    <!-- ヘッダー -->
    <header>
        <div class="header-left">
            {#if !isLoginPath}

            <button class="menu-button" onclick={toggleDrawer}>☰</button>
            {/if}
            <div class="title">抽選管理システム</div>
        </div>
    </header>
    <!-- モバイル用サイドメニュー -->
    {#if drawerOpen}
    <div class="drawer">
        <button class="drawer-close" onclick={toggleDrawer}>✖ 閉じる</button>
        <nav>
            {#each menuItems as item}
            <a href={item.href} onclick={toggleDrawer}>{item.name}</a>
            {/each}
        </nav>
    </div>
    {/if}


    <!-- 本体 -->
    <div class="layout-body">
        <!-- デスクトップ用サイドバー -->
        {#if !isLoginPath}
        <aside class="sidebar">
            <nav>
                {#each menuItems as item}
                <a href={item.href}>{item.name}</a>
                {/each}

            </nav>
        </aside>
        {/if}
        <main class="main">
            <slot />
        </main>
    </div>
</div>