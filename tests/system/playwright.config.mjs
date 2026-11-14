import { defineConfig, devices } from "@playwright/test";

if (
  !process.env.SYSTEM_TEST_ORIGIN ||
  !process.env.SYSTEM_TEST_PROJECT ||
  !process.env.SYSTEM_TEST_DEPARTURE
) {
  throw new Error(
    "Run system tests using pnpm test, not the Playwright CLI directly.",
  );
}

export default defineConfig({
  testDir: "./specs",
  timeout: 120_000,
  expect: { timeout: 15_000 },
  workers: 1,
  retries: 0,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: process.env.SYSTEM_TEST_ORIGIN,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    ...devices["Desktop Chrome"],
  },
});
