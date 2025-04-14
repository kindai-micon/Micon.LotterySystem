import { writable } from 'svelte/store';
interface Authority {
    Name: string;
}
interface Role {
    Name: string;
    Authorities: Authority[];
}

interface User {
    UserName: string;
    Roles: Role[];
}

// 初期値（未ログイン状態）
const initialUser: User | null = null;

export const user = writable<User | null>(initialUser);