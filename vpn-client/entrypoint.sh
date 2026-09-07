#!/bin/sh
set -eu
exec openvpn --config /vpn/vpn.conf \
  --redirect-gateway def1 \
  --script-security 2
