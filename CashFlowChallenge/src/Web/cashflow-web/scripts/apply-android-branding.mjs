import fs from 'node:fs';
import path from 'node:path';

const root = process.cwd();
const androidRoot = path.join(root, 'android', 'app', 'src', 'main');
const drawableDir = path.join(androidRoot, 'res', 'drawable');
const manifestPath = path.join(androidRoot, 'AndroidManifest.xml');

fs.mkdirSync(drawableDir, { recursive: true });

const vector = `<?xml version="1.0" encoding="utf-8"?>
<vector xmlns:android="http://schemas.android.com/apk/res/android"
    android:width="108dp"
    android:height="108dp"
    android:viewportWidth="108"
    android:viewportHeight="108">
    <path android:fillColor="#0F172A" android:pathData="M18,0h72a18,18 0,0 1,18 18v72a18,18 0,0 1,-18 18h-72a18,18 0,0 1,-18 -18v-72a18,18 0,0 1,18 -18z" />
    <path android:fillColor="#00000000" android:strokeColor="#FFFFFF" android:strokeWidth="6" android:strokeLineJoin="round" android:pathData="M27,34h54a10,10 0,0 1,10 10v34a10,10 0,0 1,-10 10h-54a10,10 0,0 1,-10 -10v-34a10,10 0,0 1,10 -10z" />
    <path android:fillColor="#00000000" android:strokeColor="#60A5FA" android:strokeWidth="6" android:strokeLineCap="round" android:pathData="M34,34v-5a8,8 0,0 1,8 -8h31a8,8 0,0 1,8 8v5" />
    <path android:fillColor="#1E293B" android:strokeColor="#FFFFFF" android:strokeWidth="6" android:strokeLineJoin="round" android:pathData="M66,51h20a7,7 0,0 1,7 7v8a7,7 0,0 1,-7 7h-20a11,11 0,0 1,0 -22z" />
    <path android:fillColor="#60A5FA" android:pathData="M68,62m-3,0a3,3 0,1 1,6 0a3,3 0,1 1,-6 0" />
</vector>`;

fs.writeFileSync(path.join(drawableDir, 'ic_cashflow_launcher.xml'), vector);

let manifest = fs.readFileSync(manifestPath, 'utf8');
manifest = manifest
  .replace('android:icon="@mipmap/ic_launcher"', 'android:icon="@drawable/ic_cashflow_launcher"')
  .replace('android:roundIcon="@mipmap/ic_launcher_round"', 'android:roundIcon="@drawable/ic_cashflow_launcher"');
fs.writeFileSync(manifestPath, manifest);

console.log('CashFlow Android launcher branding applied.');
