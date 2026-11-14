import { randomBytes } from "node:crypto";
import { createServer } from "node:net";
import { spawn } from "node:child_process";
import { fileURLToPath } from "node:url";

const directory = fileURLToPath(new URL(".", import.meta.url));
const project = `polonia-system-${randomBytes(12).toString("hex")}`;
const date = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000)
  .toISOString()
  .slice(0, 10);

async function availablePort() {
  const server = createServer();
  await new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(0, "127.0.0.1", resolve);
  });
  const port = server.address().port;
  await new Promise((resolve, reject) =>
    server.close((error) => (error ? reject(error) : resolve())),
  );
  return port;
}

function command(executable, args, env) {
  return new Promise((resolve, reject) => {
    const child = spawn(executable, args, {
      cwd: directory,
      env,
      stdio: "inherit",
    });
    child.once("error", reject);
    child.once("exit", (code, signal) => {
      if (code === 0) resolve();
      else reject(new Error(`${executable} exited with ${signal ?? code}`));
    });
  });
}

async function waitForCustomerReads(origin, departure) {
  const search = new URLSearchParams({
    DepartureAirport: "ORD",
    ArrivalAirport: "KRK",
    DepartureDate: departure,
    Page: "1",
    PageSize: "20",
  });
  let lastError;
  for (let attempt = 0; attempt < 30; attempt++) {
    try {
      const flightResponse = await fetch(
        `${origin}/api/web/flights?${search}`,
        { signal: AbortSignal.timeout(5000) },
      );
      if (!flightResponse.ok)
        throw new Error(`Flight read returned ${flightResponse.status}`);
      const flights = await flightResponse.json();
      if (!Array.isArray(flights))
        throw new Error("Flight read did not return an array");
      const fixtures = flights.filter((flight) =>
        ["SY101", "SY102"].includes(flight.flightNumber),
      );
      if (
        fixtures.length !== 2 ||
        fixtures.some((flight) => !Number.isInteger(flight.id))
      ) {
        throw new Error("Expected both provisioned fixture flights");
      }
      const fares = new URLSearchParams({ Currency: "USD" });
      fixtures.forEach((flight) =>
        fares.append("FlightIds", String(flight.id)),
      );
      const fareResponse = await fetch(`${origin}/api/web/fares?${fares}`, {
        signal: AbortSignal.timeout(5000),
      });
      if (!fareResponse.ok)
        throw new Error(`Fare read returned ${fareResponse.status}`);
      const prices = await fareResponse.json();
      if (
        !Array.isArray(prices) ||
        fixtures.some(
          (flight) =>
            !prices.some((price) => price.flightId === String(flight.id)),
        )
      ) {
        throw new Error("Expected a provisioned fare for each fixture flight");
      }
      return;
    } catch (error) {
      lastError = error;
      await new Promise((resolve) => setTimeout(resolve, 1000));
    }
  }
  throw new Error("Customer flight and fare reads did not become ready", {
    cause: lastError,
  });
}

const port = await availablePort();
const env = {
  ...process.env,
  SYSTEM_TEST_PROJECT: project,
  SYSTEM_TEST_DB_PASSWORD: randomBytes(24).toString("hex"),
  SYSTEM_TEST_BROKER_PASSWORD: randomBytes(24).toString("hex"),
  SYSTEM_TEST_SESSION_PASSWORD: randomBytes(32).toString("hex"),
  SYSTEM_TEST_WEB_PORT: String(port),
  SYSTEM_TEST_ORIGIN: `http://127.0.0.1:${port}`,
  SYSTEM_TEST_DEPARTURE: date,
};
const compose = ["compose", "--project-name", project, "-f", "compose.yaml"];
let failed = false;

try {
  await command(
    "docker",
    [
      ...compose,
      "up",
      "--build",
      "--detach",
      "--wait",
      "--wait-timeout",
      "180",
      "nginx",
    ],
    env,
  );
  await waitForCustomerReads(env.SYSTEM_TEST_ORIGIN, env.SYSTEM_TEST_DEPARTURE);
  await command(
    process.execPath,
    [
      fileURLToPath(
        new URL("./node_modules/@playwright/test/cli.js", import.meta.url),
      ),
      "test",
    ],
    env,
  );
} catch (error) {
  failed = true;
  console.error(error);
  try {
    await command(
      "docker",
      [...compose, "logs", "--no-color", "--tail", "200"],
      env,
    );
  } catch (logsError) {
    console.error("Could not collect system-test container logs:", logsError);
  }
} finally {
  try {
    await command(
      "docker",
      [...compose, "down", "--volumes", "--remove-orphans"],
      env,
    );
  } catch (cleanupError) {
    failed = true;
    console.error(
      `Cleanup failed for isolated Compose project ${project}:`,
      cleanupError,
    );
  }
}

if (failed) process.exitCode = 1;
