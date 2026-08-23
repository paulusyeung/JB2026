#!/bin/sh
set -e

# Substitute the backend URL into the nginx config. Falls back to the
# bundled docker-compose service name when BACKEND_URL is not provided.
export BACKEND_URL="${BACKEND_URL:-http://backend:8080}"

envsubst '${BACKEND_URL}' \
    < /etc/nginx/conf.d/default.conf.template \
    > /etc/nginx/conf.d/default.conf
