[![EN](https://img.shields.io/badge/SECURITY-EN-2D2D2D?style=for-the-badge&logo=github&logoColor=FFFFFF)](https://github.com/Lonewolf239/AsyncReaderWriterLock/blob/main/docs/SECURITY.md)
[![RU](https://img.shields.io/badge/SECURITY-RU-2D2D2D?style=for-the-badge&logo=google-translate&logoColor=FFFFFF)](https://github.com/Lonewolf239/AsyncReaderWriterLock/blob/main/docs/SECURITY-RU.md)

## Security policy · AsyncReaderWriterLock

Report vulnerabilities responsibly so they can be fixed before public disclosure.

---

### Supported versions

AsyncReaderWriterLock follows a simple support model:

| Version | Status | Support |
|---------|--------|---------|
| 1.0.1 | **Current** | Security patches + bug fixes |
| Older | **Deprecated** | No patches — upgrade strongly recommended |

---

### Reporting a vulnerability

**Do not open a public issue for security vulnerabilities.**

Contact the maintainer directly:

- **Telegram:** [@an1onime](https://t.me/an1onime)

Include in your report:

1. **Affected version(s).**
2. **Description** of the vulnerability and its impact.
3. **Reproduction steps** — minimal code or configuration to trigger the issue.
4. **Suggested fix** (if you have one).

You will receive an acknowledgment within **48 hours**. A fix will be developed privately and released as a patch before public disclosure.

---

### Scope

The following areas are in scope for security reports:

- Thread‑safety issues (deadlocks, race conditions).
- Incorrect lock release (e.g., not releasing readers/writers).
- Cancellation handling (preventing resource leaks).
- Memory safety (no unmanaged memory used).

Out of scope: denial‑of‑service via excessive waiting (lock is fair by design, but large queues are a normal use case).

---

### Responsible disclosure

We take security seriously. If you discover a vulnerability, we ask that you give us a reasonable time to address it before any public disclosure.
