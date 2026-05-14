import csv, os, json, time, requests

INPUT_CSV  = "oxford_5000.csv"
OUTPUT_CSV = "../src/EnglishLearner.App/Assets/WordLists/words_enriched.csv"
CACHE_FILE = "translate_cache.json"

CEFR_MAP = {"a1":1, "a2":1, "b1":2, "b2":3, "c1":4, "c2":5}

TYPE_ABBR = {
    "verb": "v.", "noun": "n.", "adjective": "adj.", "adverb": "adv.",
    "preposition": "prep.", "conjunction": "conj.", "pronoun": "pron.",
    "determiner": "det.", "exclamation": "excl.", "interjection": "interj.",
    "modal verb": "mod.", "phrasal verb": "phr.v.", "prefix": "pref.",
    "suffix": "suf.", "abbreviation": "abbr.", "number": "num.",
    "article": "art.", "ordinal number": "ord.", "letter": "n.",
}

PROXIES = {"http": "http://127.0.0.1:7898", "https": "http://127.0.0.1:7898"}

BATCH_SIZE = 50

def load_cache():
    if os.path.exists(CACHE_FILE):
        with open(CACHE_FILE, encoding="utf-8") as f:
            return json.load(f)
    return {}

def save_cache(cache):
    with open(CACHE_FILE, "w", encoding="utf-8") as f:
        json.dump(cache, f, ensure_ascii=False, indent=2)

def translate_batch(words, cache):
    """批量翻译，每次 BATCH_SIZE 个词"""
    to_translate = [w for w in words if w not in cache]
    print(f"  需翻译: {len(to_translate)}/{len(words)} 词", flush=True)

    for i in range(0, len(to_translate), BATCH_SIZE):
        batch = to_translate[i:i+BATCH_SIZE]
        text = "\n".join(batch)
        for attempt in range(3):
            try:
                url = "https://translate.googleapis.com/translate_a/single"
                params = {"client": "gtx", "sl": "en", "tl": "zh-CN", "dt": "t", "q": text}
                r = requests.get(url, params=params, proxies=PROXIES, timeout=30)
                r.raise_for_status()
                result = r.json()
                translated = "".join(item[0] for item in result[0])
                parts = translated.split("\n")
                for j, w in enumerate(batch):
                    cache[w] = parts[j] if j < len(parts) else ""
                break
            except Exception as e:
                print(f"  batch {i//BATCH_SIZE+1} attempt {attempt+1} failed: {e}", flush=True)
                time.sleep(3 * (attempt + 1))

        done = min(i + BATCH_SIZE, len(to_translate))
        print(f"  [{done}/{len(to_translate)}] translated", flush=True)
        save_cache(cache)
        time.sleep(1.5)

def make_meaning(word_type, chinese):
    abbr = TYPE_ABBR.get(word_type.lower().strip(), "")
    if chinese:
        return f"{abbr} {chinese}".strip()
    return ""

# ---- 主流程 ----
os.makedirs(os.path.dirname(OUTPUT_CSV), exist_ok=True)

rows = []
seen = set()
with open(INPUT_CSV, encoding="utf-8") as f:
    for row in csv.DictReader(f):
        word = row["word"].strip()
        if not word or word in seen:
            continue
        seen.add(word)
        cefr = row.get("cefr", "").strip().lower()
        if cefr not in CEFR_MAP:
            continue
        rows.append(row)

all_words = [r["word"].strip() for r in rows]
print(f"Oxford CSV: {len(rows)} unique words", flush=True)

cache = load_cache()
print(f"Cache: {len(cache)} cached translations", flush=True)

# 批量翻译
translate_batch(all_words, cache)
print(f"Translation done: {len(cache)} total cached", flush=True)

# 写输出 CSV
with open(OUTPUT_CSV, "w", newline="", encoding="utf-8") as fout:
    writer = csv.DictWriter(fout, fieldnames=[
        "Text", "Phonetic", "Meaning", "Example", "DifficultyLevel"])
    writer.writeheader()

    count = 0
    for row in rows:
        word = row["word"].strip()
        phonetic = row.get("phon_br", "") or row.get("phon_n_am", "") or ""
        word_type = row.get("type", "")
        example = row.get("example", "") or ""
        level = CEFR_MAP[row.get("cefr", "").strip().lower()]

        chinese = cache.get(word, "")
        meaning = make_meaning(word_type, chinese)
        if not meaning:
            meaning = row.get("definition", "")

        writer.writerow({
            "Text":            word,
            "Phonetic":        phonetic,
            "Meaning":         meaning,
            "Example":         example,
            "DifficultyLevel": level,
        })
        count += 1

print(f"Done: {count} words -> {OUTPUT_CSV}", flush=True)
