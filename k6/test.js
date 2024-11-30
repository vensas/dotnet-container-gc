import http from "k6/http";
import { check, sleep } from "k6";

const maxUsers = __ENV.MAX_USERS || 10;

export const options = {
  stages: [
    { duration: "30s", target: maxUsers }, // Ramp up to n users in 30 seconds
    { duration: "1m", target: maxUsers }, // Stay at n users for 1 minute
    { duration: "30s", target: 0 }, // Ramp down to 0 users in 30 seconds
  ],
};

const BASE_URL = __ENV.BASE_URL || "http://127.0.0.1:5011";

export default function () {
  // Create a new person

  // Function to generate a random string
  function randomString(length) {
    const chars = "abcdefghijklmnopqrstuvwxyz";
    let result = "";
    for (let i = 0; i < length; i++) {
      result += chars.charAt(Math.floor(Math.random() * chars.length));
    }
    return result;
  }

  // Function to generate a random address
  function randomAddress() {
    return {
      Street: `${Math.floor(1 + Math.random() * 1000)} ${randomString(7)} St`,
      City: randomString(7),
      State: randomString(2).toUpperCase(),
      Country: "USA",
      PostalCode: `${Math.floor(10000 + Math.random() * 90000)}`,
    };
  }

  // Generate the test person
  const person = {
    Name: `Person_${randomString(7)}`,
    Age: Math.floor(Math.random() * 60) + 20, // Random age between 20 and 80
    CreatedAt: new Date().toISOString(),
    Address: randomAddress(),
    Metadata:
      Math.random() < 0.5
        ? {
            Hobby: ["Photography", "Cooking", "Hiking"][
              Math.floor(Math.random() * 3)
            ],
            Language: ["English", "Spanish", "French"][
              Math.floor(Math.random() * 3)
            ],
          }
        : null, // 50% chance of being null
  };

  const postResponse = http.post(`${BASE_URL}/people`, JSON.stringify(person), {
    headers: { "Content-Type": "application/json" },
  });
  check(postResponse, {
    "POST /people status is 200": (r) => r.status === 200,
  });

  const getResponse = http.get(`${BASE_URL}/people`);
  check(getResponse, {
    "GET /people status is 200": (r) => r.status === 200,
    "GET /people returns a list": (r) => Array.isArray(JSON.parse(r.body)),
    "GET /people contains the added person": (r) =>
      r.body.includes(person.Name),
  });

  sleep(1); // Pause for a second between iterations
}
