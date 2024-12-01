import http from "k6/http";
import { check, sleep } from "k6";

const maxUsers = __ENV.MAX_USERS || 10;

export const options = {
  stages: [
    { duration: "15s", target: maxUsers }, // Ramp up to n users in 30 seconds
    { duration: "1m", target: maxUsers }, // Stay at n users for 1 minute
    { duration: "15s", target: 0 }, // Ramp down to 0 users in 30 seconds
  ],
};

const BASE_URL = __ENV.BASE_URL || "http://localhost:8080";

export default function () {
  function randomString(length) {
    const characters =
      "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    const charactersLength = characters.length;
    const result = new Array(length);
    for (let i = 0; i < length; i++) {
      result[i] = characters.charAt(
        Math.floor(Math.random() * charactersLength)
      );
    }
    return result.join("");
  }

  const person = {
    Name: `Person_${randomString(7)}`,
    Age: Math.floor(Math.random() * 60) + 20,
    CreatedAt: new Date().toISOString(),
    Metadata: {},
  };

  const postResponse = http.post(`${BASE_URL}/people`, JSON.stringify(person), {
    headers: { "Content-Type": "application/json" },
  });
  check(postResponse, {
    "POST /people status is 201": (r) => r.status === 201,
  });

  const personCreated = JSON.parse(postResponse.body);
  const personUrl = `${BASE_URL}/people/${personCreated.id}`;

  const getResponse = http.get(personUrl, {
    tags: { name: "GetPeopleUrl" },
  });
  check(getResponse, {
    "GET /people status is 200": (r) => r.status === 200,
    "GET /people contains the added person": (r) =>
      r.body.includes(person.Name),
  });

  const deleteResponse = http.del(personUrl, JSON.stringify(person), {
    headers: { "Content-Type": "application/json" },
    tags: { name: "DeletePersonUrl" },
  });
  check(deleteResponse, {
    "DELETE /people/:id status is 200": (r) => r.status === 200,
  });

  sleep(0.25);
}
