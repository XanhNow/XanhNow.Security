#!/usr/bin/env bash
set -Eeuo pipefail

source_dir="/etc/xanhnow/s101/smart-otp/mtls"
target_dir="/etc/xanhnow/s101/security/smart-otp-mtls"
dropin_dir="/etc/systemd/system/xanhnow-security-api.service.d"

ca_crt="$source_dir/smart-otp-ca.crt"
client_crt="$source_dir/xanhnow-security-client.crt"
client_key="$source_dir/xanhnow-security-client.key"

test -s "$ca_crt"
test -s "$client_crt"
test -s "$client_key"

install -d -o root -g xanhnow -m 0750 "$target_dir"
install -o root -g xanhnow -m 0640 "$ca_crt" "$target_dir/smart-otp-ca.crt"
install -o root -g xanhnow -m 0640 "$client_crt" "$target_dir/xanhnow-security-client.crt"
install -o root -g xanhnow -m 0640 "$client_key" "$target_dir/xanhnow-security-client.key"

install -d -o root -g root -m 0755 "$dropin_dir"
rm -f "$dropin_dir/10-smartotp-mtls.conf"
rm -f "$dropin_dir/20-child-app-endpoints.conf"
rm -f "$dropin_dir/30-smartotp-mtls.conf"
rm -f "$dropin_dir/90-smartotp-plaintext.conf"

cat > "$dropin_dir/90-smart-otp-mtls.conf" <<EOF
[Service]
Environment=SecurityIntegration__SmartOtp__BaseAddress=https://localhost:5104
Environment=SecurityIntegration__SmartOtp__RequiresMtls=true
Environment=SecurityIntegration__SmartOtp__TrustedCaPath=$target_dir/smart-otp-ca.crt
Environment=SecurityIntegration__SmartOtp__ClientCertificatePath=$target_dir/xanhnow-security-client.crt
Environment=SecurityIntegration__SmartOtp__ClientCertificateKeyPath=$target_dir/xanhnow-security-client.key
EOF

systemctl daemon-reload
echo "Security Smart OTP mTLS client material installed."
