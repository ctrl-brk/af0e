const fs = require('node:fs');
const path = require('node:path');

const root = process.cwd();
const source = path.join(root, 'node_modules', 'mapbox-gl', 'dist', 'mapbox-gl-csp-worker.js');
const target = path.join(root, 'public', 'mapbox-gl-csp-worker.js');

if (!fs.existsSync(source)) {
  console.error(`Mapbox worker not found at: ${source}`);
  process.exit(1);
}

fs.mkdirSync(path.dirname(target), { recursive: true });
fs.copyFileSync(source, target);

console.log('Synced mapbox-gl-csp-worker.js to public/.');

