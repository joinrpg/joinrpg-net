#!/usr/bin/env node
/**
 * Downloads Roboto font files from Google Fonts for self-hosting.
 *
 * Usage: node tools/download-roboto-fonts.js
 *
 * After running, commit the downloaded files:
 *   git add src/JoinRpg.WebComponents/wwwroot/fonts/Roboto/
 *   git commit -m "Add Roboto font files for self-hosting"
 */
const https = require('https');
const fs = require('fs');
const path = require('path');

const destDir = path.join(__dirname, '../src/JoinRpg.WebComponents/wwwroot/fonts/Roboto');
fs.mkdirSync(destDir, { recursive: true });

function get(url, headers = {}) {
    return new Promise((resolve, reject) => {
        https.get(url, { headers }, (res) => {
            if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
                return get(res.headers.location, headers).then(resolve).catch(reject);
            }
            const chunks = [];
            res.on('data', c => chunks.push(c));
            res.on('end', () => resolve(Buffer.concat(chunks)));
            res.on('error', reject);
        }).on('error', reject);
    });
}

// Mapping from Google Fonts unicode-range to file name subset
const subsetMap = [
    { name: 'cyrillic-ext', ranges: ['U+0460-052F'] },
    { name: 'cyrillic',     ranges: ['U+0301, U+0400-045F'] },
    { name: 'latin-ext',    ranges: ['U+0100-02AF'] },
    { name: 'latin',        ranges: ['U+0000-00FF'] },
];

function guessSubset(unicodeRange) {
    if (unicodeRange.includes('U+0460')) return 'cyrillic-ext';
    if (unicodeRange.includes('U+0400') || unicodeRange.includes('U+0301, U+0400')) return 'cyrillic';
    if (unicodeRange.includes('U+0100')) return 'latin-ext';
    return 'latin';
}

async function main() {
    const ua = 'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36';

    console.log('Fetching Google Fonts CSS (Roboto 400, 400i, 700, 700i with cyrillic)...');
    const cssUrl = 'https://fonts.googleapis.com/css2?family=Roboto:ital,wght@0,400;0,700;1,400;1,700&display=swap';
    const css = (await get(cssUrl, { 'User-Agent': ua })).toString();

    // Parse @font-face blocks
    const blocks = [...css.matchAll(/@font-face\s*\{([^}]+)\}/gs)].map(m => m[1]);
    console.log(`Found ${blocks.length} @font-face blocks`);

    const downloads = [];
    for (const block of blocks) {
        const urlMatch    = block.match(/src:\s*url\((https:\/\/fonts\.gstatic\.com\/[^)]+\.woff2)\)/);
        const weightMatch = block.match(/font-weight:\s*(\d+)/);
        const styleMatch  = block.match(/font-style:\s*(\w+)/);
        const rangeMatch  = block.match(/unicode-range:\s*([^\n;]+)/);

        if (!urlMatch) continue;

        const weight = weightMatch?.[1] ?? '400';
        const style  = styleMatch?.[1]  ?? 'normal';
        const range  = rangeMatch?.[1]?.trim() ?? '';
        const subset = guessSubset(range);
        const styleTag = style === 'italic' ? 'italic' : 'normal';
        const filename = `roboto-${subset}-${weight}-${styleTag}.woff2`;

        downloads.push({ url: urlMatch[1], filename });
    }

    // Deduplicate by filename (keep first)
    const seen = new Set();
    const unique = downloads.filter(d => { if (seen.has(d.filename)) return false; seen.add(d.filename); return true; });

    console.log(`\nDownloading ${unique.length} font files to ${destDir}:`);
    for (const { url, filename } of unique) {
        const dest = path.join(destDir, filename);
        process.stdout.write(`  ${filename} ... `);
        const data = await get(url);
        fs.writeFileSync(dest, data);
        console.log(`${data.length} bytes`);
    }

    console.log('\nDone! Now run:');
    console.log('  git add src/JoinRpg.WebComponents/wwwroot/fonts/Roboto/');
    console.log('  git commit -m "Add Roboto font files for self-hosting"');
}

main().catch(err => { console.error(err.message); process.exit(1); });
