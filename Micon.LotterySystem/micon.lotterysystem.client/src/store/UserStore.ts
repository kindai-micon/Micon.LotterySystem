import { writable } from 'svelte/store';

export interface User {
    userName: string;
    roles: { name: string }[];
}

interface Authority {
    name: string;
}

interface Role {
    name: string;
    authorities: Authority[];
}

// 初期値（未ログイン状態）
const initialUser: User | null = null;
export const user = writable<User | null>(initialUser);

export async function loadUser(): Promise<void> {
    try {
        const res = await fetch('/api/user/MyInfo');
        if (res.ok) {
            const userData: User = await res.json();
            user.set(userData);
        } else {
            console.log('ユーザー情報の取得に失敗: ステータス', res.status);
            user.set(null);
        }
    } catch (error) {
        console.error('ユーザー情報の取得に失敗:', error);
        user.set(null);
    }
}
