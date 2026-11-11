# EyeCanDo

A landscape Unity 3D navigation MVP for children aged 3–7 and their caregivers.

## Open and run

1. In Unity Hub choose **Add → Add project from disk** and select this `EyeCanDo` folder.
2. Open with **Unity 6000.6.8f1**, installed at `/Volumes/Development/UnityEditors/6000.6.8f1/Unity.app`.
3. Allow the first package import to finish. Package downloads may require internet during development; the game itself works offline.
4. Open `Assets/EyeCanDo/Scenes/Start.unity`, select a **16:9 landscape** Game view, and press **Play**. The configured Play Mode start scene also starts here when another scene is open.
5. Select Start. On a new profile, a caregiver enters a name and selects Continue. Returning profiles go straight to the map.

Use the mouse to click or a touchscreen to tap. Select any equipment landmark or its large label to explore a journey. All Discover, Watch and Practice activities display Coming Soon. The shopping bag opens the Shop placeholder; the gear opens Settings. In Settings select Edit, enter a name, then Save or Cancel. Escape closes a dialog or returns from a journey to the map.

## Project structure

- `Assets/EyeCanDo/Scenes`: saved Start, JourneyMap and JourneySelection scenes.
- `Assets/EyeCanDo/Prefabs`: original owl, equipment destinations, application root, pathway pebble and sublevel card.
- `Assets/EyeCanDo/Data`: catalog and three editable journey assets.
- `Assets/EyeCanDo/Runtime`: navigation, profile, scene controllers, responsive UI and gentle animations.
- `Assets/EyeCanDo/Editor`: explicit scene-generation utilities and validation tools.
- `Validation`: machine-readable check results and Game-view screenshots.

The username is the only profile value, saved locally with PlayerPrefs key `EyeCanDo.Profile.Username`, under company/product **EyeCanDo / EyeCanDo**. Setup and Settings share one validator: trim the name, require 1–20 characters and a single line. Rich text is disabled when displaying names. No account, service connection or paid asset is needed.

## Extend a journey

1. Create a **EyeCanDo → Journey** asset and give it a unique stable ID.
2. Assign its title, short title, accent, pictogram and destination prefab. The destination prefab needs `JourneyDestination`, a collider and a large pictorial selector; bind both to its definition.
3. Add ordered sublevel definitions with unique stable IDs, phases, titles and pictograms. Add the journey to the catalog. The map generates destinations and paths from this list when it changes; the journey scene generates its cards from the selected definition.
4. To activate an activity in a later iteration, set its `isPlayable` flag and full Unity scene path and enable that scene in Build Profiles. An unavailable or unconfigured scene keeps the friendly Coming Soon flow.

The current IDs are `eye-drops`, `slit-lamp`, and `vision-testing`. Sublevel IDs append `.discover`, `.watch` or `.practice`. IDs are independent of display text. A small persistent application root owns the profile and scene navigation; content-scene controllers own their dialogs. Transitions block additional taps. Only one content scene and EventSystem are active at a time.

## Checks and regeneration

The **EyeCanDo → Validation** menu runs profile/configuration checks and input-driven Play Mode checks. The latter temporarily replaces the username and restores it when Play Mode ends. It exercises the real EventSystem with simulated mouse and touch input. Reports are written to `Validation/`.

The initial implementation passed **33 profile/configuration checks** and **56 Play Mode navigation/layout checks** in Unity 6000.6.8f1. A separate complete Editor shutdown/restart confirmed username persistence and the returning-player flow; an immediate second journey request was ignored during the first transition. The map was checked at 1920×1080, 1920×1200, 1440×1080 and 2520×1080; additional captures cover the Start and journey screens at 4:3 and Settings at ultrawide. The original local profile was restored after checking.

Observed Editor limitation: Unity 6.6's **SearchDatabase indexing** logged an `ArgumentOutOfRangeException` during initialization. Its stack trace is in Unity's Editor search code. The navigation report records this separately in `editorIssues`; application checks passed and there were no C# compilation errors. Physical-device and standalone-build validation have not been performed.

**EyeCanDo → Generate MVP Scenes and Assets** rebuilds the generated scenes, data and prefabs from the C# artwork/build utilities. It is an explicit authoring tool, does not run when the game starts, and overwrites manual edits to those generated assets. Use it only when deliberately regenerating the MVP; future feature work should normally edit existing assets. Text resources can be imported through the preceding Prepare Text Resources menu item.

Only navigation is implemented. Preparation gameplay, audio narration, progression, rewards, shop purchases and unrelated settings are deferred. Validation targets the Unity Editor; standalone builds and physical tablet tests are deferred.
