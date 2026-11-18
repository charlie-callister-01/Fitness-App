# Two Themes (Keep and Clean) on one component set

Decided 2026-11-16, during the UI/UX design phase, after a moodboard of four directions.

Decision:
- The app ships two **Themes**: **Keep**, a fun theme inspired by old-school RuneScape and Arthurian linocut prints (bevelled stone panels, rune-yellow text with a hard black shadow, item-slot Badge tiles, ringed status orbs, zigzag tapestry borders), and **Clean**, a plain modern theme (flat surfaces, system font, no texture).
- Both Themes use **one component set**: identical layouts, sizes, touch targets and behaviour. A Theme only swaps design tokens (colour, font, border, radius, shadow, texture) and artwork. Token names are identical across Themes.
- Theme and **Mode** (dark, light, system) are separate settings. Keep is designed dark-first; Clean is equally good in both.
- Inputs are never themed beyond tokens: number fields stay high-contrast and use a plain tabular font in both Themes, so the Workout log stays fast.
- Implementation: CSS custom properties scoped by `data-theme` and `data-mode` attributes on the root element; switching is instant and needs no reload. Artwork that differs per Theme (Badges, orbs, textures) is chosen by token or a tiny per-Theme asset map.
- No Jagex art, logos, icons or fonts ship in the app. The display font is one token (`--font-display`), so a personal build can swap it locally.

Why: the owner wants the app to be fun to open, but logging must stay fast and readable in a bright gym. Keeping one component set means a Theme costs a token file, not a second UI.

Rejected: a single fantasy-only theme (risk to log-screen legibility, harder to release publicly); separate component sets per theme (doubles the UI work and the testing).
