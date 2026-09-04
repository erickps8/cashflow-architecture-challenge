const required = [
  'ANDROID_KEYSTORE_BASE64',
  'ANDROID_KEYSTORE_PASSWORD',
  'ANDROID_KEY_ALIAS',
  'ANDROID_KEY_PASSWORD',
];

const missing = required.filter((name) => !process.env[name]);

if (missing.length) {
  console.error(`Missing Play Store signing secrets: ${missing.join(', ')}`);
  process.exit(1);
}

console.log('Play Store signing secrets are configured.');
