#!/bin/bash
set -e

if [ -z "$GOOGLE_APPLICATION_CREDENTIALS" ] || [ ! -f "$GOOGLE_APPLICATION_CREDENTIALS" ]; then
    echo "ERROR: GOOGLE_APPLICATION_CREDENTIALS is not set or file does not exist"
    exit 1
fi

# Exchange SA credentials for an OAuth access token scoped to cloudkms.
# gcloud is NOT installed on windows-latest runners; google-auth Python lib
# is the reliable path (see browserstack-csharp-sdk PR #704 for the
# enumeration of failed alternatives).
GCP_ACCESS_TOKEN=$(python3 -c "
import google.auth
import google.auth.transport.requests
creds, _ = google.auth.default(scopes=['https://www.googleapis.com/auth/cloudkms'])
creds.refresh(google.auth.transport.requests.Request())
print(creds.token)
")

# Download Jsign 7.0 + SHA-256 verify (supply-chain integrity).
JSIGN_VERSION="7.0"
JSIGN_SHA256="325df319621e7fa74384c8852efdb5828871bf6405648a4c621ee5fc37c59b6c"
curl -fsL "https://github.com/ebourg/jsign/releases/download/${JSIGN_VERSION}/jsign-${JSIGN_VERSION}.jar" -o jsign.jar
echo "${JSIGN_SHA256}  jsign.jar" | sha256sum -c - || { echo "ERROR: Jsign checksum verification failed"; exit 1; }

# Sign every nupkg produced by the pack step.
SIGNED_ANY=0
for NUPKG in BrowserStackLocal/BrowserStackLocal/bin/Release/*.nupkg; do
    if [ ! -f "$NUPKG" ]; then
        continue
    fi
    echo "Signing $NUPKG"
    java -jar jsign.jar \
        --storetype GOOGLECLOUD \
        --storepass "$GCP_ACCESS_TOKEN" \
        --keystore "projects/browserstack-production/locations/us-east1/keyRings/prod-comodo-win-cert-keyring" \
        --alias "prod-comodo-win-cert-key/cryptoKeyVersions/1" \
        --certfile comodo_signing_cert.crt \
        --tsaurl https://timestamp.sectigo.com \
        "$NUPKG"
    SIGNED_ANY=1
done

if [ "$SIGNED_ANY" -eq 0 ]; then
    echo "ERROR: no .nupkg files found under BrowserStackLocal/BrowserStackLocal/bin/Release/"
    exit 1
fi

echo "Signing complete"
