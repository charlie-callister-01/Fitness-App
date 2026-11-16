# 01: Exercises and settings through the exchange-format seam

**What to build:** The tracer bullet for A1. A minimal exchange document (format version, Exercises, Settings) can be written to JSON and read back losslessly through the single public reader/writer in `Core`. Bad input (malformed JSON, missing or newer format version, bad Exercise fields, duplicate or badly formatted IDs) produces plain-language field-level errors naming the exact path, never exceptions. Also starts the written data spec (root document, version scheme, Exercise with log kind, tags and benchmark flag, Settings, units and ID rules) and adds the ID generator interface so tests use deterministic IDs.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Data spec document exists with the root, Exercise and Settings sections and a worked JSON example
- [ ] Round trip of a valid minimal dataset is identical (camelCase, string enums, canonical units)
- [ ] Errors carry a path, a code and a plain-language message; malformed, empty, missing/zero/newer version cases covered
- [ ] ID format and uniqueness validated; ID generator is an interface with a deterministic test double
- [ ] The spec's example is parsed by the real reader in a conformance test
- [ ] No dependency on storage, UI or clock; `Ui` untouched
