export const API = "http://localhost:5005";

export interface Article {
  id: number;
  title: string;
  contentJson: string;
}

// Стримим NDJSON: читаем тело по кускам через ReadableStream, режем по \n,
// отдаём готовые объекты по мере прихода.
export async function* streamNdjson<T>(
  url: string,
  signal?: AbortSignal,
): AsyncGenerator<T> {
  const res = await fetch(url, { signal });
  if (!res.ok || !res.body) throw new Error(`HTTP ${res.status}`);

  const reader = res.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true });

    let nl: number;
    while ((nl = buffer.indexOf("\n")) >= 0) {
      const line = buffer.slice(0, nl);
      buffer = buffer.slice(nl + 1);
      if (line.trim()) yield JSON.parse(line) as T;
    }
  }
  if (buffer.trim()) yield JSON.parse(buffer) as T;
}

// Стримим один большой объект: читаем тело по кускам, репортим прогресс по байтам,
// возвращаем собранный текст (для показа в интерфейсе).
export async function fetchStreaming(
  url: string,
  onProgress: (bytes: number) => void,
  signal?: AbortSignal,
): Promise<string> {
  const res = await fetch(url, { signal });
  if (!res.ok || !res.body) throw new Error(`HTTP ${res.status}`);

  const reader = res.body.getReader();
  const decoder = new TextDecoder();
  let text = "";
  let bytes = 0;
  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    bytes += value.byteLength;
    text += decoder.decode(value, { stream: true });
    onProgress(bytes);
  }
  text += decoder.decode();
  return text;
}

export const now = () => performance.now();
export const ms = (start: number) => Math.round(performance.now() - start);
export const kb = (chars: number) => Math.round(chars / 1024);
