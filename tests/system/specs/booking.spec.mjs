import { execFileSync } from "node:child_process";
import { expect, test } from "@playwright/test";

function query(database, sql) {
  return execFileSync(
    "docker",
    [
      "compose",
      "--project-name",
      process.env.SYSTEM_TEST_PROJECT,
      "-f",
      "compose.yaml",
      "exec",
      "-T",
      "postgres",
      "psql",
      "-X",
      "-v",
      "ON_ERROR_STOP=1",
      "-U",
      "system_test",
      "-d",
      database,
      "-t",
      "-A",
      "-c",
      sql,
    ],
    { encoding: "utf8" },
  ).trim();
}

async function submitReservation(page, flightNumber, travellers) {
  const search = new URLSearchParams({
    from: "ORD",
    to: "KRK",
    departure: process.env.SYSTEM_TEST_DEPARTURE,
    tripType: "oneWay",
    adults: String(travellers),
  });
  await page.goto(`/?${search}`);
  await page.getByRole("button", { name: "Search flights" }).click();
  await expect(page).toHaveURL(/\/flightlist/);

  const card = page.getByRole("heading", { name: flightNumber }).locator("..");
  for (let index = 0; index < travellers; index++) {
    await card
      .getByRole("combobox", { name: `Adult ${index + 1} fare` })
      .click();
    await page
      .getByRole("option", { name: "Economy · Light · $125.00" })
      .click();
  }
  await page
    .getByRole("button", { name: "Review priced offer and add passengers" })
    .click();
  await expect(page).toHaveURL(/\/booking\?/);

  for (let index = 0; index < travellers; index++) {
    await page
      .getByLabel("First name", { exact: true })
      .nth(index)
      .fill(`Test${index + 1}`);
    await page
      .getByLabel("Last name", { exact: true })
      .nth(index)
      .fill("Traveller");
    await page.locator('input[type="date"]').nth(index).fill("1990-01-01");
  }
  await page.getByLabel("Contact name").fill("System Test");
  await page.getByLabel("Contact email").fill("system@example.test");
  await page.getByRole("button", { name: "Review reservation" }).click();
  await page.getByRole("button", { name: "Reserve without payment" }).click();

  const heading = page.getByRole("heading", {
    name: /^Reservation reference: /,
  });
  await expect(heading).toBeVisible();
  const code = (await heading.innerText())
    .replace("Reservation reference: ", "")
    .trim();
  expect(code).toMatch(/^[A-Z0-9]{6}$/);
  return code;
}

function reservationIdentity(code) {
  if (!/^[A-Z0-9]{6}$/.test(code))
    throw new Error("Invalid reservation reference");
  const result = query(
    "polonia_booking",
    `
    SELECT r."Id", f."FlightId"
    FROM "Reservations" r JOIN "ReservationFlights" f ON f."ReservationId" = r."Id"
    WHERE r."Code" = '${code}'`,
  );
  if (!/^\d+\|\d+$/.test(result))
    throw new Error("Expected exactly one reservation and flight");
  return result.split("|");
}

function inventoryState(reservationId, flightId) {
  if (!/^\d+$/.test(reservationId) || !/^\d+$/.test(flightId)) {
    throw new Error("Invalid service-owned identifiers");
  }
  return query(
    "polonia_seating",
    `
    SELECT i."ReservedSeats",
      (SELECT COUNT(*) FROM "Assignments" a
       WHERE a."ReservationId" = '${reservationId}' AND a."FlightId" = '${flightId}')
    FROM "SeatInventories" i WHERE i."FlightId" = '${flightId}' AND i."CabinId" = 'system-economy'`,
  );
}

test("a guest books and reloads a confirmed reservation with a durable allocation", async ({
  page,
}) => {
  const code = await submitReservation(page, "SY101", 1);
  await expect(
    page.getByRole("heading", { name: "Your reservation is confirmed." }),
  ).toBeVisible({ timeout: 90_000 });
  await expect(
    page.getByText(
      "Payment is not collected. This reservation is not a paid ticket.",
    ),
  ).toBeVisible();
  await expect(
    page.getByText("Test1 Traveller", { exact: true }),
  ).toBeVisible();

  const [reservationId, flightId] = reservationIdentity(code);
  await expect
    .poll(() => inventoryState(reservationId, flightId), { timeout: 30_000 })
    .toBe("1|1");
  await page.reload();
  await expect(
    page.getByRole("heading", { name: `Reservation reference: ${code}` }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Your reservation is confirmed." }),
  ).toBeVisible();
  await expect(
    page.getByText("Test1 Traveller", { exact: true }),
  ).toBeVisible();
});

test("insufficient capacity cancels the reservation without allocating seats", async ({
  page,
}) => {
  const code = await submitReservation(page, "SY102", 2);
  await expect(
    page.getByText("The reservation was cancelled (inventory_unavailable)."),
  ).toBeVisible({ timeout: 90_000 });
  await expect(
    page.getByRole("heading", { name: "Your reservation is confirmed." }),
  ).toHaveCount(0);
  const [reservationId, flightId] = reservationIdentity(code);
  await expect
    .poll(() => inventoryState(reservationId, flightId), { timeout: 30_000 })
    .toBe("0|0");
});
