# Play Store release

O workflow `.github/workflows/android-play-store.yml` gera o AAB de release a partir da `master`.

Secrets obrigatórios no GitHub:

- `ANDROID_KEYSTORE_BASE64`
- `ANDROID_KEYSTORE_PASSWORD`
- `ANDROID_KEY_ALIAS`
- `ANDROID_KEY_PASSWORD`

A chave de assinatura e suas senhas nunca devem ser commitadas no repositório.
