# This repository now builds and runs via service-level Dockerfiles in docker-compose.yml.
# Legacy monolith image build steps were removed to avoid duplicated architecture paths.

FROM alpine:3.20
WORKDIR /workspace
CMD ["sh", "-c", "echo 'Use: docker compose up --build' && sleep infinity"]