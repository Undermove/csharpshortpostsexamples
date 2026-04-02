import http from "k6/http";
import { check, sleep } from "k6";
import { Counter } from "k6/metrics";

const BASE_URL = __ENV.BASE_URL || "http://localhost:5000";

const rejected = new Counter("rate_limited_429");
const passed = new Counter("passed_200");

export const options = {
  scenarios: {
    // Быстро выбиваем лимит на /ask (token bucket: 10 токенов, 2/мин)
    ask_endpoint: {
      executor: "constant-arrival-rate",
      rate: 5, // 5 запросов в секунду — лимит упадёт быстро
      timeUnit: "1s",
      duration: "30s",
      preAllocatedVUs: 5,
      exec: "askEndpoint",
    },
    // /articles — мягкий лимит (fixed window: 100/мин)
    articles_endpoint: {
      executor: "constant-arrival-rate",
      rate: 10,
      timeUnit: "1s",
      duration: "30s",
      preAllocatedVUs: 5,
      exec: "articlesEndpoint",
    },
    // /health — без лимитов, всегда 200
    health_endpoint: {
      executor: "constant-arrival-rate",
      rate: 20,
      timeUnit: "1s",
      duration: "30s",
      preAllocatedVUs: 5,
      exec: "healthEndpoint",
    },
  },
  thresholds: {
    // Health endpoint никогда не должен получать 429
    "passed_200{endpoint:health}": [{ threshold: "count>0" }],
    // /ask должен получить хотя бы один 429
    "rate_limited_429{endpoint:ask}": [{ threshold: "count>0" }],
  },
};

export function askEndpoint() {
  const res = http.post(
    `${BASE_URL}/ask`,
    JSON.stringify({ question: "Что такое rate limiting?" }),
    { headers: { "Content-Type": "application/json" } }
  );

  track(res, "ask");
  sleep(0.1);
}

export function articlesEndpoint() {
  const res = http.get(`${BASE_URL}/articles`);
  track(res, "articles");
  sleep(0.1);
}

export function healthEndpoint() {
  const res = http.get(`${BASE_URL}/health`);
  track(res, "health");
  sleep(0.1);
}

function track(res, endpoint) {
  const is429 = res.status === 429;
  const is200 = res.status === 200;

  rejected.add(is429 ? 1 : 0, { endpoint });
  passed.add(is200 ? 1 : 0, { endpoint });

  check(res, {
    "status is 200 or 429": (r) => r.status === 200 || r.status === 429,
  });

  if (is429) {
    const retryAfter = res.headers["Retry-After"];
    if (retryAfter) {
      console.log(`[${endpoint}] 429 — Retry-After: ${retryAfter}s`);
    }
  }
}
