// k6-бенчмарк одного варианта. Нагрузку и латентность даёт k6, а пик памяти / аллокации /
// число gen2-сборок берём из семплера через /memory (reset компактит LOH и сбрасывает пик).
//
// Список (нужен ?count):
//   for v in list ndjson sse; do ENDPOINT=/articles/$v k6 run bench.k6.js; done
// Один большой объект (без count):
//   for v in getstring gettextreader getstream; do ENDPOINT=/article/1/$v QUERY= k6 run bench.k6.js; done
//
// Env: ENDPOINT, COUNT (200), QUERY (по умолчанию count=COUNT), VUS (8), DURATION (12s), API.
import http from "k6/http";

const API = __ENV.API || "http://localhost:5005";
const ENDPOINT = __ENV.ENDPOINT || "/articles/ndjson";
const COUNT = __ENV.COUNT || "200";
const QUERY = __ENV.QUERY !== undefined ? __ENV.QUERY : `count=${COUNT}`;
const URL = QUERY ? `${API}${ENDPOINT}?${QUERY}` : `${API}${ENDPOINT}`;

// ITER задан → фиксированное число запросов (честное сравнение аллокаций/gen2);
// иначе — нагрузка на время.
const load = __ENV.ITER
  ? { executor: "shared-iterations", vus: Number(__ENV.VUS || 8), iterations: Number(__ENV.ITER), maxDuration: "120s" }
  : { executor: "constant-vus", vus: Number(__ENV.VUS || 8), duration: __ENV.DURATION || "12s" };

export const options = {
  scenarios: { load },
  summaryTrendStats: ["avg", "p(95)", "max"],
};

export function setup() {
  http.post(`${API}/memory/reset`);
  const m = http.get(`${API}/memory`).json();
  return { gen2: m.gen2, alloc: m.totalAllocatedMb };
}

export default function () {
  http.get(URL); // http_req_waiting в отчёте = время до первого байта (TTFB)
}

export function teardown(data) {
  const m = http.get(`${API}/memory`).json();
  console.log(
    `\n>>> ${ENDPOINT}: peakHeap=${m.peakManagedHeapMb}MB  ` +
    `allocated(+)=${m.totalAllocatedMb - data.alloc}MB  gen2(+)=${m.gen2 - data.gen2}`,
  );
}
