Fetch {url} and return its main textual content VERBATIM as plain text.

Rules:
- Do NOT summarize, paraphrase, or add commentary.
- Preserve headings, code blocks, and list structure.
- If longer than ~{max_words} words, return the first {max_words} words and
  append '[TRUNCATED]' on a new line.
- No preamble. Start directly with the page content.
