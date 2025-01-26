import http from "k6/http";
import { check, sleep } from "k6";

const maxUsers = __ENV.MAX_USERS || 10;

export const options = {
  stages: [
    { duration: "60s", target: maxUsers }, // Ramp up
    { duration: "120s", target: maxUsers }, // Sustained load
    { duration: "60s", target: maxUsers * 2 }, // Spike
    { duration: "120s", target: maxUsers }, // Recovery
    { duration: "60s", target: 0 }, // Scale down
  ],
};

const BASE_URL = __ENV.BASE_URL || "http://localhost:5643";

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

  // Create multiple persons in batch
  for (let i = 0; i < 3; i++) {
    const person = {
      Name: `Person_${randomString(7)}`,
      Age: Math.floor(Math.random() * 60) + 20,
      CreatedAt: new Date().toISOString(),
      Metadata: {},
    };

    const postResponse = http.post(`${BASE_URL}/people`, JSON.stringify(person), {
      headers: { "Content-Type": "application/json" },
    });
    
    if (postResponse.status === 201) {
      const personCreated = JSON.parse(postResponse.body);
      const personUrl = `${BASE_URL}/people/${personCreated.id}`;
      
      // Simulate repeated access patterns
      for(let j = 0; j < 3; j++) {
        http.get(personUrl);
      }
    }
  }

  sleep(Math.random() * 2 + 1); // Variable sleep between 1-3 seconds
}
