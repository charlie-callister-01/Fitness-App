# 03: Prompt pack tab (S-10 Prompts)

**What to build:** Build the **Prompts** tab on `/backup` presenting the three ready-made prompt cards from `Core.Exchange` (**Analyse my training**, **Write me a Plan**, and **Write Guides**) with short plain-language explanations and a **Copy prompt** button on each. The **Write Guides** card lets the owner choose which Exercises to include (defaulting to Exercises without a Guide), and copying any prompt writes it to the clipboard via the browser-capabilities seam with clear copy confirmation.

**Blocked by:** 02: Export tab, Backup download, and Weekly review link (S-10 Export, F-06)

**Status:** ready-for-agent

- [ ] Prompts tab renders all three prompt cards (**Analyse my training**, **Write me a Plan**, **Write Guides**) with plain-language descriptions.
- [ ] **Write Guides** defaults to Exercises in the Exercise library without a Guide and allows selecting/deselecting Exercises before copying.
- [ ] Tapping **Copy prompt** on any card copies the generated prompt text from `PromptPack` to the clipboard and shows a clear confirmation state.
- [ ] UI tests cover all three prompt cards, Exercise selection for Write Guides, and clipboard copy output with 0 build warnings.
