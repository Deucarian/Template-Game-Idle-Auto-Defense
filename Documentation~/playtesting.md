# Player Experience Playtesting

Generate the game, open `Assets/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame/OPEN_THIS_TO_TEST_IdleAutoDefense_PlayableGame.unity`, and press Play.

## Flow Checklist

1. Confirm the main menu appears and combat does not advance before Start Run.
2. Confirm the assigned authored run profile and `04:40` expected duration are shown.
3. Start the run and complete or skip the ten-step first-run briefing.
4. Confirm top timer, wave count/name, core health, credits, level/XP, profile, and Overdrive are readable.
5. Unlock or improve a mounted module. Verify displayed authored damage, cadence, range, rank, cost, affordability, and locked state.
6. Activate Overdrive by button or `Space`; verify active duration and cooldown state.
7. Reach a draft; verify three large eligible authored cards, rarity labels/frames, targets, effect copy, rank preview, and `1`/`2`/`3` input.
8. Use `Esc`, `Tab`, and `B` to inspect pause, Current Build, settings, and persistent research.
9. Confirm elite/boss health and offscreen marker presentation appears and clears after the threat dies.
10. Win or lose and inspect result, time, wave, kills, elite/boss status, currency, upgrades, rarity counts, ranks, core damage, restart, and menu actions.
11. Restart and confirm debug remains hidden, rewards are not duplicated, and tutorial does not repeat.
12. Return to the menu, relaunch after simulated time, and claim the authored offline reward once.
13. Switch between Bastion Command and Neon Bastion; confirm selection persists.
14. Press `F1`; confirm diagnostics are optional and disappear again on restart/menu transition.

## Known Truthful Omissions

- Continue is omitted because full mid-run resume is not implemented.
- The optional 2x offline claim is omitted when monetization availability is absent.
- Per-module damage and top-module statistics are omitted because the combat runtime does not expose attribution.
- Quit is omitted in the Editor and WebGL.

These omissions must not be replaced by fake controls or fabricated statistics.
