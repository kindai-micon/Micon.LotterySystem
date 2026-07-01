import adapter from '@sveltejs/adapter-static';

/** @type {import('@sveltejs/kit').Config} */
const config = {
    kit: {
        adapter: adapter({
            pages: '../wwwroot',
            assets: '../wwwroot',
            fallback: 'index.html'
        })
    }
};

export default config;