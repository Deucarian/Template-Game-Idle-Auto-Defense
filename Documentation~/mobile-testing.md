# Mobile And Safe-Area Testing

The target is phone-like landscape. The runtime panel has no fixed 1280x720 minimum; player UI scales from the panel reference resolution, applies `Screen.safeArea` padding, compacts below authored width/height breakpoints, and shows a rotate-device message in portrait.

## Required Viewports

Test Game view at:

- 1920x1080
- 1280x720
- 960x540
- 844x390
- 1024x768

At every landscape size verify:

- timer and major-threat bar do not overlap
- four module cards and Overdrive remain tappable
- touch targets remain at least the authored 44px minimum
- reward cards remain readable and selectable without hover
- pause, Build, settings, tutorial, offline claim, and summary scroll or fit
- safe-area padding keeps corner controls away from display cutouts
- toast, threat marker, and bottom module bar do not cover required HUD values
- normal play contains no debug panel

For portrait, verify the rotate-device message replaces gameplay interaction. Keyboard/mouse and standard UI Toolkit pointer/touch events must continue to use the same command paths.

`IdleAutoDefensePlayerExperienceController.CalculateSafeAreaInsets` has EditMode coverage for all five landscape targets. Visual inspection is still required after changing fonts, copy length, panel dimensions, or theme contrast.
