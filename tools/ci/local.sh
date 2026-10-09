#!/usr/bin/env bash
# The gate, run on this machine before anything is merged. Prints OK on its last line when everything passed.
#   tools/ci/local.sh          build, tests, then the site in Docker with an empty content folder: every page must answer
#   tools/ci/local.sh quick    build and tests only
set -euo pipefail
cd "$(dirname "$0")/../.."

echo "== Build and tests"
dotnet test PublicDomainAudioBooks.sln -c Release --nologo 2>&1 | grep -E " error |warn RZ|Failed |Passed!|Failed!" || true
dotnet test PublicDomainAudioBooks.sln -c Release --nologo --no-build > /dev/null

if [ "${1:-}" != "quick" ]; then
  echo "== The site in Docker, with no books (project pdab-smoke, http://127.0.0.1:8229)"
  export COMPOSE_PROJECT_NAME=pdab-smoke PDAB_PORT=8229
  trap 'docker compose down --remove-orphans > /dev/null 2>&1 || true' EXIT
  docker compose up --build -d > /dev/null 2>&1 || { echo "FAILED: the image did not build or start"; docker compose logs --tail 30; exit 1; }
  for i in $(seq 1 40); do curl -s -m 3 -o /dev/null http://127.0.0.1:8229/healthz && break; sleep 3; done
  for page in / /books /authors /about /feeds/podcast.xml /sitemap.xml /robots.txt /healthz /held.json; do
    code=$(curl -s -m 20 -o /dev/null -w '%{http_code}' "http://127.0.0.1:8229$page")
    [ "$code" = "200" ] || { echo "FAILED: $page answered $code"; exit 1; }
  done
  code=$(curl -s -m 20 -o /dev/null -w '%{http_code}' http://127.0.0.1:8229/books/no-such-book)
  [ "$code" = "404" ] || { echo "FAILED: a book that is not there answered $code, not 404"; exit 1; }
  curl -s -m 20 http://127.0.0.1:8229/ | grep -q "neoui-extra" || { echo "FAILED: the home page does not load the interface's styles"; exit 1; }
  curl -s -m 20 http://127.0.0.1:8229/ | grep -q "The first books are on their way" || { echo "FAILED: the empty home page does not say it is empty"; exit 1; }
  echo "every page answered"
fi

echo
echo "OK"
