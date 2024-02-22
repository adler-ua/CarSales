/** @type {import('next').NextConfig} */
const nextConfig = {
    images: {
        domains: [
            /* whitelisting of domains to allow load external images */
            'cdn.pixabay.com'
        ]
    }
};

export default nextConfig;
