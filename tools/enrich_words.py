from typing import Optional
import csv, json, time, requests, os

INPUT_CSV  = "oxford_5000.csv"        # 第1步下载的文件
OUTPUT_CSV = "../data/wordlists/words_enriched.csv"

CEFR_MAP = {"a1":1,"a2":1,"b1":2,"b2":3,"c1":4,"c2":5}

PROXIES = {"http": "http://127.0.0.1:7898", "https": "http://127.0.0.1:7898"}

def fetch_word(word: str) -> Optional[dict]:
    try:
        r = requests.get(
            f"https://api.dictionaryapi.dev/api/v2/entries/en/{word}",
            timeout=10, proxies=PROXIES)
        if r.status_code != 200:
            return None
        data = r.json()[0]
        phonetic = data.get("phonetic", "")
        if not phonetic:
            for p in data.get("phonetics", []):
                if p.get("text"):
                    phonetic = p["text"]
                    break
        meaning, example = "", ""
        for m in data.get("meanings", []):
            defs = m.get("definitions", [])
            if defs:
                meaning  = meaning  or defs[0].get("definition", "")
                example  = example  or defs[0].get("example", "")
            if meaning and example:
                break
        return {"phonetic": phonetic, "meaning": meaning, "example": example}
    except Exception:
        return None

os.makedirs(os.path.dirname(OUTPUT_CSV), exist_ok=True)

with open(INPUT_CSV, encoding="utf-8") as fin, \
     open(OUTPUT_CSV, "w", newline="", encoding="utf-8") as fout:

    reader = csv.DictReader(fin)
    writer = csv.DictWriter(fout, fieldnames=[
        "Text","Phonetic","Meaning","Example","DifficultyLevel"])
    writer.writeheader()

    for i, row in enumerate(reader):
        word  = row["word"].strip().lower()
        cefr  = row.get("cefr", "b1").strip().lower()
        level = CEFR_MAP.get(cefr, 2)

        info = fetch_word(word)
        if info and info["meaning"]:
            writer.writerow({
                "Text":            word,
                "Phonetic":        info["phonetic"],
                "Meaning":         info["meaning"],
                "Example":         info["example"],
                "DifficultyLevel": level,
            })
            print(f"[{i+1}] OK {word}")
        else:
            # API 没有的词，只保留基础信息
            writer.writerow({
                "Text": word, "Phonetic":"","Meaning":"",
                "Example":"","DifficultyLevel":level})
            print(f"[{i+1}] -- {word} (no definition)")

        time.sleep(0.2)   # 每5秒最多25个请求，不触发限流

print(f"\n完成 → {OUTPUT_CSV}")