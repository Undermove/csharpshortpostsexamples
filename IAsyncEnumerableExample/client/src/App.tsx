import { useRef, useState } from "react";
import { API, Article, fetchStreaming, kb, ms, now, streamNdjson } from "./api";

type Variant = "idle" | "list" | "ndjson" | "sse";

export default function App() {
  return (
    <div className="page">
      <header>
        <h1>Стриминг списка: list · ndjson · sse</h1>
        <p className="sub">
          Один и тот же список статей, отданный тремя способами с бэка на{" "}
          <code>{API}</code>. Жми кнопку и смотри, кто рисует сразу, а кто держит
          экран пустым до конца.
        </p>
      </header>

      <ListDemo />
      <BigArticleDemo />

      <footer>
        list — буфер (весь JSON-массив разом). ndjson — стрим по строке, парсим сами.
        sse — стрим, который браузер парсит сам через EventSource. Одна большая статья —
        стримим тело и показываем по мере прихода.
      </footer>
    </div>
  );
}

function ListDemo() {
  const [count, setCount] = useState(40);
  const [variant, setVariant] = useState<Variant>("idle");
  const [list, setList] = useState<Article[]>([]);
  const [firstMs, setFirstMs] = useState<number | null>(null);
  const [totalMs, setTotalMs] = useState<number | null>(null);
  const abort = useRef<AbortController | null>(null);
  const es = useRef<EventSource | null>(null);

  const reset = () => {
    abort.current?.abort();
    es.current?.close();
    abort.current = new AbortController();
    setList([]);
    setFirstMs(null);
    setTotalMs(null);
  };

  // 1) LIST — буфер: ждём весь ответ, рисуем разом.
  const runList = async () => {
    reset();
    setVariant("list");
    const t = now();
    try {
      const res = await fetch(`${API}/articles/list?count=${count}`,
        { signal: abort.current!.signal });
      const data: Article[] = await res.json();
      setFirstMs(ms(t));
      setList(data);
      setTotalMs(ms(t));
    } finally {
      setVariant("idle");
    }
  };

  // 2) NDJSON — стрим, парсим тело сами по \n.
  const runNdjson = async () => {
    reset();
    setVariant("ndjson");
    const t = now();
    try {
      for await (const a of streamNdjson<Article>(
        `${API}/articles/ndjson?count=${count}`,
        abort.current!.signal,
      )) {
        setList((prev) => {
          if (prev.length === 0) setFirstMs(ms(t));
          return [...prev, a];
        });
      }
      setTotalMs(ms(t));
    } finally {
      setVariant("idle");
    }
  };

  // 3) SSE — стрим, который браузер парсит сам. Никакого reader/буфера.
  const runSse = () => {
    reset();
    setVariant("sse");
    const t = now();
    const source = new EventSource(`${API}/articles/sse?count=${count}`);
    es.current = source;

    source.addEventListener("article", (e) => {
      const a: Article = JSON.parse((e as MessageEvent).data);
      setList((prev) => {
        if (prev.length === 0) setFirstMs(ms(t));
        return [...prev, a];
      });
    });
    // Финальное событие — закрываем сами, иначе EventSource переподключится.
    source.addEventListener("done", () => {
      setTotalMs(ms(t));
      source.close();
      setVariant("idle");
    });
    source.onerror = () => {
      source.close();
      setVariant("idle");
    };
  };

  const busy = variant !== "idle";
  const Btn = ({ v, label }: { v: Variant; label: string }) => (
    <button
      className={v === "ndjson" || v === "sse" ? "primary" : ""}
      disabled={busy}
      onClick={v === "list" ? runList : v === "ndjson" ? runNdjson : runSse}
    >
      {variant === v ? "…" : label}
    </button>
  );

  return (
    <section className="card">
      <h2>Список статей — три варианта</h2>
      <p className="hint">
        Список из полных статей (~234 КБ каждая). list ждёт весь массив; ndjson и sse
        показывают статьи по мере прихода.
      </p>

      <div className="controls">
        <label>
          статей: <b>{count}</b>
          <input type="range" min={5} max={120} value={count}
            disabled={busy} onChange={(e) => setCount(+e.target.value)} />
        </label>
        <div className="btns">
          <Btn v="list" label="List" />
          <Btn v="ndjson" label="NDJSON" />
          <Btn v="sse" label="SSE" />
        </div>
      </div>

      <div className="stats">
        <Stat label="первая статья на экране" value={firstMs} unit="мс" />
        <Stat label="всё загрузилось" value={totalMs} unit="мс" />
        <Stat label="на экране" value={list.length} unit="шт" />
      </div>

      <div className={`feed ${variant === "ndjson" || variant === "sse" ? "live" : ""}`}>
        {list.map((a) => (
          <div className="row" key={a.id}>
            <span className="id">#{a.id}</span>
            <span className="title">{a.title}</span>
            <span className="size">{kb(a.contentJson.length)} КБ</span>
          </div>
        ))}
        {list.length === 0 && (
          <div className="empty">
            {busy ? "ждём первую статью…" : "выбери вариант: List / NDJSON / SSE"}
          </div>
        )}
      </div>
    </section>
  );
}

function BigArticleDemo() {
  const [id, setId] = useState(1);
  const [loading, setLoading] = useState(false);
  const [bytes, setBytes] = useState(0);
  const [firstMs, setFirstMs] = useState<number | null>(null);
  const [totalMs, setTotalMs] = useState<number | null>(null);
  const [article, setArticle] = useState<{ title: string; body: string } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const abort = useRef<AbortController | null>(null);

  const load = async () => {
    abort.current?.abort();
    abort.current = new AbortController();
    setLoading(true);
    setBytes(0); setFirstMs(null); setTotalMs(null); setArticle(null); setError(null);
    const t = now();
    try {
      let first = false;
      const text = await fetchStreaming(`${API}/article/${id}/getstream`, (b) => {
        if (!first) { first = true; setFirstMs(ms(t)); }
        setBytes(b);
      }, abort.current.signal);
      setTotalMs(ms(t));
      const parsed = JSON.parse(text);
      setArticle({ title: parsed.title ?? `#${id}`, body: String(parsed.body ?? "") });
    } catch (e) {
      setError(String(e));
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className="card">
      <h2>Одна большая статья (~234 КБ) — стримим и показываем</h2>
      <p className="hint">
        Грузим одну тяжёлую статью через /article/&#123;id&#125;/getstream (LONGBLOB + GetStream),
        читаем тело потоком и показываем по мере прихода — весь объект в памяти браузера разом не держим.
      </p>

      <div className="controls">
        <label>
          id статьи:
          <input type="number" min={1} max={500} value={id}
            disabled={loading} onChange={(e) => setId(+e.target.value)} />
        </label>
        <div className="btns">
          <button className="primary" onClick={load} disabled={loading}>
            {loading ? "грузим…" : "Загрузить (стрим)"}
          </button>
        </div>
      </div>

      <div className="stats">
        <Stat label="первый байт" value={firstMs} unit="мс" />
        <Stat label="готово" value={totalMs} unit="мс" />
        <Stat label="получено" value={bytes ? kb(bytes) : null} unit="КБ" />
      </div>

      <div className="progress">
        <div className="bar" style={{ width: `${Math.min(100, (bytes / 240000) * 100)}%` }} />
      </div>

      {error && <div className="empty">ошибка: {error}</div>}
      {article && (
        <div className="article">
          <div className="article-title">{article.title}</div>
          <div className="article-body">{article.body.slice(0, 600)}…</div>
          <div className="article-meta">тело: {kb(article.body.length)} КБ текста</div>
        </div>
      )}
    </section>
  );
}

function Stat({ label, value, unit }: { label: string; value: number | null; unit: string }) {
  return (
    <div className="stat">
      <div className="stat-val">
        {value == null ? "—" : value}
        {value != null && <span className="unit"> {unit}</span>}
      </div>
      <div className="stat-label">{label}</div>
    </div>
  );
}
