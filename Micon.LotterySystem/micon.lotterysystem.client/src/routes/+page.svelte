<script lang="ts">
    import { user } from "../store/UserStore";
    import type { User } from '../store/UserStore';
    import { loadUser } from "../store/UserStore";
    import { onMount } from 'svelte';

</script>
{#if $user != null}
<h1>Welcome {$user.userName}</h1>
<p>Visit <a href="https://svelte.dev/docs/kit">svelte.dev/docs/kit</a> to read the documentation</p>
{/if}