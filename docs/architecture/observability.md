# OpenTelemetry and Honeycomb

[Documentation index](README.md)

## Current implementation

Flight, booking and seating share opt-in OpenTelemetry registration in
[SharedObservabilityExtensions](../../backend/shared/Infrastructure/DependencyInjection/SharedObservabilityExtensions.cs).
Normal startup and CI do not export telemetry. `Telemetry:Enabled=true` requires
an explicit HTTP(S) `OTEL_EXPORTER_OTLP_ENDPOINT`; invalid/missing configuration
fails startup rather than silently selecting a destination.

The [telemetry Compose overlay](../../compose.telemetry.yaml) enables all three
backends and adds one shared OpenTelemetry Collector container:

```text
flight-service  --+
booking-service --+-- OTLP/gRPC (private Docker network) --> Collector --> HTTPS OTLP --> Honeycomb
seating-service --+
```

This is a shared collector, not a per-application sidecar. Application containers
know only the collector endpoint. Only the collector receives the Honeycomb ingest
key. The normal [Compose stack](../../compose.yaml) remains usable without that key.
The collector image is pinned to `otel/opentelemetry-collector-contrib:0.165.0`.

### Signals

- Traces: ASP.NET Core requests, outgoing HttpClient requests, MassTransit
  publish/send/receive/consume activities, and Npgsql database activities.
  Service names are `flight-service`, `booking-service`, and `seating-service`.
  ASP.NET Core `/health` traces are excluded. Exceptions are not recorded by the
  HTTP instrumentation as free-text exception events.
- Metrics: ASP.NET Core and HttpClient request metrics, Kestrel metrics, and
  MassTransit's built-in meter. Compose exports every 15 seconds.
- Logs: an additional OpenTelemetry provider exports Warning and above, retaining
  severity, timestamp, instrumentation scope/category, event identifiers where
  supplied, and trace/span correlation. Console logging is not replaced.

MassTransit propagates activity context through messaging. The live booking/seating
workflow was verified to export traces sharing trace IDs across both services.
Asynchronous work can outlive an HTTP request; do not keep its server span open
until reservation completion.

Browser/Node SSR instrumentation, infrastructure/host metrics, custom business
metrics and alerts are not part of this initial implementation. Optional trace
sampling is described below; the default collector exports all recorded traces.
The frontend still uses sample data and is not connected to the booking API.

## Local setup

1. Follow [Docker prerequisites and schema provisioning](../../docker/README.md).
2. Create a Honeycomb **ingest** API key for the US `test` environment. MCP OAuth
   grants query/control access, not collector ingestion credentials.
3. Add these settings to the existing ignored root `.env`. Never paste the key
   into chat, commit it, or put it in frontend/application configuration:

   ```dotenv
   HONEYCOMB_API_KEY=<supply-locally>
   HONEYCOMB_ENDPOINT=https://api.honeycomb.io
   HONEYCOMB_METRICS_DATASET=polonia-metrics
   OTEL_HEALTH_PORT=13133
   ```

   [`.env.example`](../../.env.example) contains empty/non-secret placeholders.
   Do not overwrite an existing `.env` when copying the example.
   The key must be allowed to ingest into the chosen datasets, including dataset
   creation if they do not already exist.

4. From the repository root, after database migrations:

   ```powershell
   docker compose -f compose.yaml -f compose.telemetry.yaml config --quiet
   docker compose -f compose.yaml -f compose.telemetry.yaml up -d --build --wait
   Invoke-WebRequest -UseBasicParsing http://localhost:13133/
   docker compose -f compose.yaml -f compose.telemetry.yaml logs --tail 50 otel-collector
   ```

   Never print/save the full rendered Compose configuration: it contains secrets.
   Collector health is listener/process liveness, **not** proof that Honeycomb
   accepted data. Backends depend on collector process startup; exporter retries
   handle temporary receiver unavailability.

5. Generate non-sensitive requests, such as an empty catalog read:

   ```powershell
   Invoke-WebRequest -UseBasicParsing http://localhost:5102/api/booking/catalog
   ```

   Allow roughly 15 seconds for metrics and batching. Inspect the Honeycomb `test`
   environment for the service datasets and its `metrics` dataset (or the
   configured dataset on accounts using dataset-header routing). A successful
   request alone does not prove export; check collector errors and Honeycomb data.
   Do not use real passengers or guest credentials in the local HTTP-only stack.

6. Stop with both files so the collector is included:

   ```powershell
   docker compose -f compose.yaml -f compose.telemetry.yaml down
   ```

Traces/logs route by `service.name` in modern Honeycomb environments. The collector
sends the explicit `x-honeycomb-dataset` header for metrics, but this team's `test`
environment was observed to ingest them into the built-in `metrics` dataset
despite the `polonia-metrics` header. Discover the actual dataset before querying;
do not assume the header determines routing on every Honeycomb account.
Honeycomb Classic would require a
different trace/log dataset configuration. For EU, use
`https://api.eu1.honeycomb.io` and a key from the EU environment.

For a backend running directly on the host, point it at a collector you operate:

```powershell
$env:Telemetry__Enabled = 'true'
$env:OTEL_EXPORTER_OTLP_ENDPOINT = 'http://localhost:4317'
```

The checked-in overlay deliberately does **not** publish OTLP ports. Add an
explicit localhost-only port mapping for host development; do not publicly expose
the unauthenticated receiver. The only published collector endpoint is
localhost-bound liveness.

## Privacy and reliability boundaries

### Optional error-focused sampling

The [sampling configuration](../../docker/otel/collector.sampled.yaml) is a
partial collector configuration merged after the default file, reusing its
receivers, privacy filters and exporters without duplicating them. Enable it with
the [additional Compose overlay](../../compose.telemetry.sampled.yaml):

```powershell
docker compose -f compose.yaml -f compose.telemetry.yaml -f compose.telemetry.sampled.yaml config --quiet
docker compose -f compose.yaml -f compose.telemetry.yaml -f compose.telemetry.sampled.yaml up -d --build --wait
```

Use all three files for subsequent `logs`, `ps` and `down` commands. Omit the
sampling overlay to return to the default unsampled collector.

- Keep traces if any received span has OTel `ERROR` status or an integer
  `http.response.status_code` between 500 and 599. These policies are ORed with
  a trace-ID-based 5% probabilistic baseline for other traces; small batches
  will not necessarily contain exactly 5%. HTTP 4xx/business rejections alone
  are not treated as errors. Instrumentation must mark failures correctly;
  warning/error logs alone do not trigger trace retention.
- Services use `OTEL_TRACES_SAMPLER=always_on` so the SDK records all instrumented
  spans, including spans with an unsampled incoming parent. Sampling happens
  only at the collector; application-to-collector trace traffic is not reduced.
  Other callers must likewise avoid head sampling for complete error traces.
- Metrics remain unsampled. Keep all Warning/Error/Fatal logs independently of
  trace sampling, including logs whose trace is discarded. Lower/unspecified
  severities are filtered; the backend already exports Warning and above.
  Log bodies and attributes still use the same privacy filtering. This does
  not enable full-text logs, additional instrumentation, or extra exception data.
- Wait 30 seconds from the first received span before deciding, then batch for
  export. Buffer at most 4,096 pending traces; this is a local starting point,
  not production sizing. Decision caches keep 16,384 sampled and 65,536 rejected
  trace IDs so late spans can follow prior decisions.
- A failure arriving after a rejection cannot reverse a cached decision.
  Long-running messaging workflows can therefore lose late error traces.
  Cache eviction, buffer pressure, restart or export failures can also cause
  incomplete/lost traces: 100% error retention is the policy, not a delivery
  guarantee. Monitor collector memory/drops and tune the wait/capacity against
  measured traffic before production use.
- All spans for a trace must reach the same tail-sampling collector. Scaling
  collectors requires trace-ID-aware routing, not ordinary load balancing.
  Use unsampled metrics for request/error rates; raw sampled span counts and
  duration distributions are biased toward failures.

### Shared boundaries

The [collector configuration](../../docker/otel/collector.yaml) runs a fail-closed
attribute allowlist before batching/export for all signals:

- Keep service identity/environment and selected HTTP, messaging and database
  system attributes. Keep templated `http.route`, not raw URLs/query strings.
- Remove SQL text, payload attributes, guest/passenger fields, exception messages/
  stack traces and arbitrary resource attributes. Clear span status descriptions.
  Normalize HTTP/database/messaging span names to avoid paths or SQL in names.
- Keep exception event type only, and normalize other event names.
- **Replace exported log bodies with a fixed diagnostic summary.** Log arguments,
  scopes and exception text can contain guest tokens, paths or passenger data, so
  those details remain in local logs rather than Honeycomb. This is deliberately
  conservative; richer safe message templates require a reviewed logging policy.

This protects the configured backend instrumentation, not arbitrary future data
sources or arbitrary user-provided values in allowed fields. Review new
instrumentation and allowlist changes before enabling export. Protect local
console logs separately; collector redaction does not alter them.

The collector has a memory limiter, bounded batch sizes, in-memory sending queues,
and retry/backoff for temporary Honeycomb export failures. Retries stop after five
minutes; full queues/refusals/exhausted retries are logged and can drop telemetry.
Collector restart loses in-memory queued data. This is not durable audit storage
or a guarantee of delivery. No telemetry failure is converted into a successful
business operation, and business persistence remains independent of observability.

Local Compose uses plaintext OTLP on its private network. Production needs
private ingress/TLS as appropriate, secret injection, capacity monitoring,
collector failure alerts, and reviewed retention/sampling. Telemetry is not a
substitute for durable domain events or business analytics.

## Verification status

- Optional sampling: pinned collector merged-config validation and Compose
  merge/dependency/key-isolation checks passed. A local synthetic OTLP test using
  the actual 30-second decision window retained all 40 error traces (20 OTel
  errors, 20 HTTP 503s), including both spans submitted in separate batches.
  Retained 104/2,000 normal traces (5.2%) and 11/200 HTTP 400 traces (baseline,
  not error retention). All 20 metric datapoints and Warning/Error/Fatal logs
  survived; lower/unspecified log severities and injected private fields did
  not. No Honeycomb traffic was sent for this test. The default overlay remains
  unsampled; the temporary collector and test helper were removed.
- Release solution build passed (one pre-existing unread mediator warning).
- Five new registration tests cover disabled/invalid configuration, resource
  identity, incoming W3C context, HTTP/MassTransit activity collection, HTTP
  metrics and correlated logs. All 48 shared integration tests, 18 non-external
  booking integration tests and 43 flight integration tests passed.
- Collector configuration validated with the pinned image. A local synthetic
  OTLP sink received traces, metrics and logs, with verified key/dataset headers,
  correlation and removal of injected private-data sentinels.
- Both real broker workflow cases passed with telemetry enabled; the local sink
  observed cross-service trace IDs, ASP.NET/MassTransit/Npgsql scopes, HTTP and
  messaging metrics, and logs. Disposable validation resources were removed.
- Default Compose requires no Honeycomb key. Overlay merge preserved infrastructure
  dependencies and kept the key out of application environments.
- Honeycomb ingestion was verified on 2026-11-08 using the ignored local ingest
  key and only synthetic telemetry through a temporary collector. MCP queries
  confirmed one trace and a correlated redacted warning log in
  `polonia-ingestion-check`, plus `polonia.ingestion.check = 1` in `metrics`.
  The temporary collector was removed; no application stack was started and no
  customer data was sent. This proves collector-to-Honeycomb delivery, not live
  application traffic in Honeycomb. Hosted CI of this change is still pending.

References:
[Honeycomb Collector setup](https://docs.honeycomb.io/send-data/opentelemetry/collector),
[OpenTelemetry .NET exporters](https://opentelemetry.io/docs/languages/dotnet/exporters/),
[MassTransit observability](https://masstransit.io/documentation/configuration/observability).
